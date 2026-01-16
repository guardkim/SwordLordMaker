# [파일명: Enemy_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 내 적(Enemy) 및 보스(Boss)의 생성, AI 동작, 데이터 관리, 그리고 오브젝트 풀링을 담당합니다. 일반 적은 Object Pool을 통해 재사용되며, 보스는 개별 생성 후 파괴됩니다.

**주요 스크립트:**
- `EnemyStat.cs` - 적 데이터 모델 (Data Layer)
- `IEnemyStatRepository.cs` - 적 저장소 인터페이스
- `EnemyStatRepository.cs` - 적 데이터 저장소 구현 (Repository Layer)
- `EnemySpawner.cs` - 적 생성 및 오브젝트 풀 관리 (Manager Layer)
- `EnemyAI.cs` - 적 AI 로직
- `EnemyAnimation.cs` - 적 애니메이션 제어
- `EnemyHPBar.cs` - 적 체력바 UI (UI Layer)
- `Billboard.cs` - 빌보드 효과 (UI Layer)

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
EnemySpawner (Manager Layer)
    └─> EnemyStatRepository (Repository Layer) → BGDatabase
    └─> BossStatRepository (Repository Layer) → BGDatabase
    └─> ObjectPool<EnemyAI> (오브젝트 풀링)
        └─> EnemyAI (AI 로직)
            └─> EnemyAnimation (애니메이션)
            └─> EnemyHPBar (체력바)
                └─> Billboard (빌보드)
```

### Dependencies
- **외부 시스템:**
  - `BGDatabase`: 적/보스 스탯 데이터 영속화
  - `UnityEngine.Pool.ObjectPool`: 오브젝트 풀링 구현
  - `IDamageable`: 데미지 인터페이스 (게임 내 컴포넌트)

### Diagram Description
```
┌─────────────────────────────────────────────────────┐
│                 EnemySpawner                       │
│  (Singleton + Object Pool Manager)                 │
│  - Spawn()        - 풀에서 적 가져오기              │
│  - Return()       - 풀에 반환 (보스는 Destroy)     │
│  - SpawnBoss()    - 보스 직접 생성                 │
└────────────┬────────────────────┬───────────────────┘
             │                    │
             ▼                    ▼
    ┌────────────────┐   ┌─────────────────┐
    │ EnemyStatRepo  │   │ BossStatRepo    │
    │  (Repository)  │   │  (Repository)   │
    └────────────────┘   └─────────────────┘
             │
             ▼
    ┌────────────────┐
    │   ObjectPool   │
    │   <EnemyAI>    │
    └───────┬────────┘
            │
            ▼
    ┌────────────────┐
    │    EnemyAI     │
    │   (IDamageable)│
    └───────┬────────┘
            │
            ▼
    ┌────────────────┐
    │  EnemyHPBar    │
    │   + Billboard  │
    └────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### EnemyStat (Data Layer)
- **기능:** 불변 데이터 클래스 (record)
- **주요 필드:**
  - `MaxHP`, `AttackDamage`, `GoldReward` → BigInteger
  - `MoveSpeed` → float
  - `Exp` → double
- **용도:** BGDatabase의 EnemyStat 테이블과 매핑

### EnemyStatRepository (Repository Layer)
- **기능:** BGDatabase에서 EnemyStat 로드
- **핵심 메서드:**
  - `GetById(string id)` - ID로 적 스탯 조회
  - BGDatabase의 string → BigInteger 변환 로직

### EnemySpawner (Manager Layer)
- **디자인 패턴:** Singleton, Object Pool, Factory
- **핵심 메서드:**
  - `Spawn(string statId, int spawnPointIndex)` - 일반 적 스폰 (풀 사용)
  - `SpawnBoss(string bossStatId, StageStat stageStat)` - 보스 스폰 (직접 생성)
  - `Return(EnemyAI enemy)` - 적 반환 (보스는 Destroy)
  - `SpawnWithMultiplier(string statId, StageStat stageStat)` - 스테이지 배율 적용
- **오브젝트 풀 설정:**
  - `defaultCapacity`: 10
  - `maxSize`: 50
  - 풀에서 가져올 때: `SetActive(true)`
  - 풀에 반환할 때: `ResetForPool()`, `SetActive(false)`

### EnemyAI
- **기능:** 적 AI 동작 (이동, 공격 등)
- **인터페이스:** `IDamageable` 구현
- **메서드:**
  - `Initialize(EnemyStat stat)` - 스탯 초기화
  - `InitializeAsBoss(EnemyStat stat)` - 보스 초기화
  - `ResetForPool()` - 풀 반환 전 상태 리셋

