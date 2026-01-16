# [파일명: Boss_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 내 보스(Boss)의 스탯 데이터를 관리합니다. 보스는 일반 적과 분리되어 고유한 스탯과 AI를 가지며, 스테이지 마지막에 등장하여 플레이어에게 강력한 도전을 제공합니다.

**주요 스크립트:**
- `BossStat.cs` (Data Layer) - 보스 데이터 모델
- `IBossStatRepository.cs` - 보스 저장소 인터페이스
- `BossStatRepository.cs` (Repository Layer) - BGDatabase 연동

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
EnemySpawner (Manager Layer)
    └─> BossStatRepository (Repository Layer) → BGDatabase
    └─> BossStat (Data Layer) - 보스 스탯 데이터
    └─> SpawnBoss(string bossStatId, StageStat stageStat)
        └─> 배율 적용된 보스 스폰 (풀링 미사용)
```

### Dependencies
- **외부 시스템:**
  - `BGDatabase`: 보스 스탯 데이터 영속화
  - `EnemySpawner`: 보스 스폰 및 배율 적용
  - `StageStat`: 스테이지 배율 참조

### Diagram Description
```
┌─────────────────────────────────────────────┐
│          EnemySpawner                       │
│  - SpawnBoss(bossStatId, stageStat)        │
└────────────┬───────────────────────────────┘
             │
             ▼
    ┌────────────────┐
    │ BossStatRepo   │
    │  (Repository)  │
    └────────────────┘
             │
             ▼
    ┌────────────────┐
    │   BossStat     │
    │  - MaxHP       │ (BigInteger)
    │  - AttackDamage│ (BigInteger)
    │  - MoveSpeed   │ (float)
    │  - GoldReward  │ (BigInteger)
    │  - Exp         │ (double)
    └────────────────┘
             │
             ▼ EnemyStat으로 변환 후 배율 적용
    ┌────────────────┐
    │  EnemyAI (Boss)│
    └────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### BossStat (Data Layer)
- **기능:** 불변 데이터 클래스 (record)
- **주요 필드:**
  - `MaxHP`, `AttackDamage`, `GoldReward` → BigInteger
  - `MoveSpeed` → float
  - `Exp` → double
- **용도:** BGDatabase의 BossStat 테이블과 매핑

### IBossStatRepository
- **기능:** 보스 저장소 인터페이스
- **메서드:** `GetById(string bossStatId)`

### BossStatRepository (Repository Layer)
- **기능:** BGDatabase에서 BossStat 로드
- **테이블:** `BossStat` 테이블
- **핵심 메서드:**
  - `GetById(string bossStatId)` - ID로 보스 스탯 조회
  - string → BigInteger 변환 로직

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 보스와 일반 적을 분리했는가?
- **목적:** 보스의 고유한 스탯과 AI 관리
- **효과:** 일반 적 시스템과 보스 시스템의 책임 분리

### 2. 왜 BigInteger를 사용했는가?
- **목적:** 방치형 게임의 무한 스케일링 지원
- **효과:** 보스 체력, 공격력, 골드 보상 무한 증가

### 3. 왜 보스는 오브젝트 풀링하지 않는가?
- **목적:** 보스는 스테이지당 1회만 등장
- **효과:** 풀링 불필요, 직접 Instantiate/Destroy

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
**EnemySpawner:**
- `BossPrefab`: 보스 프리팹 할당 (선택 사항)
- `BossSpawnPoint`: 보스 스폰 위치

### 초기화 방법
1. 씬에 EnemySpawner 프리팹 배치
2. BossSpawnPoint 설정
3. BGDatabase에 `BossStat` 테이블 생성
4. 테이블에 보스 데이터 입력 (ID, 스탯 등)
5. 스크립트에서 `EnemySpawner.Instance.SpawnBoss(bossStatId, stageStat)` 호출

### 사용 예시
```csharp
// 보스 스폰
StageStat stageStat = StageManager.Instance.GetStageStat(currentStageId);
EnemySpawner.Instance.SpawnBoss("BOSS_DRAGON", stageStat);
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **보스 오브젝트 풀 버그**
   - **위치:** `EnemySpawner.Return()`
   - **문제:** 보스는 `Destroy()` 처리되지만, 만약 `IsBoss` 확인이 실패하면 풀에 반환 시도
   - **현재 상태:** `IsBoss` 확인 로직이 있어 안전함
   - **개선:** `IsBoss` 플래그 강화 또는 Boss 전용 `ReturnBoss()` 메서드 분리

2. **NullReference 가능성**
   - **위치:** `SpawnBoss()` 메서드
   - **문제:** `_bossStatRepository`가 null일 때
   - **방어:** null 체크가 필요함

### Refactoring 제안
1. **BossAI와 EnemyAI 분리**
   - **현재:** 하나의 EnemyAI에서 `IsBoss` 플래그로 구분
   - **개선:** 상속 구조로 분리 (BossAI : EnemyAI)

2. **보스 전용 스폰 로직 분리**
   - **현재:** `EnemySpawner.SpawnBoss()`
   - **개선:** `BossSpawner` 별도 클래스로 분리

3. **BossStat과 EnemyStat의 통합**
   - **현재:** 별도 record 타입
   - **개선:** 공통 `BaseEntityStat` record를 만들고 상속

### 코드 스타일
- ✅ BigInteger 사용 올바름
- ✅ record 타입 사용 올바름
- ⚠️ 보스와 적 시스템이 엉켜있어 책임 분리가 약함
- ⚠️ `IsBoss` 플래그로 구분하는 방식이 모호함
