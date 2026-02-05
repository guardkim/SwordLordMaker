# Manager 간 의존성 주입(DI) 리팩토링 계획서

## 작성일: 2026-02-04 (v2.0 - ServiceLocator 운영 규칙 추가)

---

## 1. 목표

### 1.1 이번 작업 목표

1. **Manager 간 `.Instance` 직접 참조 제거/최소화**
   - 인터페이스(포트) 기반 접근으로 전환
   - ServiceLocator 패턴을 통한 의존성 획득

2. **요청 성격의 통신 → 인터페이스 메서드 호출**
   - "해줘" 요청은 주입된 인터페이스 메서드로 처리
   - "일어났다" 사실 알림은 이벤트로 유지

3. **의존성 연결 단일 지점화**
   - ServiceLocator + ServiceInstaller/SceneServiceInstaller 패턴 적용

4. **SRP 위반 해결**
   - EnemySpawner에서 보상 지급 로직 분리 → EnemyRewardHandler

### 1.2 비목표 (이번 작업에서 제외)

- ❌ UI 스크립트 전면 마이그레이션
- ❌ ServiceDependentUI 베이스 클래스 도입
- ❌ SoundManager, EffectManager 등 대규모 SRP 분해
- ❌ 로직/밸런스/데이터 값 변경

---

## 2. ServiceLocator 운영 규칙 (상세)

### 2.1 서비스 등록/초기화 순서와 Ready 신호 타이밍

#### 2.1.1 Unity 라이프사이클과 등록 타이밍

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Unity 라이프사이클                                │
├─────────────────────────────────────────────────────────────────────┤
│ 1. Awake (모든 오브젝트)                                            │
│    ├─ DontDestroySingleton.Awake() → Initialize() → Register       │
│    └─ Singleton.Awake() → Initialize() → Register                  │
│                                                                     │
│ 2. OnEnable (모든 오브젝트)                                         │
│                                                                     │
│ 3. Start (모든 오브젝트)                                            │
│    ├─ ServiceInstaller.Start() → ServiceLocator.MarkAsReady()      │
│    └─ 각 Manager.Start() → InitializeDependencies() → Resolve      │
│                                                                     │
│ 4. Update 루프 시작                                                 │
└─────────────────────────────────────────────────────────────────────┘
```

#### 2.1.2 등록 순서 보장 전략

| 단계 | 시점 | 수행 작업 | 책임 클래스 |
|------|------|----------|-------------|
| 1 | Awake | 자기 자신을 ServiceLocator에 등록 | 각 Manager (Initialize 내부) |
| 2 | Start 초반 | `ServiceLocator.MarkAsReady()` 호출 | ServiceInstaller |
| 3 | Start | 필요한 서비스 Resolve | 각 Manager (InitializeDependencies) |

#### 2.1.3 Ready 신호 사용 패턴

```csharp
// 방법 1: Start()에서 직접 Resolve (권장 - 대부분의 경우)
private void Start()
{
    InitializeDependencies();
    SubscribeToEvents();
}

// 방법 2: Ready 이벤트 구독 (특수 케이스 - 동적 생성 객체)
private void Awake()
{
    if (ServiceLocator.IsReady)
    {
        InitializeDependencies();
    }
    else
    {
        ServiceLocator.OnServicesReady += InitializeDependencies;
    }
}

private void OnDestroy()
{
    ServiceLocator.OnServicesReady -= InitializeDependencies;
}
```

#### 2.1.4 Script Execution Order 설정 (권장)

```
-100: ServiceInstaller (전역 서비스 준비)
  0 : 기본값 (대부분의 Manager, 컴포넌트)