### EnemyAnimation
- **기능:** 적 애니메이션 파라미터 업데이트

### EnemyHPBar (UI Layer)
- **기능:** 적 체력바 표시
- **Billboard:** 카메라 방향으로 항상 정면

### Billboard
- **기능:** 항상 카메라를 바라보는 빌보드 효과
- **용도:** EnemyHPBar에 적용

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 Object Pool을 사용했는가?
- **목적:** Instantiate/Destroy 반복으로 인한 성능 저하 방지
- **효과:** 적이 자주 스폰/사망하는 방치형 게임에서 GC 발생 최소화

### 2. 왜 보스는 풀링하지 않는가?
- **목적:** 보스는 스테이지당 1회만 등장하므로 풀링 불필요
- **구현:** `Return()` 메서드에서 `IsBoss` 확인 후 `Destroy()`

### 3. 왜 BigInteger를 사용했는가?
- **목적:** 방치형 게임의 무한 스케일링 지원
- **대상:** 체력, 공격력, 골드 보상

### 4. 왜 Billboard를 분리했는가?
- **목적:** 체력바 재사용 (다른 적에서도 사용 가능)
- **효과:** 카메라 방향 자동 추적

### 5. 왜 스테이지 배율을 EnemySpawner에서 적용하는가?
- **목적:** Repository/Manager Layer 책임 분리
- **효과:** 원본 스탯 유지 (스테이지 배율은 런타임에만 적용)

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
**EnemySpawner:**
- `EnemyPrefab`: 일반 적 프리팹 할당
- `BossPrefab`: 보스 프리팹 할당 (선택 사항)
- `Spawn Points`: 적 스폰 위치 (Transform 배열)
- `Boss Spawn Point`: 보스 스폰 위치
- `Pool Settings`: `Default Capacity` (기본 10), `Max Size` (기본 50)

**EnemyAI:**
- 적 프리팹의 EnemyAI 컴포넌트 설정

**EnemyHPBar:**
- `HealthBarImage`: HP 바 이미지 할당
- Billboard 컴포넌트 추가

### 초기화 방법
1. 씬에 EnemySpawner 프리팹 배치
2. Spawn Points 설정
3. EnemyPrefab/BossPrefab 할당
4. BGDatabase에 EnemyStat/BossStat 테이블 데이터 입력
5. 스크립트에서 `EnemySpawner.Instance.Spawn()` 호출

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **보스 오브젝트 풀 버그 (중요)**
   - **위치:** `EnemySpawner.Return()`
   - **문제:** 보스는 `Destroy()` 처리되지만, `IsBoss` 속성 확인 로직이 충분하지 않으면 풀에 반환 시도 가능
   - **해결:** 이미 `IsBoss` 확인 로직이 있어 안전함

2. **NullReference 가능성**
   - **위치:** `Spawn()` 메서드
   - **문제:** `_spawnPoints` 또는 `_repository`가 null일 때
   - **방어:** 이미 null 체크가 있어 안전함

### 성능 이슈
1. **Billboard Update**
   - **위치:** `Billboard.cs`의 `Update()`에서 `transform.LookAt(Camera.main.transform)`
   - **문제:** 매 프레임 실행
   - **개선:** 카메라 이동 시에만 갱신

2. **Object Pool Max Size 초과**
   - **위치:** `EnemySpawner`의 `_maxSize = 50`
   - **문제:** 동시에 50마리 이상의 적이 필요할 때 추가 Instantiate 발생
   - **개선:** `maxSize`를 Inspector에서 조절 가능

### Refactoring 제안
1. **EnemyAI와 BossAI 분리**
   - **현재:** 하나의 EnemyAI에서 `IsBoss` 플래그로 구분
   - **개선:** 상속 구조로 분리 (BossAI : EnemyAI)

2. **스테이지 배율 계산 로직 분리**
   - **현재:** `EnemySpawner.ApplyMultiplier()`
   - **개선:** 별도 Utility 클래스로 분리

3. **EnemyHPBar 재사용성 강화**
   - **현재:** Enemy 전용
   - **개선:** IDamageable 인터페이스 기반으로 PlayerHPBar와 통합

### 코드 스타일
- ✅ BigInteger 사용 올바름
- ✅ Object Pool 패턴 올바르게 구현
- ✅ 네이밍 명확함
- ⚠️ `MultiplyBigInteger()` 메서드의 정밀도 계산(1000 단위)이 복잡할 수 있음
