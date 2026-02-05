# 아키텍처 리팩토링: 의존성 주입(DI) 기반 구조 개편

## 목차
1. [현재 상태 분석](#1-현재-상태-분석)
2. [문제점 식별](#2-문제점-식별)
3. [목표 아키텍처](#3-목표-아키텍처)
4. [인터페이스 설계](#4-인터페이스-설계)
5. [서비스 로케이터 구현](#5-서비스-로케이터-구현)
6. [단계별 구현 계획](#6-단계별-구현-계획)
7. [파일 변경 목록](#7-파일-변경-목록)
8. [마이그레이션 가이드](#8-마이그레이션-가이드)
9. [검증 및 테스트](#9-검증-및-테스트)

---

## 1. 현재 상태 분석

### 1.1 Manager 클래스 현황

| Manager | 싱글톤 타입 | 의존하는 Manager | Repository | 이벤트 수 |
|---------|-----------|-----------------|------------|----------|
| GameManager | DontDestroy | StageManager | - | 3 |
| StageManager | Singleton | GameManager, EnemySpawner, SoundManager | IStageRepository | 4 |
| PlayerSessionManager | DontDestroy | - | BGDatabase 직접 | 2 |
| PlayerStatManager | DontDestroy | PlayerSessionManager | IPlayerStatRepository | 2 |
| CurrencyManager | DontDestroy | PlayerSessionManager | ICurrencyRepository | 1 |
| UpgradeManager | DontDestroy | PlayerSessionManager, CurrencyManager | IUpgradeRepository | 2 |
| EnemySpawner | Singleton | CurrencyManager, PlayerStatManager | IEnemyStatRepository, IBossStatRepository | 2 |
| SoundManager | DontDestroy | - | ISoundRepository | 0 |
| EffectManager | Singleton | QuarterViewCamera | - | 0 |
| PopupManager | DontDestroy | - | - | 3 |
| FadeManager | DontDestroy | - | - | 0 |
| RedDotManager | DontDestroy | - | - | 1 |
| OfflineRewardManager | DontDestroy | PlayerSessionManager, CurrencyManager, PlayerStatManager | IOfflineRewardRepository | 1 |

### 1.2 현재 의존성 그래프

```
                    ┌─────────────────────────────────┐
                    │       PlayerSessionManager       │
                    │   (앱 전체 생명주기 - 로그인)    │
                    └──────────────┬──────────────────┘
                                   │ OnLoginCompleted
        ┌──────────────┬───────────┼───────────┬──────────────┐
        ▼              ▼           ▼           ▼              ▼
┌───────────────┐ ┌────────────┐ ┌──────────┐ ┌───────────────┐
│PlayerStatMgr  │ │CurrencyMgr │ │UpgradeMgr│ │OfflineReward  │
└───────┬───────┘ └─────┬──────┘ └────┬─────┘ └───────────────┘
        │               │              │
        └───────────────┼──────────────┘
                        │
        ┌───────────────┴───────────────┐
        ▼                               ▼
┌───────────────┐               ┌───────────────┐
│  GameManager  │◄─────────────►│ StageManager  │
└───────┬───────┘               └───────┬───────┘
        │                               │
        ▼                               ▼
┌───────────────┐               ┌───────────────┐
│ PlayerHealth  │               │ EnemySpawner  │
└───────────────┘               └───────┬───────┘
                                        │
                        ┌───────────────┼───────────────┐
                        ▼               ▼               ▼
                ┌───────────────┐ ┌──────────┐ ┌───────────────┐
                │ CurrencyMgr   │ │PlayerStat│ │   EnemyAI     │
                │ (보상 지급)   │ │(경험치)  │ │               │
                └───────────────┘ └──────────┘ └───────────────┘
```

### 1.3 Player/Enemy 클래스 현황

**Player 계층:**
```
PlayerHealth : MonoBehaviour, IDamageable
├── 의존: PlayerStatManager, UpgradeManager, GameManager
├── 이벤트: OnHealthChanged, OnDeath
└── 컴포지션: PlayerAnimation, PlayerAutoMovement

PlayerMovement / PlayerAutoMovement : MonoBehaviour
├── 의존: PlayerStatManager, UpgradeManager
└── 이벤트 구독: OnUpgraded, OnInitialized

PlayerStatManager : DontDestroySingleton
├── 의존: PlayerSessionManager (로그인 대기)
└── Repository: IPlayerStatRepository
```

**Enemy 계층:**
```
EnemyAI : MonoBehaviour, IDamageable
├── 상태: Idle, Chase, Attack, SkillAttack, Hit, Dead
├── 이벤트: OnDied(EnemyAI, EnemyStat)
├── 의존: DamageFloaterManager, EffectManager, SoundManager
└── 컴포지션: EnemyAnimation, EnemyHPBar, EnemySkillHandler

EnemySpawner : Singleton
├── 의존: CurrencyManager, PlayerStatManager (보상 지급 - SRP 위반)
├── Repository: IEnemyStatRepository, IBossStatRepository
└── 이벤트: OnEnemyDiedEvent, OnBossDiedEvent
```

---

## 2. 문제점 식별

### 2.1 SOLID 원칙 위반

| 원칙 | 위반 사항 | 위치 |
|------|----------|------|
| **SRP** | EnemySpawner가 보상 지급까지 담당 | EnemySpawner.HandleEnemyDied() |
| **SRP** | StageManager가 EnemySpawner 상태 프록시 | StageManager.IsBossSpawned 등 |
| **OCP** | 새 Manager 추가 시 기존 코드 수정 필요 | 싱글톤 직접 참조 |
| **DIP** | 상위 모듈이 하위 모듈 구체 클래스에 의존 | 모든 .Instance 호출 |
| **ISP** | 클라이언트가 사용하지 않는 메서드에 의존 | 거대 Manager 인터페이스 |

### 2.2 구체적 문제점

#### 강한 결합 (Tight Coupling)
```csharp
// ❌ 현재: 구체 클래스 직접 참조
public class PlayerHealth : MonoBehaviour
{
    private void Start()
    {
        double baseHealth = PlayerStatManager.Instance.BaseMaxHealth;
        GameManager.Instance.RegisterPlayer(this);
        UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
    }
}
```

#### 테스트 불가능
- 싱글톤 직접 참조로 인해 유닛 테스트에서 Mock 주입 불가
- 통합 테스트만 가능

#### 순환 의존성 위험
```
GameManager → StageManager → EnemySpawner → CurrencyManager
                    ↑                              │
                    └──────────────────────────────┘
```

#### 디미터 법칙 위반
```csharp
// ❌ 현재
public int AliveEnemyCount => EnemySpawner.Instance?.AliveEnemies.Count ?? 0;
public bool IsBossSpawned => EnemySpawner.Instance?.IsBossSpawned ?? false;
```

---

## 3. 목표 아키텍처

### 3.1 핵심 원칙

1. **인터페이스 기반 추상화**: 모든 Manager는 인터페이스를 통해 접근
2. **의존성 주입**: 구체 클래스가 아닌 추상화에 의존
3. **서비스 로케이터**: Unity 환경에서의 실용적 DI 구현
4. **책임 분리**: 각 클래스는 단일 책임만 담당
5. **이벤트 기반 통신**: Manager 간 느슨한 결합 유지

### 3.2 목표 아키텍처 다이어그램

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           ServiceLocator                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Dictionary<Type, object> _services                              │   │
│  │  Register<T>(T service) / Resolve<T>() / TryResolve<T>()        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐          ┌───────────────┐          ┌───────────────┐
│ IGameManager  │          │IStageManager  │          │IEnemySpawner  │
├───────────────┤          ├───────────────┤          ├───────────────┤
│ GameManager   │          │ StageManager  │          │ EnemySpawner  │
└───────────────┘          └───────────────┘          └───────────────┘
        │                           │                           │
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐          ┌───────────────┐          ┌───────────────┐
│IPlayerHealth  │          │IStageStat     │          │ IRewardHandler│
│(컴포넌트용)   │          │Provider       │          │ (분리됨)      │
└───────────────┘          └───────────────┘          └───────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                          Event Bus (선택적)                              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Subscribe<T>(Action<T>) / Publish<T>(T eventData)              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.3 계층 구조 (수정된 DDD)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Presentation Layer (UI)                                                  │
│   - MonoBehaviour UI 컴포넌트                                           │
│   - 인터페이스를 통해서만 Manager 접근                                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Application Layer (Manager)                                              │
│   - 유스케이스 구현                                                     │
│   - 인터페이스 구현                                                     │
│   - ServiceLocator에 등록                                               │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Domain Layer (Data + Interface)                                          │
│   - Entity, Value Objects                                               │
│   - Repository 인터페이스                                               │
│   - Manager 인터페이스                                                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ Infrastructure Layer (Repository)                                        │
│   - Repository 구현                                                     │
│   - 외부 시스템 연동 (BGDatabase 등)                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 인터페이스 설계

### 4.1 Core Interfaces (핵심 Manager)

#### IGameManager
```csharp
// Assets/02.Scripts/Core/Interface/IGameManager.cs
public interface IGameManager
{
    event Action OnPlayerDeath;
    event Action OnPlayerRevive;
    event Action<int> OnRequestStageRestart;

    void RegisterPlayer(IPlayerHealth player);
    void UnregisterPlayer();
    void HandlePlayerDeath();
}
```

#### IStageManager
```csharp
// Assets/02.Scripts/Core/Interface/IStageManager.cs
public interface IStageManager
{
    event Action<int> OnStageStarted;
    event Action<int> OnStageCleared;
    event Action OnAllStagesCleared;
    event Action<IEnemy> OnBossSpawned;

    int CurrentStageId { get; }
    string CurrentStageName { get; }
    StageStat CurrentStageStat { get; }

    void StartGame();
    void StartStage(int stageId);
    void SpawnBoss();
    void RestartCurrentStage();
    void OnPlayerDied();
}
```

#### IEnemySpawner
```csharp
// Assets/02.Scripts/Core/Interface/IEnemySpawner.cs
public interface IEnemySpawner
{
    event Action<IEnemy> OnEnemyDied;
    event Action<IEnemy> OnBossDied;
    event Action<IEnemy> OnBossSpawned;
    event Action<EnemyStat> OnEnemyDiedWithStat;  // 보상 처리용

    int AliveEnemyCount { get; }
    bool IsBossSpawned { get; }
    bool IsBossAlive { get; }

    IEnemy SpawnEnemy(string statId, StageStat stageStat);
    IEnemy SpawnBoss(string bossStatId, StageStat stageStat);
    void ReturnEnemy(IEnemy enemy);
    void ReturnAll();
    void ResetBossState();
}
```

### 4.2 Player Interfaces

#### IPlayerHealth
```csharp
// Assets/02.Scripts/Core/Interface/IPlayerHealth.cs
public interface IPlayerHealth : IDamageable
{
    event Action<double, double> OnHealthChanged;
    event Action OnDeath;

    double MaxHealth { get; }
    double CurrentHealth { get; }
    bool IsDead { get; }

    void Heal(double amount);
    void Revive();
}
```

#### IPlayerStatProvider
```csharp
// Assets/02.Scripts/Core/Interface/IPlayerStatProvider.cs
public interface IPlayerStatProvider
{
    event Action<int> OnLevelUp;
    event Action<double, double> OnExpChanged;

    int Level { get; }
    double CurrentExp { get; }
    double MaxExp { get; }
    double BaseMaxHealth { get; }
    float BaseMoveSpeed { get; }

    void AddExp(double amount);
}
```

### 4.3 Currency & Upgrade Interfaces

#### ICurrencyManager
```csharp
// Assets/02.Scripts/Core/Interface/ICurrencyManager.cs
public interface ICurrencyManager
{
    event Action<ECurrencyType, double> OnCurrencyChanged;

    double Gold { get; }
    double Ruby { get; }

    void AddGold(double amount);
    void AddRuby(double amount);
    bool TrySpendGold(double amount);
    bool TrySpendRuby(double amount);
    double GetCurrency(ECurrencyType type);
}
```

#### IUpgradeManager
```csharp
// Assets/02.Scripts/Core/Interface/IUpgradeManager.cs
public interface IUpgradeManager
{
    event Action<string, int> OnUpgraded;
    event Action OnInitialized;

    bool IsReady { get; }

    bool TryUpgrade(string upgradeId);
    int GetLevel(string upgradeId);
    double GetBonus(string upgradeId);
    double GetCost(string upgradeId);
    double GetPlayerHealthBonus();
    double GetPlayerMoveSpeedBonus();
}
```

### 4.4 Enemy Interface

#### IEnemy
```csharp
// Assets/02.Scripts/Core/Interface/IEnemy.cs
public interface IEnemy : IDamageable
{
    event Action<IEnemy, EnemyStat> OnDied;

    string Id { get; }
    bool IsBoss { get; }
    bool IsDead { get; }
    double CurrentHealth { get; }
    double MaxHealth { get; }
    Transform Transform { get; }

    void Initialize(EnemyStat stat);
    void InitializeAsBoss(EnemyStat stat);
    void ResetForPool();
}
```

### 4.5 Utility Interfaces

#### IRewardHandler (새로 분리)
```csharp
// Assets/02.Scripts/Core/Interface/IRewardHandler.cs
public interface IRewardHandler
{
    void HandleReward(EnemyStat stat);
}
```

#### ISoundService
```csharp
// Assets/02.Scripts/Core/Interface/ISoundService.cs
public interface ISoundService
{
    void PlaySFX(ESfxId sfxId);
    void PlaySFX(ESfxId sfxId, Vector3 position);
    void PlayBGM(EBgmId bgmId);
    void StopBGM();
    void SetMasterVolume(float volume);
    void SetBGMVolume(float volume);
    void SetSFXVolume(float volume);
}
```

#### IEffectService
```csharp
// Assets/02.Scripts/Core/Interface/IEffectService.cs
public interface IEffectService
{
    void PlayHitVfx(Vector3 position);
    void PlayHitVfxByIndex(int index, Vector3 position);
    void PlaySkillVfx(Vector3 position);
    void PlayCameraShake(float duration, float strength, int vibrato);
}
```

---

## 5. 서비스 로케이터 구현

### 5.1 ServiceLocator 클래스

```csharp
// Assets/02.Scripts/Core/ServiceLocator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static bool _isInitialized;

    public static event Action OnServicesReady;

    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
        }
    }

    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
        }
    }

    public static T Resolve<T>() where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        Debug.LogError($"[ServiceLocator] Service {type.Name} not registered.");
        return null;
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object obj))
        {
            service = obj as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
        _isInitialized = false;
    }

    public static void MarkAsReady()
    {
        _isInitialized = true;
        OnServicesReady?.Invoke();
    }

    public static bool IsReady => _isInitialized;
}
```

### 5.2 ServiceInstaller (Bootstrap)

```csharp
// Assets/02.Scripts/Core/ServiceInstaller.cs
using UnityEngine;

public class ServiceInstaller : MonoBehaviour
{
    [Header("DontDestroy Services")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerSessionManager _playerSessionManager;
    [SerializeField] private PlayerStatManager _playerStatManager;
    [SerializeField] private CurrencyManager _currencyManager;
    [SerializeField] private UpgradeManager _upgradeManager;
    [SerializeField] private SoundManager _soundManager;
    [SerializeField] private PopupManager _popupManager;
    [SerializeField] private FadeManager _fadeManager;
    [SerializeField] private RedDotManager _redDotManager;
    [SerializeField] private OfflineRewardManager _offlineRewardManager;

    [Header("Handlers")]
    [SerializeField] private EnemyRewardHandler _rewardHandler;

    private void Awake()
    {
        RegisterServices();
    }

    private void RegisterServices()
    {
        // Core Services
        ServiceLocator.Register<IGameManager>(_gameManager);
        ServiceLocator.Register<IPlayerStatProvider>(_playerStatManager);
        ServiceLocator.Register<ICurrencyManager>(_currencyManager);
        ServiceLocator.Register<IUpgradeManager>(_upgradeManager);
        ServiceLocator.Register<ISoundService>(_soundManager);

        // Handlers
        ServiceLocator.Register<IRewardHandler>(_rewardHandler);

        // Mark as ready after all services registered
        ServiceLocator.MarkAsReady();
    }

    private void OnDestroy()
    {
        ServiceLocator.Clear();
    }
}
```

### 5.3 SceneServiceInstaller (씬별 서비스)

```csharp
// Assets/02.Scripts/Core/SceneServiceInstaller.cs
using UnityEngine;

public class SceneServiceInstaller : MonoBehaviour
{
    [Header("Scene Services")]
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private EffectManager _effectManager;

    private void Awake()
    {
        RegisterSceneServices();
    }

    private void RegisterSceneServices()
    {
        if (_stageManager != null)
            ServiceLocator.Register<IStageManager>(_stageManager);

        if (_enemySpawner != null)
            ServiceLocator.Register<IEnemySpawner>(_enemySpawner);

        if (_effectManager != null)
            ServiceLocator.Register<IEffectService>(_effectManager);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IStageManager>();
        ServiceLocator.Unregister<IEnemySpawner>();
        ServiceLocator.Unregister<IEffectService>();
    }
}
```

---

## 6. 단계별 구현 계획

### Phase 1: 기반 인프라 구축

#### 1-A: Core 인터페이스 정의
```
Assets/02.Scripts/Core/
├── Interface/
│   ├── IGameManager.cs
│   ├── IStageManager.cs
│   ├── IEnemySpawner.cs
│   ├── IEnemy.cs
│   ├── IPlayerHealth.cs
│   ├── IPlayerStatProvider.cs
│   ├── ICurrencyManager.cs
│   ├── IUpgradeManager.cs
│   ├── IRewardHandler.cs
│   ├── ISoundService.cs
│   └── IEffectService.cs
├── ServiceLocator.cs
├── ServiceInstaller.cs
└── SceneServiceInstaller.cs
```

#### 1-B: 기존 Singleton 수정
```csharp
// 기존 Singleton<T>에 인터페이스 지원 추가
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // ... 기존 코드 ...

    protected virtual void RegisterToServiceLocator() { }
    protected virtual void UnregisterFromServiceLocator() { }
}
```

### Phase 2: Manager 리팩토링

#### 2-A: GameManager
```csharp
public class GameManager : DontDestroySingleton<GameManager>, IGameManager
{
    // 인터페이스 구현
    public event Action OnPlayerDeath;
    public event Action OnPlayerRevive;
    public event Action<int> OnRequestStageRestart;

    private IPlayerHealth _playerHealth;

    public void RegisterPlayer(IPlayerHealth player)
    {
        _playerHealth = player;
        _playerHealth.OnDeath += HandlePlayerDeath;
    }

    // ... 나머지 구현 ...

    protected override void Initialize()
    {
        ServiceLocator.Register<IGameManager>(this);
    }
}
```

#### 2-B: StageManager
```csharp
public class StageManager : Singleton<StageManager>, IStageManager
{
    private IEnemySpawner _enemySpawner;
    private ISoundService _soundService;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _maxStageId = _repository.GetMaxStageId();
        ServiceLocator.Register<IStageManager>(this);
    }

    private void Start()
    {
        // ServiceLocator에서 의존성 획득
        _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();
        _soundService = ServiceLocator.Resolve<ISoundService>();

        SubscribeToEvents();
        // ...
    }

    private void SpawnEnemy(StageStat stage)
    {
        _enemySpawner?.SpawnEnemy(stage.EnemyStatId, stage);
    }

    public void SpawnBoss()
    {
        if (_currentStageStat == null) return;

        StopSpawning();
        _enemySpawner?.ReturnAll();

        var boss = _enemySpawner?.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
        if (boss != null)
        {
            OnBossSpawned?.Invoke(boss);
        }
    }
}
```

#### 2-C: EnemySpawner + RewardHandler 분리
```csharp
// EnemySpawner - 스폰 책임만
public class EnemySpawner : Singleton<EnemySpawner>, IEnemySpawner
{
    public event Action<IEnemy> OnEnemyDied;
    public event Action<IEnemy> OnBossDied;
    public event Action<IEnemy> OnBossSpawned;
    public event Action<EnemyStat> OnEnemyDiedWithStat;

    private void HandleEnemyDied(EnemyAI enemy, EnemyStat stat)
    {
        if (stat == null) return;

        // ❌ 제거: 보상 지급 로직
        // CurrencyManager.Instance?.AddGold(stat.GoldReward);
        // PlayerStatManager.Instance?.AddExp(stat.Exp);

        // ✅ 이벤트만 발행 (보상은 RewardHandler가 처리)
        OnEnemyDiedWithStat?.Invoke(stat);

        if (enemy.IsBoss)
        {
            OnBossDied?.Invoke(enemy);
        }
        else
        {
            OnEnemyDied?.Invoke(enemy);
        }
    }
}

// EnemyRewardHandler - 보상 책임
public class EnemyRewardHandler : MonoBehaviour, IRewardHandler
{
    private ICurrencyManager _currencyManager;
    private IPlayerStatProvider _playerStatManager;
    private IEnemySpawner _enemySpawner;

    private void Start()
    {
        _currencyManager = ServiceLocator.Resolve<ICurrencyManager>();
        _playerStatManager = ServiceLocator.Resolve<IPlayerStatProvider>();
        _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();

        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemyDiedWithStat += HandleReward;
        }
    }

    private void OnDestroy()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemyDiedWithStat -= HandleReward;
        }
    }

    public void HandleReward(EnemyStat stat)
    {
        if (stat == null) return;

        _currencyManager?.AddGold(stat.GoldReward);
        _playerStatManager?.AddExp(stat.Exp);
    }
}
```

### Phase 3: Player/Enemy 리팩토링

#### 3-A: PlayerHealth
```csharp
public class PlayerHealth : MonoBehaviour, IPlayerHealth
{
    private IPlayerStatProvider _statProvider;
    private IUpgradeManager _upgradeManager;
    private IGameManager _gameManager;

    public event Action<double, double> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        // ServiceLocator 준비 대기
        if (ServiceLocator.IsReady)
        {
            InitializeDependencies();
        }
        else
        {
            ServiceLocator.OnServicesReady += InitializeDependencies;
        }
    }

    private void InitializeDependencies()
    {
        ServiceLocator.OnServicesReady -= InitializeDependencies;

        _statProvider = ServiceLocator.Resolve<IPlayerStatProvider>();
        _upgradeManager = ServiceLocator.Resolve<IUpgradeManager>();
        _gameManager = ServiceLocator.Resolve<IGameManager>();

        if (_statProvider != null)
        {
            _baseMaxHealth = _statProvider.BaseMaxHealth;
        }

        _gameManager?.RegisterPlayer(this);

        if (_upgradeManager != null)
        {
            _upgradeManager.OnUpgraded += OnUpgradeChanged;
            _upgradeManager.OnInitialized += OnUpgradeManagerInitialized;
        }

        ApplyUpgradeBonus();
    }

    private void OnDestroy()
    {
        if (_upgradeManager != null)
        {
            _upgradeManager.OnUpgraded -= OnUpgradeChanged;
            _upgradeManager.OnInitialized -= OnUpgradeManagerInitialized;
        }
    }
}
```

#### 3-B: EnemyAI
```csharp
public class EnemyAI : MonoBehaviour, IEnemy
{
    private IEffectService _effectService;
    private ISoundService _soundService;

    public event Action<IEnemy, EnemyStat> OnDied;

    public string Id => _stat?.Id ?? string.Empty;
    public bool IsBoss => _isBoss;
    public Transform Transform => transform;

    private void Awake()
    {
        // 풀에서 재사용되므로 매번 Resolve하지 않음
        CacheDependencies();
    }

    private void CacheDependencies()
    {
        _effectService = ServiceLocator.Resolve<IEffectService>();
        _soundService = ServiceLocator.Resolve<ISoundService>();
    }

    public void TakeDamage(double damage, bool isCrit)
    {
        if (_currentState == State.Dead) return;

        _currentHealth -= damage;
        UpdateHPBar();

        // 이펙트 (인터페이스 통해 접근)
        _effectService?.PlayHitVfx(transform.position);
        _effectService?.PlayCameraShake(0.1f, 0.5f, 10);

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TriggerHitState();
        }
    }

    private void Die()
    {
        _currentState = State.Dead;
        _agent.enabled = false;

        _soundService?.PlaySFX(ESfxId.MonsterDead, transform.position);
        _enemyAnimation.Die();

        OnDied?.Invoke(this, _stat);

        StartCoroutine(ReturnToPoolAfterDelay(3f));
    }
}
```

### Phase 4: UI 리팩토링

#### 4-A: UI 베이스 클래스
```csharp
// Assets/02.Scripts/UI/Base/ServiceDependentUI.cs
public abstract class ServiceDependentUI : MonoBehaviour
{
    protected virtual void Awake()
    {
        if (ServiceLocator.IsReady)
        {
            OnServicesReady();
        }
        else
        {
            ServiceLocator.OnServicesReady += OnServicesReady;
        }
    }

    protected virtual void OnDestroy()
    {
        ServiceLocator.OnServicesReady -= OnServicesReady;
        UnsubscribeFromEvents();
    }

    protected abstract void OnServicesReady();
    protected abstract void SubscribeToEvents();
    protected abstract void UnsubscribeFromEvents();
}
```

#### 4-B: CurrencyUI 예시
```csharp
public class CurrencyUI : ServiceDependentUI
{
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _rubyText;

    private ICurrencyManager _currencyManager;

    protected override void OnServicesReady()
    {
        _currencyManager = ServiceLocator.Resolve<ICurrencyManager>();
        SubscribeToEvents();
        UpdateUI();
    }

    protected override void SubscribeToEvents()
    {
        if (_currencyManager != null)
        {
            _currencyManager.OnCurrencyChanged += HandleCurrencyChanged;
        }
    }

    protected override void UnsubscribeFromEvents()
    {
        if (_currencyManager != null)
        {
            _currencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }

    private void HandleCurrencyChanged(ECurrencyType type, double value)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currencyManager == null) return;

        _goldText.text = _currencyManager.Gold.ToString("N0");
        _rubyText.text = _currencyManager.Ruby.ToString("N0");
    }
}
```

---

## 7. 파일 변경 목록

### 7.1 신규 생성 파일

| Phase | 파일 경로 | 설명 |
|-------|----------|------|
| 1-A | `Assets/02.Scripts/Core/Interface/IGameManager.cs` | 게임 매니저 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IStageManager.cs` | 스테이지 매니저 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IEnemySpawner.cs` | 적 스포너 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IEnemy.cs` | 적 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IPlayerHealth.cs` | 플레이어 체력 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IPlayerStatProvider.cs` | 플레이어 스탯 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/ICurrencyManager.cs` | 재화 매니저 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IUpgradeManager.cs` | 강화 매니저 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IRewardHandler.cs` | 보상 핸들러 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/ISoundService.cs` | 사운드 서비스 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/Interface/IEffectService.cs` | 이펙트 서비스 인터페이스 |
| 1-A | `Assets/02.Scripts/Core/ServiceLocator.cs` | 서비스 로케이터 |
| 1-A | `Assets/02.Scripts/Core/ServiceInstaller.cs` | 서비스 설치자 |
| 1-A | `Assets/02.Scripts/Core/SceneServiceInstaller.cs` | 씬별 서비스 설치자 |
| 2-C | `Assets/02.Scripts/Enemy/Handler/EnemyRewardHandler.cs` | 보상 핸들러 (분리) |
| 4-A | `Assets/02.Scripts/UI/Base/ServiceDependentUI.cs` | UI 베이스 클래스 |

**총 신규 파일: 16개**

### 7.2 수정 파일

| Phase | 파일 경로 | 변경 내용 |
|-------|----------|----------|
| 1-B | `Assets/02.Scripts/Util/Singleton.cs` | ServiceLocator 등록 지원 |
| 1-B | `Assets/02.Scripts/Util/DontDestroySingleton.cs` | ServiceLocator 등록 지원 |
| 2-A | `Assets/02.Scripts/Game/GameManager.cs` | IGameManager 구현 |
| 2-B | `Assets/02.Scripts/Stage/Manager/StageManager.cs` | IStageManager 구현, 의존성 주입 |
| 2-C | `Assets/02.Scripts/Enemy/Manager/EnemySpawner.cs` | IEnemySpawner 구현, 보상 로직 제거 |
| 3-A | `Assets/02.Scripts/Player/PlayerHealth.cs` | IPlayerHealth 구현, 의존성 주입 |
| 3-A | `Assets/02.Scripts/Player/PlayerMovement.cs` | 의존성 주입 |
| 3-A | `Assets/02.Scripts/Player/PlayerAutoMovement.cs` | 의존성 주입 |
| 3-A | `Assets/02.Scripts/Player/Manager/PlayerStatManager.cs` | IPlayerStatProvider 구현 |
| 3-B | `Assets/02.Scripts/Enemy/EnemyAI.cs` | IEnemy 구현, 의존성 주입 |
| 2-A | `Assets/02.Scripts/Currency/Manager/CurrencyManager.cs` | ICurrencyManager 구현 |
| 2-A | `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs` | IUpgradeManager 구현 |
| 2-A | `Assets/02.Scripts/Sound/Manager/SoundManager.cs` | ISoundService 구현 |
| 2-A | `Assets/02.Scripts/Effect/Manager/EffectManager.cs` | IEffectService 구현 |
| 4-B | `Assets/02.Scripts/UI/CurrencyUI.cs` | ServiceDependentUI 상속 |
| 4-B | `Assets/02.Scripts/UI/StageUI.cs` | ServiceDependentUI 상속 |
| 4-B | `Assets/02.Scripts/UI/BossSpawnUI.cs` | ServiceDependentUI 상속 |
| 4-B | `Assets/02.Scripts/Player/PlayerHealthUI.cs` | ServiceDependentUI 상속 |
| 4-B | `Assets/02.Scripts/Upgrade/UI/UpgradeSlotUI.cs` | ServiceDependentUI 상속 |

**총 수정 파일: 19개**

### 7.3 씬 수정

| 씬 | 변경 내용 |
|----|----------|
| `StartScene` | ServiceInstaller 프리팹 배치 |
| `MainScene` | SceneServiceInstaller, EnemyRewardHandler 배치 |

---

## 8. 마이그레이션 가이드

### 8.1 기존 코드 → 신규 코드 변환

#### 싱글톤 직접 호출 → ServiceLocator
```csharp
// ❌ Before
CurrencyManager.Instance.AddGold(100);
PlayerStatManager.Instance.AddExp(50);

// ✅ After
var currency = ServiceLocator.Resolve<ICurrencyManager>();
var stats = ServiceLocator.Resolve<IPlayerStatProvider>();
currency?.AddGold(100);
stats?.AddExp(50);
```

#### 이벤트 구독 → 인터페이스 기반
```csharp
// ❌ Before
UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;

// ✅ After
private IUpgradeManager _upgradeManager;

private void OnServicesReady()
{
    _upgradeManager = ServiceLocator.Resolve<IUpgradeManager>();
    _upgradeManager.OnUpgraded += OnUpgradeChanged;
}
```

#### HasInstance 체크 → TryResolve
```csharp
// ❌ Before
if (CurrencyManager.HasInstance)
{
    CurrencyManager.Instance.AddGold(100);
}

// ✅ After
if (ServiceLocator.TryResolve<ICurrencyManager>(out var currency))
{
    currency.AddGold(100);
}
```

### 8.2 점진적 마이그레이션 전략

```
Week 1: Phase 1 (기반 인프라)
├── Core 인터페이스 정의
├── ServiceLocator 구현
└── ServiceInstaller 구현

Week 2: Phase 2 (Manager 리팩토링)
├── GameManager, StageManager 인터페이스 구현
├── EnemySpawner + RewardHandler 분리
└── Currency, Upgrade Manager 인터페이스 구현

Week 3: Phase 3 (Player/Enemy 리팩토링)
├── PlayerHealth, PlayerMovement 의존성 주입
├── EnemyAI 의존성 주입
└── 기존 싱글톤 직접 호출 제거

Week 4: Phase 4 (UI 리팩토링)
├── ServiceDependentUI 베이스 클래스 적용
├── 모든 UI 컴포넌트 마이그레이션
└── 테스트 및 버그 수정
```

### 8.3 하위 호환성 유지

마이그레이션 기간 동안 기존 코드와의 호환성 유지:

```csharp
public class CurrencyManager : DontDestroySingleton<CurrencyManager>, ICurrencyManager
{
    // 기존 싱글톤 접근 유지 (Deprecated)
    [Obsolete("Use ServiceLocator.Resolve<ICurrencyManager>() instead")]
    public new static CurrencyManager Instance => _instance;

    // 인터페이스 구현
    public double Gold => _gold;
    public double Ruby => _ruby;

    protected override void Initialize()
    {
        // ServiceLocator에 등록
        ServiceLocator.Register<ICurrencyManager>(this);
    }
}
```

---

## 9. 검증 및 테스트

### 9.1 단위 테스트 가능성

DI 적용 후 Mock 객체 주입 가능:

```csharp
[Test]
public void PlayerHealth_TakeDamage_ReducesHealth()
{
    // Arrange
    var mockStatProvider = new MockPlayerStatProvider { BaseMaxHealth = 100 };
    var mockUpgradeManager = new MockUpgradeManager();

    ServiceLocator.Register<IPlayerStatProvider>(mockStatProvider);
    ServiceLocator.Register<IUpgradeManager>(mockUpgradeManager);

    var playerHealth = new GameObject().AddComponent<PlayerHealth>();

    // Act
    playerHealth.TakeDamage(30, false);

    // Assert
    Assert.AreEqual(70, playerHealth.CurrentHealth);

    // Cleanup
    ServiceLocator.Clear();
}
```

### 9.2 통합 테스트 체크리스트

- [ ] 게임 시작 → 스테이지 로드 → 적 스폰 정상
- [ ] 적 처치 → 보상 지급 (금화, 경험치) 정상
- [ ] 보스 스폰 → 처치 → 스테이지 클리어 정상
- [ ] 플레이어 사망 → 부활 → 스테이지 재시작 정상
- [ ] 강화 → 플레이어 스탯 반영 정상
- [ ] UI 갱신 (재화, 체력, 스테이지) 정상
- [ ] 씬 전환 시 서비스 유지/해제 정상

### 9.3 성능 테스트

| 항목 | 기준 | 측정 방법 |
|------|------|----------|
| ServiceLocator.Resolve 호출 | < 0.01ms | Profiler |
| 이벤트 구독/해제 | 메모리 누수 없음 | Memory Profiler |
| 적 100마리 스폰 | FPS 60 유지 | Frame Debugger |

---

## 10. 예상 효과

### 10.1 정량적 효과

| 지표 | Before | After |
|------|--------|-------|
| Manager 간 직접 결합 | 15+ 개소 | 0 |
| 싱글톤 직접 호출 | 50+ 개소 | 0 (Deprecated) |
| 테스트 가능 클래스 | 0% | 80%+ |
| 순환 의존성 | 3+ | 0 |

### 10.2 정성적 효과

1. **테스트 용이성**: Mock 객체 주입으로 유닛 테스트 가능
2. **유지보수성**: 인터페이스 변경 없이 구현체 교체 가능
3. **확장성**: 새 기능 추가 시 기존 코드 수정 최소화
4. **가독성**: 의존성이 명시적으로 드러남
5. **디버깅**: 서비스 등록/해제 추적 용이

---

## 11. 위험 요소 및 대응

### 11.1 예상 위험

| 위험 | 영향도 | 대응 방안 |
|------|--------|----------|
| ServiceLocator 미등록 서비스 접근 | 높음 | TryResolve 사용, 로그 경고 |
| 이벤트 구독 해제 누락 | 중간 | OnDestroy 체크리스트, 정적 분석 |
| 성능 저하 (Resolve 호출 증가) | 낮음 | 캐싱, Start()에서만 Resolve |
| 마이그레이션 중 버그 | 중간 | 점진적 마이그레이션, 회귀 테스트 |

### 11.2 롤백 계획

모든 기존 싱글톤 접근을 유지하면서 인터페이스 지원을 추가하므로,
문제 발생 시 ServiceLocator 사용 코드만 롤백 가능.

---

## 12. 결론

### 12.1 핵심 변경 사항

1. **인터페이스 기반 추상화**: 11개 핵심 인터페이스 도입
2. **ServiceLocator 패턴**: Unity 환경에 적합한 DI 구현
3. **책임 분리**: EnemyRewardHandler 분리로 SRP 준수
4. **점진적 마이그레이션**: 하위 호환성 유지하며 단계적 전환

### 12.2 작업 규모

| 항목 | 수량 |
|------|------|
| 신규 파일 | 16개 |
| 수정 파일 | 19개 |
| 예상 작업 기간 | 4주 |

### 12.3 우선순위

```
1순위: Phase 1 (기반 인프라) - ServiceLocator, 핵심 인터페이스
2순위: Phase 2 (Manager 리팩토링) - SRP 위반 해결
3순위: Phase 3 (Player/Enemy) - 게임플레이 핵심
4순위: Phase 4 (UI) - 프레젠테이션 계층
```

검토 후 피드백 주시면 수정하겠습니다.