+100: SceneServiceInstaller (씬별 서비스 검증)
```

### 2.2 Resolve 폴백 정책

#### 2.2.1 폴백 정책 유형

| 정책 | 설명 | 사용 시점 |
|------|------|----------|
| **Allow (허용)** | ServiceLocator 실패 시 기존 싱글톤 사용 | 전환기 (현재) |
| **Warn (경고)** | 폴백 사용 시 Debug.LogWarning 출력 | 전환기 후반 |
| **Deny (거부)** | 폴백 없이 null 반환, 호출자가 처리 | 완전 전환 후 |
| **Assert (단언)** | 폴백 실패 시 즉시 에러, 게임 중단 | 필수 서비스 |

#### 2.2.2 현재 적용 정책: Allow + Warn 준비

```csharp
private void InitializeDependencies()
{
    // 1차: ServiceLocator에서 획득 시도
    _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();

    // 2차: 폴백 (전환기 한정)
    if (_enemySpawner == null && EnemySpawner.Instance)
    {
        _enemySpawner = EnemySpawner.Instance;
        // TODO: 전환 완료 후 아래 주석 해제
        // Debug.LogWarning($"[{GetType().Name}] IEnemySpawner fallback to singleton");
    }

    // 3차: 필수 서비스 검증 (Assert 정책 적용 대상)
    // Debug.Assert(_enemySpawner != null, "IEnemySpawner is required!");
}
```

#### 2.2.3 서비스별 폴백 정책 매트릭스

| 서비스 | 현재 정책 | 목표 정책 | 필수 여부 |
|--------|----------|----------|----------|
| IEnemySpawner | Allow | Deny | O (StageManager) |
| IStageService | Allow | Deny | O (GameManager) |
| IGameService | Allow | Warn | △ (선택적) |
| ICurrencyService | Allow | Deny | O (RewardHandler) |
| IPlayerStatService | Allow | Deny | O (RewardHandler) |

#### 2.2.4 폴백 제거 로드맵

```
Phase 1 (현재): Allow - 모든 서비스 폴백 허용
Phase 2 (다음): Warn - 폴백 시 경고 로그 출력
Phase 3 (완료): Deny/Assert - 폴백 제거, 순수 DI
```

### 2.3 의존성 가시성 규칙 (InitializeDependencies 패턴)

#### 2.3.1 패턴 구조

```csharp
public class SomeManager : Singleton<SomeManager>, ISomeService
{
    // 1. 인터페이스 타입으로 필드 선언 (private)
    private IEnemySpawner _enemySpawner;
    private ICurrencyService _currencyService;

    // 2. Initialize()에서는 자기 자신만 등록
    protected override void Initialize()
    {
        ServiceLocator.Register<ISomeService>(this);
    }

    // 3. Start()에서 의존성 획득 (InitializeDependencies 호출)
    private void Start()
    {
        InitializeDependencies();
        SubscribeToEvents();
    }

    // 4. 의존성 획득 로직을 별도 메서드로 분리
    private void InitializeDependencies()
    {
        _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();
        _currencyService = ServiceLocator.Resolve<ICurrencyService>();

        // 폴백 (전환기)
        if (_enemySpawner == null && EnemySpawner.Instance)
        {
            _enemySpawner = EnemySpawner.Instance;
        }
    }

    // 5. OnDestroy에서 정리
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        ServiceLocator.Unregister<ISomeService>();
    }
}
```

#### 2.3.2 규칙 요약

| 규칙 | 설명 |
|------|------|
| **필드는 인터페이스 타입** | `private IEnemySpawner _enemySpawner;` |
| **Resolve는 Start()에서** | Awake보다 늦게, 모든 등록 완료 후 |
| **InitializeDependencies 분리** | 가독성, 테스트 용이성 |
| **폴백은 명시적으로** | 주석과 함께 폴백 로직 작성 |
| **null 체크 후 사용** | `_enemySpawner?.SpawnEnemy()` |

#### 2.3.3 금지 패턴

```csharp
// ❌ 금지: Awake에서 Resolve (등록 순서 보장 안됨)
protected override void Initialize()
{
    _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>(); // WRONG
}

// ❌ 금지: Update에서 매번 Resolve (성능 문제)
private void Update()
{
    var spawner = ServiceLocator.Resolve<IEnemySpawner>(); // WRONG
    spawner.SpawnEnemy(...);
}

