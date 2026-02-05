# Phase 6: StageManager와 EnemySpawner 책임 분리 작업계획서

---

## 0. 접근 방식 비교 및 권장안

### 0.1 프로젝트 현황 분석

현재 프로젝트의 매니저 간 통신 패턴을 분석한 결과:

| 패턴 | 사용 빈도 | 주요 용도 |
|------|----------|----------|
| **이벤트 기반 (Action/event)** | ⭐⭐⭐⭐⭐ 매우 높음 | Manager → UI 알림, 상태 변경 브로드캐스트 |
| **직접 호출 (싱글톤)** | ⭐⭐⭐⭐⭐ 매우 높음 | Manager → Manager 동작 요청 |
| **인터페이스** | ⭐⭐ 낮음 | Repository 계층에만 사용 |
| **메시지 버스** | ☆ 없음 | 미사용 |

**프로젝트 이벤트 현황**: 32개의 public event 선언

```
GameManager: OnPlayerDeath, OnPlayerRevive, OnRequestStageRestart
PlayerStatManager: OnLevelUp, OnExpChanged
CurrencyManager: OnCurrencyChanged
UpgradeManager: OnUpgraded, OnInitialized
StageManager: OnStageStarted, OnStageCleared, OnAllStagesCleared, OnBossSpawned
EnemySpawner: OnEnemyDiedEvent, OnBossDiedEvent
...등 총 32개
```

### 0.2 접근 방식 비교

#### 방식 A: 인터페이스 기반 (DIP)

```csharp
// StageManager가 인터페이스에 의존
public class StageManager : Singleton<StageManager>
{
    private IEnemySpawner _spawner;
    private IEnemyManager _enemyManager;

    private void InitializeDependencies()
    {
        var spawner = EnemySpawner.Instance;
        _spawner = spawner;
        _enemyManager = spawner;
    }

    private void SpawnEnemy(StageStat stage)
    {
        _spawner?.SpawnEnemy(stage.EnemyStatId, stage);
    }
}
```

| 장점 | 단점 |
|------|------|
| ✅ 테스트 시 Mock 객체 주입 가능 | ❌ 프로젝트 패턴과 불일치 (Repository만 인터페이스 사용) |
| ✅ 컴파일 타임 타입 안전성 | ❌ 인터페이스 파일 추가 필요 (3개) |
| ✅ 명시적 계약 정의 | ❌ 결국 싱글톤에서 가져오므로 실질적 결합도 동일 |
| | ❌ DI 컨테이너 없이는 효과 제한적 |

#### 방식 B: 순수 이벤트 기반 (Observer)

```csharp
// StageManager가 EnemySpawner 이벤트만 구독
public class StageManager : Singleton<StageManager>
{
    // EnemySpawner 직접 참조 최소화
    // 필요한 통신은 이벤트로 처리

    public event Action<SpawnRequest> OnSpawnRequested;  // 스폰 요청 이벤트
    public event Action OnClearAllRequested;             // 적 정리 요청 이벤트

    private void SpawnEnemy(StageStat stage)
    {
        OnSpawnRequested?.Invoke(new SpawnRequest(stage.EnemyStatId, stage));
    }

    private void ClearAllEnemies()
    {
        OnClearAllRequested?.Invoke();
    }
}

// EnemySpawner가 StageManager 이벤트 구독
public class EnemySpawner : Singleton<EnemySpawner>
{
    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnSpawnRequested += HandleSpawnRequest;
            StageManager.Instance.OnClearAllRequested += ReturnAll;
        }
    }

    private void HandleSpawnRequest(SpawnRequest request)
    {
        SpawnWithMultiplier(request.StatId, request.StageStat);
    }
}
```

| 장점 | 단점 |
|------|------|
| ✅ **프로젝트 패턴과 완전 일치** | ❌ 반환값이 필요한 경우 처리 복잡 |
| ✅ 느슨한 결합 (완전한 디커플링) | ❌ 이벤트 흐름 추적이 어려울 수 있음 |
| ✅ 새로운 파일 추가 불필요 | ❌ 동기적 응답이 필요한 경우 부적합 |
| ✅ 확장 용이 (새 구독자 추가만) | |

#### 방식 C: 하이브리드 (이벤트 + 최소 직접 호출)

