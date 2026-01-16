# [파일명: Stage_Documentation.md]

## 1. 📂 모듈 개요 (Module Overview)

이 모듈은 게임 스테이지의 데이터 관리와 진행 흐름을 담당합니다. 스테이지별 배율(체력, 공격력, 속도 등)을 정의하고, 스테이지 시작/클리어 이벤트를 발행합니다.

**주요 스크립트:**
- `StageStat.cs` (Data Layer) - 스테이지 데이터 모델
- `IStageRepository.cs` - 스테이지 저장소 인터페이스
- `StageRepository.cs` (Repository Layer) - BGDatabase 연동
- `StageManager.cs` (Manager Layer) - 스테이지 진행 관리

---

## 2. 🏗️ 아키텍처 및 상호작용 (Architecture & Interactions)

### Script Flow
```
StageManager (Singleton, Manager Layer)
    └─> StageRepository (Repository Layer) → BGDatabase
    └─> StageStat (Data Layer) - 배율 데이터
    └─> OnStageStarted, OnStageCleared (Event)
        └─> EnemySpawner, GameManager, UI 구독
```

### Dependencies
- **외부 시스템:**
  - `BGDatabase`: 스테이지 데이터 영속화
  - `EnemySpawner`: 스테이지 배율 적용된 적 스폰
  - `GameManager`: 스테이지 시작/클리어 통보

### Diagram Description
```
┌─────────────────────────────────────────────┐
│           StageManager                     │
│        (DontDestroySingleton)             │
│  - StartStage(int)                         │
│  - ClearStage()                            │
│  - OnStageStarted, OnStageCleared (Event)   │
└────────────┬──────────────────────────────┘
             │
             ▼
    ┌────────────────┐
    │ StageRepo      │
    │  (Repository)  │
    └────────────────┘
             │
             ▼
    ┌────────────────┐
    │  StageStat     │
    │  - StageId     │
    │  - EnemyStatId │
    │  - BossStatId  │
    │  - 배율들      │
    └────────────────┘
             │
             ▼ 배율 전달
    ┌────────────────┐
    │ EnemySpawner   │
    │ SpawnWithMultiplier()│
    └────────────────┘
```

---

## 3. 📝 상세 코드 분석 (Detailed Code Analysis)

### StageStat (Data Layer)
- **기능:** 불변 데이터 클래스 (record)
- **주요 필드:**
  - `StageId`: 스테이지 번호 (int)
  - `StageName`: 스테이지 이름 (string)
  - `EnemyStatId`: 스폰할 적 타입 (string)
  - `BossStatId`: 보스 스탯 ID (string)
  - 배율들 (float): `HpMultiplier`, `AttackMultiplier`, `SpeedMultiplier`, `GoldMultiplier`, `ExpMultiplier`
- **용도:** BGDatabase의 StageStat 테이블과 매핑

### IStageRepository
- **기능:** 스테이지 저장소 인터페이스
- **메서드:** `GetById(int stageId)`, `LoadAll()`

### StageRepository (Repository Layer)
- **기능:** BGDatabase에서 StageStat 로드
- **테이블:** `StageStat` 테이블

### StageManager (Manager Layer)
- **디자인 패턴:** Singleton
- **핵심 메서드:**
  - `StartStage(int stageId)` - 스테이지 시작
  - `ClearStage()` - 스테이지 클리어
  - `GetStageStat(int stageId)` - 스테이지 데이터 조회
- **이벤트:**
  - `OnStageStarted(int stageId)` - 스테이지 시작 시 발행
  - `OnStageCleared(int stageId)` - 스테이지 클리어 시 발행

---

## 4. 💡 설계 의도 및 구현 이유 (Design Rationale)

### 1. 왜 배율(float)을 사용했는가?
- **목적:** 스테이지별 난이도 조절
- **효과:** 적 스탯에 곱셈으로 일관된 난이도 상승

### 2. 왜 이벤트 기반 통신을 사용했는가?
- **목적:** StageManager와 다른 시스템의 결합도 감소
- **효과:** 스테이지 시작/클리어 시 UI, EnemySpawner, GameManager 등 여러 시스템이 알림 수신

### 3. 왜 StageStat를 record로 정의했는가?
- **목적:** 불변 데이터 보장
- **효과:** 스테이지 데이터 무결성 보장

---

## 5. 🎮 사용 가이드 (Usage Guide within Unity)

### Inspector 설정
**StageManager:**
- 특별 설정 없음 (자동 초기화)

### 초기화 방법
1. 씬에 StageManager 프리팹 배치
2. BGDatabase에 `StageStat` 테이블 생성
3. 테이블에 스테이지 데이터 입력 (StageId, EnemyStatId, 배율 등)
4. 스크립트에서 `StageManager.Instance.StartStage(stageId)` 호출

### 사용 예시
```csharp
// 스테이지 시작
StageManager.Instance.StartStage(1);

// 스테이지 클리어
StageManager.Instance.ClearStage();

// 이벤트 구독
StageManager.Instance.OnStageStarted += (stageId) =>
{
    Debug.Log($"스테이지 {stageId} 시작");
};

StageManager.Instance.OnStageCleared += (stageId) =>
{
    Debug.Log($"스테이지 {stageId} 클리어");
};
```

---

## 6. ⚠️ 크리티컬 리뷰 및 개선점 (Critical Review)

### 잠재적 오류
1. **NullReference 가능성**
   - **위치:** `StageManager.cs`
   - **문제:** `StageRepository`가 null일 때
   - **방어:** null 체크가 필요함

### 성능 이슈
1. **배율 계산 반복**
   - **위치:** `EnemySpawner.ApplyMultiplier()`
   - **문제:** 매 스폰 시 계산
   - **개선:** 스테이지 시작 시 미리 계산 후 캐싱

### Refactoring 제안
1. **StageManager와 EnemySpawner의 통합**
   - **현재:** StageManager가 스테이지 이벤트만 발행, EnemySpawner가 배율 적용
   - **개선:** StageManager에서 직접 배율 적용된 적 스폰

2. **스테이지 진행 상태 관리 분리**
   - **현재:** StageManager가 스테이지 진행 관리
   - **개선:** `GameStateManager` 등 별도 클래스로 분리

3. **배율 데이터 타입 개선**
   - **현재:** float 배율
   - **개선:** decimal 사용 (정밀도 향상)

### 코드 스타일
- ✅ record 타입 사용 올바름
- ✅ 이벤트 기반 통신 올바름
- ✅ 네이밍 명확함
- ⚠️ 배율 계산 로직이 EnemySpawner에 있어 책임이 모호함