// ❌ 금지: 구현 타입으로 필드 선언 (결합도 증가)
private EnemySpawner _enemySpawner; // WRONG - 인터페이스 사용해야 함
```

### 2.4 씬 전환/리로드 안전성

#### 2.4.1 서비스 수명 분류

| 분류 | 수명 | 예시 | 등록/해제 시점 |
|------|------|------|---------------|
| **전역 (Global)** | 앱 전체 | GameManager, CurrencyManager | 앱 시작/종료 |
| **씬별 (Scene)** | 씬 단위 | StageManager, EnemySpawner | 씬 로드/언로드 |
| **동적 (Dynamic)** | 객체 단위 | EnemyRewardHandler | 객체 생성/파괴 |

#### 2.4.2 씬 전환 시 처리 흐름

```
┌─────────────────────────────────────────────────────────────────────┐
│ 씬 A 언로드                                                         │
├─────────────────────────────────────────────────────────────────────┤
│ 1. Singleton들 OnDestroy() 호출                                     │
│    └─ ServiceLocator.Unregister<ISomeService>() 자동 실행           │
│                                                                     │
│ 2. DontDestroySingleton들은 유지 (Unregister 안함)                  │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 씬 B 로드                                                           │
├─────────────────────────────────────────────────────────────────────┤
│ 1. 새 Singleton들 Awake() → Initialize() → Register                │
│                                                                     │
│ 2. SceneServiceInstaller.Start() → 씬별 서비스 검증                 │
│                                                                     │
│ 3. 각 컴포넌트 Start() → InitializeDependencies()                  │
│    └─ 전역 서비스: 이미 등록됨 (Resolve 성공)                       │
│    └─ 씬 서비스: 새로 등록됨 (Resolve 성공)                         │
└─────────────────────────────────────────────────────────────────────┘
```

#### 2.4.3 씬 리로드 시 주의사항

```csharp
// DontDestroySingleton: 씬 리로드 시에도 유지
public class GameManager : DontDestroySingleton<GameManager>, IGameService
{
    // OnDestroy에서 Unregister 하되,
    // DontDestroyOnLoad 객체는 앱 종료 시에만 호출됨
    private void OnDestroy()
    {
        ServiceLocator.Unregister<IGameService>();
    }
}

// Singleton: 씬 리로드 시 재등록
public class StageManager : Singleton<StageManager>, IStageService
{
    // 씬 언로드 시 자동으로 OnDestroy → Unregister
    // 씬 로드 시 새 인스턴스 Awake → Initialize → Register
}
```

#### 2.4.4 중복 등록 방지

```csharp
public static class ServiceLocator
{
    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (s_services.ContainsKey(type))
        {
            // 옵션 1: 경고 후 덮어쓰기 (현재)
            Debug.LogWarning($"[ServiceLocator] {type.Name} already registered. Replacing.");
        }
        s_services[type] = service;
    }
}
```

---

## 3. ISP(Interface Segregation Principle) 설계 가이드

### 3.1 인터페이스 분리 원칙

#### 3.1.1 기본 분류: Commands / State / Events

| 분류 | 설명 | 예시 |
|------|------|------|
| **Commands** | "해줘" 요청, 상태 변경 | `SpawnEnemy()`, `AddGold()` |
| **State (Queries)** | 현재 상태 조회, 읽기 전용 | `AliveEnemyCount`, `CurrentStageId` |
| **Events** | "일어났다" 알림, 구독/발행 | `OnEnemyDied`, `OnStageCleared` |

#### 3.1.2 분리 수준 결정 기준

```
┌─────────────────────────────────────────────────────────────────────┐
│ 질문: 이 인터페이스를 사용하는 클라이언트가 몇 종류인가?             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ 1종류 (모든 기능 사용)     → 통합 인터페이스 유지                   │
│ 2-3종류 (일부 기능만 사용) → 역할별 분리 고려                       │
│ 4종류 이상 (기능별 사용)   → 세분화 필수                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

#### 3.1.3 현재 프로젝트 적용 수준

| 인터페이스 | 분리 수준 | 이유 |
|-----------|----------|------|
| IEnemySpawner | 통합 | StageManager가 거의 전체 사용 |
| IStageService | 통합 | GameManager, UI가 대부분 사용 |
| ICurrencyService | 통합 | 단순 CRUD, 분리 불필요 |
| IGameService | 통합 | 이벤트 중심, 분리 불필요 |

### 3.2 인터페이스 설계 템플릿

#### 3.2.1 통합 인터페이스 (현재 적용)

```csharp
public interface IEnemySpawner
{
    // === State (읽기 전용 프로퍼티) ===
    int AliveEnemyCount { get; }
    bool IsBossSpawned { get; }
    bool IsBossAlive { get; }

    // === Commands (상태 변경 메서드) ===
    EnemyAI SpawnEnemy(string statId, StageStat stageStat);
    EnemyAI SpawnBoss(string bossStatId, StageStat stageStat);
    void ReturnEnemy(EnemyAI enemy);
    void ReturnAll();
    void ResetBossState();

    // === Events (알림) ===
    event Action<EnemyAI> OnEnemyDied;
    event Action<EnemyAI> OnBossDied;
    event Action<EnemyAI> OnBossSpawned;
    event Action<EnemyStat> OnEnemyDefeatedWithStat;
}
```