```csharp
// 핵심 원칙:
// - 명령(Command) → 직접 호출 (단, 최소화)
// - 알림(Notification) → 이벤트

public class StageManager : Singleton<StageManager>
{
    // 상태 조회용 프로퍼티 제거 (디미터 법칙 준수)
    // public int AliveEnemyCount => ... (제거)
    // public bool IsBossSpawned => ... (제거)

    // 필요한 정보는 이벤트로 수신
    private int _aliveEnemyCount;
    private bool _isBossSpawned;

    private void SubscribeToEvents()
    {
        EnemySpawner.Instance.OnEnemyCountChanged += count => _aliveEnemyCount = count;
        EnemySpawner.Instance.OnBossStateChanged += state => _isBossSpawned = state;
        EnemySpawner.Instance.OnBossDiedEvent += HandleBossDied;
    }

    // 명령은 직접 호출 (반환값 필요)
    public void SpawnBoss()
    {
        StopSpawning();
        EnemySpawner.Instance?.ReturnAll();

        EnemyAI boss = EnemySpawner.Instance?.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
        if (boss != null)
        {
            OnBossSpawned?.Invoke(boss);
        }
    }
}
```

| 장점 | 단점 |
|------|------|
| ✅ 실용적 균형 | ⚠️ 일관성 부족 가능성 |
| ✅ 반환값 필요한 경우 직접 호출 | ⚠️ 어떤 경우 이벤트/직접호출 선택 기준 필요 |
| ✅ 프로젝트 기존 패턴과 유사 | |
| ✅ 디미터 법칙 위반 해결 | |

### 0.3 프로젝트 적합성 분석

```
현재 프로젝트 통신 흐름:

[로그인 → 초기화]
PlayerSessionManager.OnLoginCompleted (이벤트)
    → PlayerStatManager, CurrencyManager, UpgradeManager 초기화

[강화 시스템]
UI → UpgradeManager.TryUpgrade() (직접 호출)
    → CurrencyManager.TrySpendGold() (직접 호출)
    → UpgradeManager.OnUpgraded (이벤트)
        → UI, PlayerMovement, PlayerHealth 갱신

[적 사망 → 보상]
EnemyAI.OnDied (이벤트)
    → EnemySpawner.HandleEnemyDied()
        → CurrencyManager.AddGold() (직접 호출) ← SRP 위반!
        → PlayerStatManager.AddExp() (직접 호출) ← SRP 위반!
```

**핵심 발견:**
1. **알림/브로드캐스트**: 이벤트 사용 (일관됨)
2. **동작 요청**: 직접 호출 사용 (일관됨)
3. **문제점**: EnemySpawner가 보상 지급까지 직접 호출 (SRP 위반)

### 0.4 권장안: 방식 C (하이브리드) + RewardHandler 분리

**이유:**

1. **프로젝트 일관성**: 기존 패턴(이벤트 + 직접호출 혼합)과 가장 유사
2. **실용성**: 인터페이스 없이도 충분한 결합도 감소 달성
3. **점진적 개선**: 기존 코드 최소 변경으로 SRP 위반 해결
4. **명확한 기준**:
   - 알림/상태변경 → 이벤트
   - 동작 요청(반환값 필요) → 직접 호출
   - 상태 조회 → 이벤트로 캐싱 또는 필요시만 직접 호출

### 0.5 결정 요약

| 구분 | 선택 | 이유 |
|------|------|------|
| **접근 방식** | 하이브리드 (방식 C) | 프로젝트 패턴 일관성 |
| **인터페이스 도입** | ❌ 불필요 | Repository 외 인터페이스 미사용 관행 |
| **이벤트 확장** | ✅ 필요 | 상태 알림용 이벤트 추가 |
| **RewardHandler 분리** | ✅ 필수 | SRP 원칙 준수 |

---

## 1. 현재 상태 분석

### 1.1 StageManager 현재 책임

| 책임 | 적절성 | 비고 |
|------|--------|------|
| 스테이지 진행 관리 | ✅ 적절 | 핵심 책임 |
| 스폰 루틴 관리 (Coroutine) | ⚠️ 경계 | 스폰 타이밍은 스테이지 책임 |
| EnemySpawner 직접 호출 | ❌ 부적절 | 결합도 높음 |
| 보스 상태 프록시 | ❌ 부적절 | EnemySpawner 상태 직접 노출 |
| 적 수 프록시 | ❌ 부적절 | EnemySpawner 상태 직접 노출 |