#### 3.2.2 분리 인터페이스 (미래 필요시)

```csharp
// 상태 조회만 필요한 클라이언트용
public interface IEnemySpawnerState
{
    int AliveEnemyCount { get; }
    bool IsBossSpawned { get; }
    bool IsBossAlive { get; }
}

// 스폰 명령만 필요한 클라이언트용
public interface IEnemySpawnerCommands
{
    EnemyAI SpawnEnemy(string statId, StageStat stageStat);
    EnemyAI SpawnBoss(string bossStatId, StageStat stageStat);
    void ReturnEnemy(EnemyAI enemy);
    void ReturnAll();
    void ResetBossState();
}

// 이벤트 구독만 필요한 클라이언트용
public interface IEnemySpawnerEvents
{
    event Action<EnemyAI> OnEnemyDied;
    event Action<EnemyAI> OnBossDied;
    event Action<EnemyAI> OnBossSpawned;
    event Action<EnemyStat> OnEnemyDefeatedWithStat;
}

// 통합 (필요시 다중 구현)
public interface IEnemySpawner : IEnemySpawnerState, IEnemySpawnerCommands, IEnemySpawnerEvents
{
}
```

### 3.3 이벤트 vs 메서드 호출 결정 기준

| 기준 | 이벤트 사용 | 메서드 호출 사용 |
|------|------------|-----------------|
| **방향성** | 발신자가 수신자 모름 | 발신자가 수신자 알고 있음 |
| **결과 필요** | 결과값 필요 없음 | 반환값 필요 |
| **다중 수신** | 여러 구독자 가능 | 단일 대상 |
| **성격** | "일어났다" (fact) | "해줘" (request) |

#### 3.3.1 현재 프로젝트 적용 예시

```csharp
// 이벤트: 사실 알림 (여러 구독자 가능, 결과 불필요)
_enemySpawner.OnBossDied += HandleBossDied;       // StageManager 구독
_enemySpawner.OnEnemyDefeatedWithStat += HandleReward; // RewardHandler 구독

// 메서드: 요청 (단일 대상, 결과 필요)
EnemyAI boss = _enemySpawner.SpawnBoss(bossStatId, stageStat); // 반환값 사용
_stageService.OnPlayerDied(); // 명시적 요청
```

---

## 4. 전환 단계별 계획

### 4.1 Phase 1: 핵심 Manager DI 전환 (완료)

#### 4.1.1 완료 항목

- [x] ServiceLocator 인프라 구축
- [x] IEnemySpawner 인터페이스 정의 및 구현
- [x] IStageService 인터페이스 정의 및 구현
- [x] IGameService 인터페이스 정의 및 구현
- [x] ICurrencyService 인터페이스 정의 및 구현
- [x] IPlayerStatService 인터페이스 정의 및 구현
- [x] EnemyRewardHandler 분리 (SRP)
- [x] StageManager ↔ EnemySpawner 결합 제거
- [x] GameManager ↔ StageManager 결합 제거

### 4.2 Phase 2: 보조 Manager DI 전환 (다음)

#### 4.2.1 대상

| Manager | 인터페이스 | 의존 대상 |
|---------|-----------|----------|
| UpgradeManager | IUpgradeService | ICurrencyService |
| OfflineRewardManager | (없음) | ICurrencyService, IPlayerStatService |

#### 4.2.2 작업 내용

1. IUpgradeService 인터페이스 정의
2. UpgradeManager에서 CurrencyManager.Instance 제거
3. OfflineRewardManager에서 싱글톤 참조 제거

### 4.3 Phase 3: 폴백 제거 및 검증 (미래)

#### 4.3.1 작업 내용

1. 모든 Manager의 폴백 코드 제거
2. 폴백 → Warn 전환 (로그 출력)
3. 최종적으로 폴백 완전 제거

### 4.4 Phase 4: UI 마이그레이션 (선택적)

#### 4.4.1 작업 내용 (필요시에만)

1. ServiceDependentUI 베이스 클래스 도입
2. UI가 ServiceLocator를 통해 서비스 접근
3. 기존 Manager.Instance 참조 제거