**문제점:**
```csharp
// 디미터 법칙 위반 - EnemySpawner 내부 상태 직접 접근
public int AliveEnemyCount => EnemySpawner.Instance?.AliveEnemies.Count ?? 0;
public bool IsBossSpawned => EnemySpawner.Instance?.IsBossSpawned ?? false;
public bool IsBossAlive => EnemySpawner.Instance?.IsBossAlive ?? false;

// 스폰 책임이 StageManager에 있음
private void SpawnEnemy(StageStat stage)
{
    EnemySpawner.Instance.SpawnWithMultiplier(stage.EnemyStatId, stage);
}
```

### 1.2 EnemySpawner 현재 책임

| 책임 | 적절성 | 비고 |
|------|--------|------|
| 적 오브젝트 풀 관리 | ✅ 적절 | 핵심 책임 |
| 적 스폰 (생성) | ✅ 적절 | 핵심 책임 |
| 보스 상태 관리 | ✅ 적절 | 스폰과 밀접 |
| 보상 지급 | ❌ 부적절 | SRP 위반 |
| 스탯 배율 적용 | ⚠️ 경계 | 스테이지 로직 혼재 |

**문제점:**
```csharp
// SRP 위반 - 보상 지급은 EnemySpawner의 책임이 아님
private void HandleEnemyDied(EnemyAI enemy, EnemyStat stat)
{
    CurrencyManager.Instance?.AddGold(stat.GoldReward);
    PlayerStatManager.Instance?.AddExp(stat.Exp);
    // ...
}
```

---

## 2. SOLID 원칙 기반 목표 설계 (하이브리드 방식)

### 2.1 Single Responsibility Principle (SRP) ⭐ 핵심

**StageManager의 단일 책임:**
- 스테이지 흐름 제어 (시작, 클리어, 전환)
- 스테이지 상태 관리
- 스테이지 이벤트 발행
- ~~EnemySpawner 상태 프록시~~ → 제거

**EnemySpawner의 단일 책임:**
- 적 오브젝트 생성/풀링
- 적 생명주기 관리 (스폰, 반환)
- 적 관련 이벤트 발행
- ~~보상 지급~~ → EnemyRewardHandler로 분리

**새로운 시스템 분리:**
- `EnemyRewardHandler`: 보상 처리 전담 (이벤트 구독 방식)

### 2.2 Open/Closed Principle (OCP)

이벤트 기반 확장으로 기존 코드 수정 없이 새 기능 추가 가능:

```csharp
// 새로운 보상 시스템 추가 시
public class AchievementRewardHandler : MonoBehaviour
{
    private void Start()
    {
        EnemySpawner.Instance.OnEnemyDiedWithStat += HandleAchievement;
    }

    private void HandleAchievement(EnemyStat stat)
    {
        // 업적 처리 로직 (기존 코드 수정 없음)
    }
}
```

### 2.3 Liskov Substitution Principle (LSP)

기존 Singleton<T> 상속 구조 유지. 인터페이스 도입 없이 이벤트로 대체.

### 2.4 Interface Segregation Principle (ISP)

**인터페이스 대신 이벤트로 필요한 기능만 노출:**

```csharp
// 기존 (인터페이스 방식) - 불필요
public interface IEnemySpawner { ... }
public interface IEnemyManager { ... }
public interface IBossStateProvider { ... }

// 권장 (이벤트 방식) - 프로젝트 패턴과 일치
public class EnemySpawner : Singleton<EnemySpawner>
{
    // 필요한 클라이언트만 구독
    public event Action<EnemyStat> OnEnemyDiedWithStat;     // 보상 처리용
    public event Action<EnemyAI> OnBossDiedEvent;           // 스테이지 진행용
    public event Action<EnemyAI> OnBossSpawnedEvent;        // UI 갱신용
}
```

### 2.5 Dependency Inversion Principle (DIP)

**이벤트 기반으로 의존성 역전 달성:**