---

## 5. 인터페이스 목록 및 정의

### 5.1 Core/Interface 폴더 구조

```
Assets/02.Scripts/Core/Interface/
├── IEnemySpawner.cs      # 적 스폰/관리
├── IStageService.cs       # 스테이지 진행
├── IGameService.cs        # 게임 상태 관리
├── ICurrencyService.cs    # 재화 관리
├── IPlayerStatService.cs  # 플레이어 스탯
└── IRewardHandler.cs      # 보상 처리
```

### 5.2 인터페이스 정의 상세

#### 5.2.1 IEnemySpawner

```csharp
public interface IEnemySpawner
{
    // State
    int AliveEnemyCount { get; }
    bool IsBossSpawned { get; }
    bool IsBossAlive { get; }

    // Commands
    EnemyAI SpawnEnemy(string statId, StageStat stageStat);
    EnemyAI SpawnBoss(string bossStatId, StageStat stageStat);
    void ReturnEnemy(EnemyAI enemy);
    void ReturnAll();
    void ResetBossState();

    // Events
    event Action<EnemyAI> OnEnemyDied;
    event Action<EnemyAI> OnBossDied;
    event Action<EnemyAI> OnBossSpawned;
    event Action<EnemyStat> OnEnemyDefeatedWithStat;
}
```

#### 5.2.2 IStageService

```csharp
public interface IStageService
{
    // State
    int CurrentStageId { get; }
    string CurrentStageName { get; }
    StageStat CurrentStageStat { get; }

    // Commands
    void StartGame();
    void StartStage(int stageId);
    void SpawnBoss();
    void RestartCurrentStage();
    void RestartFromStage(int stageId);
    void OnPlayerDied();

    // Events
    event Action<int> OnStageStarted;
    event Action<int> OnStageCleared;
    event Action OnAllStagesCleared;
    event Action<EnemyAI> OnBossSpawned;
}
```

#### 5.2.3 IGameService

```csharp
public interface IGameService
{
    // Commands
    void RegisterPlayer(PlayerHealth player);
    void UnregisterPlayer();

    // Events
    event Action OnPlayerDeath;
    event Action OnPlayerRevive;
    event Action<int> OnRequestStageRestart;
}
```

#### 5.2.4 ICurrencyService

```csharp
public interface ICurrencyService
{
    // State
    double Gold { get; }
    double Ruby { get; }

    // Commands
    void AddGold(double amount);
    void AddRuby(double amount);
    bool TrySpendGold(double amount);
    bool TrySpendRuby(double amount);
    double GetCurrency(ECurrencyType type);

    // Events
    event Action<ECurrencyType, double> OnCurrencyChanged;
}
```

#### 5.2.5 IPlayerStatService

```csharp
public interface IPlayerStatService
{
    // State
    int Level { get; }
    double CurrentExp { get; }
    double MaxExp { get; }
    double BaseMaxHealth { get; }
    float BaseMoveSpeed { get; }

    // Commands
    void AddExp(double amount);

    // Events
    event Action<int> OnLevelUp;
    event Action<double, double> OnExpChanged;
}
```

---

## 6. 회귀 테스트 체크리스트

### 6.1 스테이지 시스템

- [ ] 게임 시작 시 스테이지 1 자동 시작
- [ ] 적 1초 간격 스폰 정상 동작
- [ ] 보스 스폰 버튼 클릭 시 보스 등장
- [ ] 보스 처치 시 스테이지 클리어 이벤트 발생
- [ ] 스테이지 클리어 후 다음 스테이지 자동 전환 (5초 딜레이)
- [ ] 마지막 스테이지 클리어 후 재시작

### 6.2 보상 시스템

- [ ] 일반 적 처치 시 골드 지급
- [ ] 일반 적 처치 시 경험치 지급
- [ ] 보스 처치 시 골드 지급
- [ ] 보스 처치 시 경험치 지급
- [ ] 재화 UI 즉시 갱신

### 6.3 플레이어 사망/부활

- [ ] 플레이어 사망 시 모든 적 제거
- [ ] 5초 후 플레이어 부활
- [ ] 부활 후 스테이지 1에서 재시작
- [ ] 부활 후 스폰 재개

### 6.4 UI 동작 (변경 없음 확인)