```
Before (직접 의존):
StageManager → EnemySpawner (강한 결합)
EnemySpawner → CurrencyManager, PlayerStatManager (SRP 위반)

After (이벤트 기반):
StageManager ←(구독)← EnemySpawner.OnBossDiedEvent
EnemyRewardHandler ←(구독)← EnemySpawner.OnEnemyDiedWithStat
```

---

## 3. 상세 구현 계획 (하이브리드 방식)

### 3.1 Phase 6-A: EnemySpawner 이벤트 확장

**파일:** `Assets/02.Scripts/Enemy/Manager/EnemySpawner.cs`

**변경 사항:**
1. 보상 지급 로직 제거
2. 새로운 이벤트 추가 (스탯 포함 사망 이벤트, 보스 스폰 이벤트)

```csharp
public class EnemySpawner : Singleton<EnemySpawner>
{
    // 기존 이벤트 유지 (하위 호환)
    public event Action<EnemyAI> OnEnemyDiedEvent;
    public event Action<EnemyAI> OnBossDiedEvent;

    // 새 이벤트 추가
    public event Action<EnemyStat> OnEnemyDiedWithStat;  // 보상 처리용 (SRP)
    public event Action<EnemyAI> OnBossSpawnedEvent;     // 보스 스폰 알림

    private void HandleEnemyDied(EnemyAI enemy, EnemyStat stat)
    {
        if (stat == null) return;

        // ❌ 제거: 보상 지급 직접 호출 (SRP 위반)
        // CurrencyManager.Instance?.AddGold(stat.GoldReward);
        // PlayerStatManager.Instance?.AddExp(stat.Exp);

        // ✅ 추가: 이벤트로 위임
        OnEnemyDiedWithStat?.Invoke(stat);

        if (enemy.IsBoss)
        {
            if (enemy == _currentBoss)
            {
                _currentBoss = null;
                _bossSpawned = false;
            }
            OnBossDiedEvent?.Invoke(enemy);
        }
        else
        {
            OnEnemyDiedEvent?.Invoke(enemy);
        }
    }

    public EnemyAI SpawnBoss(string bossStatId, StageStat stageStat)
    {
        // ... 기존 스폰 로직 ...

        // ✅ 추가: 보스 스폰 이벤트 발행
        OnBossSpawnedEvent?.Invoke(boss);

        return boss;
    }
}
```

### 3.2 Phase 6-B: EnemyRewardHandler 분리 (SRP)

**파일:** `Assets/02.Scripts/Enemy/Handler/EnemyRewardHandler.cs` (신규)

```csharp
using UnityEngine;

/// <summary>
/// 적 처치 시 보상 지급을 담당하는 핸들러.
/// EnemySpawner의 SRP 위반을 해결하기 위해 분리됨.
/// </summary>
public class EnemyRewardHandler : MonoBehaviour
{
    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnEnemyDiedWithStat += HandleReward;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (EnemySpawner.HasInstance)
        {
            EnemySpawner.Instance.OnEnemyDiedWithStat -= HandleReward;
        }
    }

    private void HandleReward(EnemyStat stat)
    {
        if (stat == null) return;

        CurrencyManager.Instance?.AddGold(stat.GoldReward);
        PlayerStatManager.Instance?.AddExp(stat.Exp);
    }
}
```

**배치:** `EnemySpawner`와 같은 GameObject에 컴포넌트로 추가하거나, 별도 GameObject 생성.

### 3.3 Phase 6-C: StageManager 리팩토링

**파일:** `Assets/02.Scripts/Stage/Manager/StageManager.cs`

**변경 사항:**
1. 디미터 법칙 위반 프로퍼티 제거
2. EnemySpawner 이벤트 구독 방식 정리
3. 직접 호출은 동작 요청에만 사용

```csharp
public class StageManager : Singleton<StageManager>
{
    [Header("Settings")]
    [SerializeField] private bool _autoStartOnAwake = true;
    [SerializeField] private float _spawnInterval = 1f;
    [SerializeField] private float _stageTransitionDelay = 5f;

    private IStageRepository _repository;
    private int _currentStageId = 1;
    private int _maxStageId;

    private bool _isSpawning;
    private StageStat _currentStageStat;
    private Coroutine _spawnCoroutine;

    public int CurrentStageId => _currentStageId;
    public string CurrentStageName => _currentStageStat?.StageName ?? "";
    public StageStat CurrentStageStat => _currentStageStat;

    // ❌ 제거: 디미터 법칙 위반 프로퍼티
    // public int AliveEnemyCount => EnemySpawner.Instance?.AliveEnemies.Count ?? 0;
    // public bool IsBossSpawned => EnemySpawner.Instance?.IsBossSpawned ?? false;
    // public bool IsBossAlive => EnemySpawner.Instance?.IsBossAlive ?? false;

    // ✅ 대안: 필요한 곳에서 직접 EnemySpawner 조회 (최소화)
    // 또는 이벤트로 상태 동기화

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action OnAllStagesCleared;
    public event Action<EnemyAI> OnBossSpawned;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _maxStageId = _repository.GetMaxStageId();
    }

    private IStageRepository CreateRepository()
    {
        return new StageRepository();
    }

    private void Start()
    {
        SubscribeToEvents();

        if (_autoStartOnAwake)
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRequestStageRestart += HandleStageRestartRequest;
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnBossDiedEvent += HandleBossDied;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnRequestStageRestart -= HandleStageRestartRequest;
        }

        if (EnemySpawner.HasInstance)
        {
            EnemySpawner.Instance.OnBossDiedEvent -= HandleBossDied;
        }
    }

    // ... 나머지 메서드는 기존과 동일 ...

    // ✅ 직접 호출은 동작 요청에만 사용 (반환값 필요)
    private void SpawnEnemy(StageStat stage)
    {
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not found.");
            return;
        }

        EnemySpawner.Instance.SpawnWithMultiplier(stage.EnemyStatId, stage);
    }

    public void SpawnBoss()
    {
        if (_currentStageStat == null || string.IsNullOrEmpty(_currentStageStat.BossStatId))
        {
            Debug.LogWarning("[StageManager] No boss configured for this stage.");
            return;
        }

        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not found.");
            return;
        }

        StopSpawning();
        ClearAllEnemies();

        EnemyAI boss = EnemySpawner.Instance.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
        if (boss != null)
        {
            OnBossSpawned?.Invoke(boss);
        }
    }

    private void ClearAllEnemies()
    {
        EnemySpawner.Instance?.ReturnAll();
    }
}
```

### 3.4 Phase 6-D: UI 코드 수정 (디미터 법칙 준수)

**파일:** `Assets/02.Scripts/UI/BossSpawnUI.cs` (예시)

기존에 `StageManager.Instance.IsBossSpawned`를 사용했다면:

```csharp
// ❌ Before: StageManager 통해 간접 조회 (디미터 법칙 위반)
if (StageManager.Instance.IsBossSpawned) { ... }

// ✅ After: EnemySpawner 직접 조회 또는 이벤트 구독
if (EnemySpawner.Instance?.IsBossSpawned ?? false) { ... }

// 또는 이벤트 기반
private void Start()
{
    EnemySpawner.Instance.OnBossSpawnedEvent += OnBossSpawned;
    EnemySpawner.Instance.OnBossDiedEvent += OnBossDied;
}
```

---

## 4. 의존성 다이어그램

### 4.1 현재 구조 (Before)

```
┌─────────────────┐
│  StageManager   │
└────────┬────────┘
         │ 직접 호출 (강한 결합)
         │ + 디미터 법칙 위반 (프록시 프로퍼티)
         ▼
┌─────────────────┐
│  EnemySpawner   │──────┬──────┐
└─────────────────┘      │      │
         │               ▼      ▼
         │      CurrencyManager  PlayerStatManager
         │      (보상 지급 - SRP 위반)
         ▼
┌─────────────────┐
│    EnemyAI      │
└─────────────────┘

문제점:
1. StageManager가 EnemySpawner 내부 상태 직접 노출 (디미터 법칙 위반)
2. EnemySpawner가 보상 지급까지 담당 (SRP 위반)
3. 양방향 의존성으로 인한 결합도 증가
```

### 4.2 목표 구조 (After - 하이브리드 방식)