- [ ] StageUI: 스테이지 번호/이름 표시
- [ ] BossSpawnUI: 보스 스폰 버튼 동작
- [ ] CurrencyUI: 골드/루비 표시 및 갱신
- [ ] 기존 이벤트 구독 방식 정상 동작

### 6.5 씬 전환 테스트

- [ ] MainScene → StartScene 전환 후 복귀 시 정상 동작
- [ ] 씬 리로드 시 서비스 재등록 정상
- [ ] DontDestroySingleton 서비스 유지 확인

---

## 7. 남은 `.Instance` 참조 목록

### 7.1 이번 작업에서 제거된 참조 (8개)

| 파일 | 기존 코드 | 변경 후 |
|------|----------|--------|
| StageManager.cs | `EnemySpawner.Instance?.AliveEnemies.Count` | `_enemySpawner?.AliveEnemyCount` |
| StageManager.cs | `EnemySpawner.Instance.OnBossDiedEvent` | `_enemySpawner.OnBossDied` |
| StageManager.cs | `EnemySpawner.Instance?.ResetBossState()` | `_enemySpawner?.ResetBossState()` |
| StageManager.cs | `EnemySpawner.Instance.SpawnWithMultiplier()` | `_enemySpawner.SpawnEnemy()` |
| StageManager.cs | `EnemySpawner.Instance.SpawnBoss()` | `_enemySpawner.SpawnBoss()` |
| StageManager.cs | `EnemySpawner.Instance?.ReturnAll()` | `_enemySpawner?.ReturnAll()` |
| GameManager.cs | `StageManager.Instance.OnPlayerDied()` | `_stageService?.OnPlayerDied()` |
| EnemySpawner.cs | `CurrencyManager/PlayerStatManager.Instance` | 이벤트 → RewardHandler |

### 7.2 아직 남은 `.Instance` 참조

| 파일 | 대상 | 우선순위 | 비고 |
|------|------|---------|------|
| UpgradeManager.cs | CurrencyManager | 높음 | 골드 소비 요청 |
| OfflineRewardManager.cs | CurrencyManager, PlayerStatManager | 중간 | 보상 지급 |
| PlayerHealth.cs | PlayerStatManager, UpgradeManager, GameManager | 낮음 | Player 컴포넌트 |
| PlayerMovement.cs | PlayerStatManager, UpgradeManager | 낮음 | Player 컴포넌트 |
| BossSpawnUI.cs | StageManager, FadeManager | 제외 | UI |
| StageUI.cs | StageManager | 제외 | UI |
| CurrencyUI.cs | CurrencyManager | 제외 | UI |
| UpgradeSlotUI.cs | UpgradeManager, CurrencyManager | 제외 | UI |

### 7.3 남은 작업 우선순위

```
1순위: UpgradeManager ↔ CurrencyManager DI 전환
2순위: OfflineRewardManager 의존성 정리
3순위: (선택) Player 컴포넌트들 DI 전환
4순위: (미래) UI 전면 마이그레이션
```

---

## 8. 위험 요소 및 롤백 계획

### 8.1 위험 요소

| 위험 | 심각도 | 발생 조건 | 완화 방안 |
|------|--------|----------|----------|
| **서비스 등록 순서 문제** | 높음 | Awake 순서 비결정적 | Start()에서 Resolve, Script Execution Order |
| **씬 전환 시 null 참조** | 중간 | 씬별 서비스 미등록 | 폴백 패턴, null 체크 |
| **순환 의존성** | 높음 | A→B→A 형태 | ISP 분리, 이벤트 사용 |
| **성능 저하** | 낮음 | Dictionary 조회 오버헤드 | Start()에서 1회 캐싱 |
| **기존 코드 호환성** | 중간 | UI의 .Instance 사용 | 폴백 유지, 점진적 전환 |

### 8.2 롤백 계획

#### 8.2.1 롤백 트리거 조건

- 핵심 게임플레이 (스폰, 보상, 스테이지 진행) 정상 동작 불가
- 빌드 실패 또는 런타임 크래시 다수 발생
- 성능 저하가 체감될 정도로 심각

#### 8.2.2 롤백 방법

```
1. Git에서 리팩토링 이전 커밋으로 복원
   git checkout <commit-before-refactor> -- Assets/02.Scripts/

2. 또는 폴백 코드 활성화
   - 모든 InitializeDependencies()에서 폴백만 사용
   - ServiceLocator.Resolve 주석 처리
```