```
┌─────────────────┐                    ┌─────────────────┐
│  StageManager   │                    │       UI        │
└────────┬────────┘                    └────────┬────────┘
         │                                      │
         │ 동작요청(직접호출)                      │ 이벤트 구독
         │ 상태알림(이벤트구독)                    │
         ▼                                      ▼
┌─────────────────────────────────────────────────────────┐
│                      EnemySpawner                       │
│  ┌─────────────────────────────────────────────────┐    │
│  │ Events:                                         │    │
│  │   OnEnemyDiedEvent (기존)                       │    │
│  │   OnBossDiedEvent (기존)                        │    │
│  │   OnEnemyDiedWithStat (신규 - 보상용)           │    │
│  │   OnBossSpawnedEvent (신규 - UI알림)            │    │
│  └─────────────────────────────────────────────────┘    │
└────────────────────────┬────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐
│  EnemyAI    │  │ StageManager│  │ EnemyRewardHandler  │
│             │  │ (이벤트구독)│  │ (이벤트 구독)       │
└─────────────┘  └─────────────┘  └──────────┬──────────┘
                                             │
                         ┌───────────────────┼───────────────────┐
                         ▼                                       ▼
                 ┌─────────────────┐                   ┌─────────────────┐
                 │ CurrencyManager │                   │PlayerStatManager│
                 └─────────────────┘                   └─────────────────┘

개선점:
1. ✅ StageManager 프록시 프로퍼티 제거 (디미터 법칙 준수)
2. ✅ EnemySpawner 보상 로직 분리 (SRP 준수)
3. ✅ 이벤트 기반 느슨한 결합 (프로젝트 패턴 일관성)
4. ✅ 인터페이스 파일 추가 불필요 (간결함)
```

### 4.3 통신 흐름 정리

```
[적 스폰]
StageManager.SpawnEnemy()
    → EnemySpawner.SpawnWithMultiplier() (직접 호출, 반환값 필요)

[보스 스폰]
StageManager.SpawnBoss()
    → EnemySpawner.SpawnBoss() (직접 호출, 반환값 필요)
    → EnemySpawner.OnBossSpawnedEvent (이벤트 발행)
        → UI 갱신 (이벤트 구독)

[적 사망]
EnemyAI.Die()
    → EnemySpawner.HandleEnemyDied() (이벤트 구독)
        → EnemySpawner.OnEnemyDiedWithStat (이벤트 발행)
            → EnemyRewardHandler.HandleReward() (이벤트 구독)
                → CurrencyManager.AddGold() (직접 호출)
                → PlayerStatManager.AddExp() (직접 호출)

[보스 사망]
EnemyAI.Die() (보스)
    → EnemySpawner.HandleEnemyDied() (이벤트 구독)
        → EnemySpawner.OnBossDiedEvent (이벤트 발행)
            → StageManager.HandleBossDied() (이벤트 구독)
                → 스테이지 전환 처리
```

---

## 5. 파일 변경 목록 (하이브리드 방식)

| 작업 | 파일 경로 | 변경 유형 | 설명 |
|------|-----------|----------|------|
| 6-A-1 | `Assets/02.Scripts/Enemy/Manager/EnemySpawner.cs` | 수정 | 이벤트 추가, 보상 로직 제거 |
| 6-B-1 | `Assets/02.Scripts/Enemy/Handler/EnemyRewardHandler.cs` | **신규** | 보상 처리 전담 |
| 6-C-1 | `Assets/02.Scripts/Stage/Manager/StageManager.cs` | 수정 | 프록시 프로퍼티 제거, 이벤트 구독 정리 |
| 6-D-1 | `Assets/02.Scripts/UI/BossSpawnUI.cs` | 수정 | 이벤트 구독 방식으로 변경 |
| 6-D-2 | `Assets/02.Scripts/UI/StageUI.cs` | 수정 | 필요시 이벤트 구독 방식으로 변경 |

**비교:**
- 인터페이스 방식: 6개 파일 (인터페이스 3개 신규 + 기존 3개 수정)
- 하이브리드 방식: 5개 파일 (핸들러 1개 신규 + 기존 4개 수정)

---

## 6. 구현 순서 (하이브리드 방식)

```
Phase 6-A: EnemySpawner 이벤트 확장 + 보상 로직 제거
    ↓
Phase 6-B: EnemyRewardHandler 신규 생성 + 씬에 배치
    ↓
Phase 6-C: StageManager 프록시 프로퍼티 제거 + 이벤트 구독 정리
    ↓
Phase 6-D: UI 코드 수정 (필요시)
    ↓
Phase 6-E: 테스트 및 검증
```

---

## 7. 하위 호환성

기존 코드와의 호환성을 위해 다음 사항 유지:

1. `EnemySpawner.Instance` 접근 방식 유지
2. 기존 이벤트 (`OnEnemyDiedEvent`, `OnBossDiedEvent`) 유지
3. 기존 public 메서드 시그니처 유지
4. `EnemySpawner.IsBossSpawned`, `IsBossAlive`, `AliveEnemies` 프로퍼티 유지

**주의:** StageManager의 프록시 프로퍼티 제거 시 해당 프로퍼티를 사용하는 외부 코드 확인 필요.

---

## 8. 검증 항목

- [ ] 스테이지 시작 시 적 스폰 정상 동작
- [ ] 보스 스폰 버튼 정상 동작
- [ ] 보스 처치 시 스테이지 클리어 정상 동작
- [ ] 적 처치 시 보상 정상 지급 (EnemyRewardHandler 동작 확인)
- [ ] 스테이지 전환 정상 동작
- [ ] 플레이어 사망 시 적 정리 정상 동작
- [ ] UI 갱신 정상 동작 (이벤트 구독 확인)

---

## 9. 예상 효과

| 지표 | Before | After (하이브리드) |
|------|--------|-------------------|
| StageManager-EnemySpawner 결합도 | 높음 (직접 호출 + 프록시) | 중간 (필요한 직접 호출만) |
| EnemySpawner 책임 수 | 5개 | 3개 |
| 디미터 법칙 준수 | ❌ 위반 | ✅ 준수 |
| SRP 준수 | ❌ 보상 로직 혼재 | ✅ RewardHandler 분리 |
| 프로젝트 패턴 일관성 | - | ✅ 이벤트 기반 |
| 신규 파일 수 | - | 1개 (RewardHandler) |
| 확장성 | 낮음 | 높음 (이벤트 구독만 추가) |

### 방식별 비교

| 항목 | 인터페이스 방식 | 하이브리드 방식 |
|------|---------------|----------------|
| 신규 파일 | 4개 (인터페이스 3 + 핸들러 1) | 1개 (핸들러) |
| 테스트 용이성 | ⭐⭐⭐⭐⭐ Mock 완벽 지원 | ⭐⭐⭐ 이벤트 기반 테스트 |
| 프로젝트 일관성 | ⭐⭐ 기존 패턴과 불일치 | ⭐⭐⭐⭐⭐ 완전 일치 |
| 구현 복잡도 | 높음 | 낮음 |
| DI 컨테이너 필요성 | 효과 극대화에 필요 | 불필요 |

---

## 10. 추후 확장 고려사항

### 10.1 단기 (현재 리팩토링 범위)
- `EnemyRewardHandler` 분리로 SRP 준수
- StageManager 프록시 프로퍼티 제거로 디미터 법칙 준수

### 10.2 중기 (선택적 확장)
- **업적 시스템 추가 시**: `AchievementHandler`가 `OnEnemyDiedWithStat` 구독
- **통계 시스템 추가 시**: `StatisticsHandler`가 관련 이벤트 구독
- 기존 코드 수정 없이 새 핸들러 추가만으로 확장 가능 (OCP)

### 10.3 장기 (프로젝트 규모 확장 시)
1. **중앙 이벤트 버스**: 이벤트가 30개 이상 증가 시 고려
2. **DI 컨테이너 (Zenject/VContainer)**: 테스트 자동화 필요 시 고려
3. **인터페이스 도입**: 유닛 테스트 커버리지 확대 시 고려

---

## 11. 결론

**권장 방식: 하이브리드 (이벤트 + 최소 직접 호출)**

| 이유 | 설명 |
|------|------|
| 프로젝트 일관성 | 기존 32개 이벤트 사용 패턴과 완전 일치 |
| 최소 변경 | 인터페이스 파일 추가 없이 SRP 위반 해결 |
| 명확한 기준 | 알림=이벤트, 동작요청=직접호출 |
| 확장 용이 | 새 핸들러 추가만으로 기능 확장 가능 |

인터페이스 방식은 DI 컨테이너 도입 시점에 재검토하는 것이 효율적입니다.