#### 8.2.3 부분 롤백 (특정 Manager만)

```csharp
// 문제 Manager만 기존 방식으로 복원
private void InitializeDependencies()
{
    // ServiceLocator 비활성화, 폴백만 사용
    // _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();

    if (EnemySpawner.Instance)
    {
        _enemySpawner = EnemySpawner.Instance;
    }
}
```

### 8.3 커밋 전략

```
1. 인프라 커밋: ServiceLocator, 인터페이스 정의
2. Manager별 커밋: 각 Manager 수정을 개별 커밋
3. Handler 커밋: EnemyRewardHandler 분리
4. 테스트 후 병합: 모든 테스트 통과 확인 후 main 병합
```

---

## 9. 아키텍처 다이어그램

### 9.1 변경 전 (직접 참조)

```
┌─────────────────┐
│  StageManager   │
└────────┬────────┘
         │ EnemySpawner.Instance (직접 호출)
         ▼
┌─────────────────┐
│  EnemySpawner   │──► CurrencyManager.Instance (보상)
└─────────────────┘──► PlayerStatManager.Instance (경험치)
```

### 9.2 변경 후 (인터페이스 기반)

```
┌─────────────────┐
│  StageManager   │
│  (IStageService)│
└────────┬────────┘
         │ IEnemySpawner (인터페이스)
         ▼
┌─────────────────┐      이벤트      ┌─────────────────────┐
│  EnemySpawner   │ ───────────────► │ EnemyRewardHandler  │
│ (IEnemySpawner) │                  │  (IRewardHandler)   │
└─────────────────┘                  └──────────┬──────────┘
                                               │
                        ┌──────────────────────┼──────────────────────┐
                        ▼                                             ▼
                ┌─────────────────┐                         ┌─────────────────┐
                │ ICurrencyService│                         │IPlayerStatService│
                │ (CurrencyManager)                         │(PlayerStatManager)
                └─────────────────┘                         └─────────────────┘
```

### 9.3 ServiceLocator 구조

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ServiceLocator                               │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  Dictionary<Type, object> s_services                          │ │
│  │  ├─ IEnemySpawner    → EnemySpawner                           │ │
│  │  ├─ IStageService    → StageManager                           │ │
│  │  ├─ IGameService     → GameManager                            │ │
│  │  ├─ ICurrencyService → CurrencyManager                        │ │
│  │  └─ IPlayerStatService → PlayerStatManager                    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  Register<T>(T service)   Resolve<T>()   Unregister<T>()           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 10. 결론

### 10.1 이번 작업 요약

| 항목 | 수량 |
|------|------|
| 신규 인터페이스 | 6개 |
| 수정된 Manager | 5개 |
| 분리된 Handler | 1개 (EnemyRewardHandler) |
| 제거된 `.Instance` 참조 | 8개소 |

### 10.2 달성 효과

1. **StageManager ↔ EnemySpawner 결합도 감소**
   - 직접 참조 5개 → 인터페이스 포트 1개

2. **SRP 준수**
   - EnemySpawner: 스폰만 담당
   - EnemyRewardHandler: 보상만 담당

3. **테스트 가능성 향상**
   - Mock 객체 주입 가능

4. **확장성 확보**
   - 새 보상 시스템 추가 시 RewardHandler 확장만으로 가능

### 10.3 미완료 (후속 작업)

- UI 스크립트 마이그레이션 (의도적 제외)
- UpgradeManager, OfflineRewardManager DI 전환
- 폴백 코드 완전 제거 (Phase 3)

---

## 부록 A: ServiceLocator 구현 코드

```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> s_services = new();
    private static bool s_isReady;

    public static bool IsReady => s_isReady;
    public static event Action OnServicesReady;

    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (s_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] {type.Name} already registered. Replacing.");
        }
        s_services[type] = service;
    }

    public static void Unregister<T>() where T : class
    {
        s_services.Remove(typeof(T));
    }

    public static T Resolve<T>() where T : class
    {
        return s_services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        service = Resolve<T>();
        return service != null;
    }

    public static void MarkAsReady()
    {
        s_isReady = true;
        OnServicesReady?.Invoke();
    }

    public static void Clear()
    {
        s_services.Clear();
        s_isReady = false;
    }
}
```

---

**문서 끝**
