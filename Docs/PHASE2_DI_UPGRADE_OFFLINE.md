# Phase 2: UpgradeManager, OfflineRewardManager DI 전환

---

## 1. 개요

| 항목 | 내용 |
|:---|:---|
| **목표** | UpgradeManager, OfflineRewardManager의 `.Instance` 직접 참조 제거 |
| **범위** | `UpgradeManager.cs`, `OfflineRewardManager.cs` |
| **위험도** | 낮음 (기존 인터페이스 활용, 폴백 유지) |

---

## 2. SOLID 관점 검토 및 대응

### 2.1 피드백 요약

| 원칙 | 현재 상태 | 대응 |
|:---|:---|:---|
| **SRP** | Manager에 의존성 조립 책임 혼재 | 이행기 허용, 장기적으로 Bootstrapper 분리 |
| **OCP** | 인터페이스로 구현 교체 가능 | 폴백 남아있지만 전환 단계로 OK |
| **LSP** | 인터페이스 계약 일관성 필요 | 엣지 케이스 규약 문서화 필요 |
| **ISP** | 현재 인터페이스가 클 수 있음 | 이행기 유지, 필요시 분리 고려 |
| **DIP** | ServiceLocator에 여전히 의존 | 이행기 허용, 장기적으로 순수 주입 전환 |

### 2.2 반드시 해결해야 할 문제 (2가지)

#### 문제 1: 초기화 순서 리스크

**현상:**
- `Start()`에서 `InitializeDependencies()` 호출 시 ServiceLocator 등록이 완료되지 않았을 수 있음
- DontDestroySingleton은 `Start()`가 최초 1회만 호출되므로, 타이밍 놓치면 계속 null

**원인:**
```
Unity Start() 호출 순서 (비보장):
- UpgradeManager.Start() ← 이게 먼저 호출되면?
- ServiceInstaller.Start() → MarkAsReady()
- OfflineRewardManager.Start()
```

**해결책: OnServicesReady 이벤트 구독 + EnsureDependencies 백업**

```csharp
protected override void Initialize()
{
    // ... 기존 로직 ...

    // 서비스 준비 상태에 따라 의존성 초기화
    if (ServiceLocator.IsReady)
    {
        InitializeDependencies();
    }
    else
    {
        ServiceLocator.OnServicesReady += OnServicesReady;
    }
}

private void OnServicesReady()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;
    InitializeDependencies();
}

private void OnDestroy()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;
    // ... 기존 정리 로직 ...
}
```

**추가 안전장치: EnsureDependencies (lazy resolve)**

```csharp
private void EnsureDependencies()
{
    if (_currencyService != null) return;

    _currencyService = ServiceLocator.Resolve<ICurrencyService>();

    // 폴백 (전환기)
    if (_currencyService == null && CurrencyManager.Instance != null)
    {
        _currencyService = CurrencyManager.Instance;
    }
}
```

#### 문제 2: OfflineRewardManager 보상 증발 가능성

**현상:**
- 기존 계획의 `ClaimReward()`는 서비스가 null이면 지급 스킵하고 `HasPendingReward=false`로 설정
- 보상이 그대로 증발함

**해결책: 서비스 없으면 보상 유지**

```csharp
public void ClaimReward()
{
    if (!HasPendingReward || PendingReward == null) return;

    // 서비스 확인 (lazy resolve)
    EnsureDependencies();

    // 서비스가 없으면 보상 유지하고 리턴
    if (_currencyService == null || _playerStatService == null)
    {
        Debug.LogWarning("[OfflineRewardManager] 서비스가 준비되지 않아 보상을 유지합니다.");
        return;
    }

    // 보상 지급
    _currencyService.AddGold(PendingReward.GoldReward);
    _playerStatService.AddExp(PendingReward.ExpReward);

    // 지급 완료 후에만 상태 변경
    HasPendingReward = false;
    PendingReward = null;

    _ = SaveCurrentTimeAsync();

    Debug.Log("[OfflineRewardManager] 오프라인 보상 지급 완료");
}
```

---

## 3. 현재 상태 분석

### 3.1 UpgradeManager

**파일:** `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs`

**싱글톤 직접 참조:** 1개소 (`CurrencyManager.Instance`)

```csharp
// TryUpgrade() 메서드 내부
if (!CurrencyManager.Instance.TrySpendGold(cost))
```

### 3.2 OfflineRewardManager

**파일:** `Assets/02.Scripts/OfflineReward/Manager/OfflineRewardManager.cs`

**싱글톤 직접 참조:** 2개소

```csharp
// ClaimReward() 메서드 내부
CurrencyManager.Instance.AddGold(PendingReward.GoldReward);
PlayerStatManager.Instance.AddExp(PendingReward.ExpReward);
```

---

## 4. 변경 계획

### 4.1 UpgradeManager 변경

#### 4.1.1 필드 추가

```csharp
public class UpgradeManager : DontDestroySingleton<UpgradeManager>
{
    private IUpgradeRepository _repository;
    private PlayerLevels _playerLevels;

    // 추가: DI 의존성
    private ICurrencyService _currencyService;
```

#### 4.1.2 Initialize 메서드 수정

```csharp
protected override void Initialize()
{
    Debug.Log($"[UpgradeManager] Initialize 호출됨");

    PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;

    if (PlayerSessionManager.Instance.IsLoggedIn)
    {
        InitializeRepository();
    }

    // 서비스 준비 상태에 따라 의존성 초기화
    if (ServiceLocator.IsReady)
    {
        InitializeDependencies();
    }
    else
    {
        ServiceLocator.OnServicesReady += OnServicesReady;
    }
}

private void OnServicesReady()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;
    InitializeDependencies();
}
```

#### 4.1.3 InitializeDependencies 및 EnsureDependencies 추가

```csharp
private void InitializeDependencies()
{
    _currencyService = ServiceLocator.Resolve<ICurrencyService>();

    // 폴백 (전환기)
    if (_currencyService == null && CurrencyManager.Instance != null)
    {
        _currencyService = CurrencyManager.Instance;
    }

    Debug.Log($"[UpgradeManager] 의존성 초기화 완료: CurrencyService={_currencyService != null}");
}

private void EnsureDependencies()
{
    if (_currencyService != null) return;

    _currencyService = ServiceLocator.Resolve<ICurrencyService>();

    if (_currencyService == null && CurrencyManager.Instance != null)
    {
        _currencyService = CurrencyManager.Instance;
    }
}
```

#### 4.1.4 OnDestroy 수정

```csharp
private void OnDestroy()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;

    if (PlayerSessionManager.HasInstance)
    {
        PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
    }
}
```

#### 4.1.5 TryUpgrade 메서드 수정

```csharp
public bool TryUpgrade(string upgradeId)
{
    if (_repository == null)
    {
        Debug.LogWarning("[UpgradeManager] 아직 초기화되지 않았습니다.");
        return false;
    }

    UpgradeData data = _repository.GetUpgradeData(upgradeId);
    if (data == null)
    {
        Debug.LogError($"[UpgradeManager] 강화 데이터를 찾을 수 없습니다: {upgradeId}");
        return false;
    }

    int currentLevel = _playerLevels.GetLevel(upgradeId);
    double cost = data.GetCost(currentLevel);

    // lazy resolve 시도
    EnsureDependencies();

    if (_currencyService == null)
    {
        Debug.LogError("[UpgradeManager] CurrencyService가 없습니다.");
        return false;
    }

    if (!_currencyService.TrySpendGold(cost))
    {
        Debug.Log($"[UpgradeManager] 골드 부족: 필요 {CurrencyFormatter.FormatAbbreviated(cost)}");
        return false;
    }

    _playerLevels.IncrementLevel(upgradeId);
    _repository.SavePlayerLevels(_playerLevels);

    int newLevel = _playerLevels.GetLevel(upgradeId);
    OnUpgraded?.Invoke(upgradeId, newLevel);

    Debug.Log($"[UpgradeManager] 강화 성공: {upgradeId} Lv.{newLevel}");
    return true;
}
```

---

### 4.2 OfflineRewardManager 변경

#### 4.2.1 필드 추가

```csharp
public class OfflineRewardManager : DontDestroySingleton<OfflineRewardManager>
{
    // ... 기존 필드 ...

    // 추가: DI 의존성
    private ICurrencyService _currencyService;
    private IPlayerStatService _playerStatService;
```

#### 4.2.2 Initialize 메서드 수정

```csharp
protected override void Initialize()
{
    SceneManager.sceneLoaded += OnSceneLoaded;
    PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;

    if (PlayerSessionManager.Instance.IsLoggedIn)
    {
        InitializeRepository();
    }

    // 서비스 준비 상태에 따라 의존성 초기화
    if (ServiceLocator.IsReady)
    {
        InitializeDependencies();
    }
    else
    {
        ServiceLocator.OnServicesReady += OnServicesReady;
    }
}

private void OnServicesReady()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;
    InitializeDependencies();
}
```

#### 4.2.3 InitializeDependencies 및 EnsureDependencies 추가

```csharp
private void InitializeDependencies()
{
    _currencyService = ServiceLocator.Resolve<ICurrencyService>();
    _playerStatService = ServiceLocator.Resolve<IPlayerStatService>();

    // 폴백 (전환기)
    if (_currencyService == null && CurrencyManager.Instance != null)
    {
        _currencyService = CurrencyManager.Instance;
    }

    if (_playerStatService == null && PlayerStatManager.Instance != null)
    {
        _playerStatService = PlayerStatManager.Instance;
    }

    Debug.Log($"[OfflineRewardManager] 의존성 초기화 완료: Currency={_currencyService != null}, PlayerStat={_playerStatService != null}");
}

private void EnsureDependencies()
{
    if (_currencyService == null)
    {
        _currencyService = ServiceLocator.Resolve<ICurrencyService>();
        if (_currencyService == null && CurrencyManager.Instance != null)
        {
            _currencyService = CurrencyManager.Instance;
        }
    }

    if (_playerStatService == null)
    {
        _playerStatService = ServiceLocator.Resolve<IPlayerStatService>();
        if (_playerStatService == null && PlayerStatManager.Instance != null)
        {
            _playerStatService = PlayerStatManager.Instance;
        }
    }
}
```

#### 4.2.4 OnDestroy 수정

```csharp
private void OnDestroy()
{
    ServiceLocator.OnServicesReady -= OnServicesReady;
    SceneManager.sceneLoaded -= OnSceneLoaded;

    if (_autoSaveCoroutine != null)
    {
        StopCoroutine(_autoSaveCoroutine);
    }

    if (PlayerSessionManager.HasInstance)
    {
        PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
    }
}
```

#### 4.2.5 ClaimReward 메서드 수정 (보상 증발 방지)

```csharp
public void ClaimReward()
{
    if (!HasPendingReward || PendingReward == null) return;

    // lazy resolve 시도
    EnsureDependencies();

    // 서비스가 없으면 보상 유지하고 리턴 (보상 증발 방지)
    if (_currencyService == null || _playerStatService == null)
    {
        Debug.LogWarning("[OfflineRewardManager] 서비스가 준비되지 않아 보상을 유지합니다.");
        return;
    }

    // 보상 지급
    _currencyService.AddGold(PendingReward.GoldReward);
    _playerStatService.AddExp(PendingReward.ExpReward);

    // 지급 완료 후에만 상태 변경
    HasPendingReward = false;
    PendingReward = null;

    _ = SaveCurrentTimeAsync();

    Debug.Log("[OfflineRewardManager] 오프라인 보상 지급 완료");
}
```

---

## 5. 초기화 흐름 다이어그램

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Unity Awake (모든 DontDestroySingleton)                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ 1. CurrencyManager.Initialize()                                          │
│    └─ ServiceLocator.Register<ICurrencyService>(this)                   │
│                                                                          │
│ 2. PlayerStatManager.Initialize()                                        │
│    └─ ServiceLocator.Register<IPlayerStatService>(this)                 │
│                                                                          │
│ 3. UpgradeManager.Initialize()                                           │
│    └─ ServiceLocator.IsReady == false                                   │
│    └─ ServiceLocator.OnServicesReady += OnServicesReady                 │
│                                                                          │
│ 4. OfflineRewardManager.Initialize()                                     │
│    └─ ServiceLocator.IsReady == false                                   │
│    └─ ServiceLocator.OnServicesReady += OnServicesReady                 │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│ Unity Start                                                              │
├─────────────────────────────────────────────────────────────────────────┤
│ ServiceInstaller.Start()                                                 │
│    └─ ServiceLocator.MarkAsReady()                                      │
│        └─ OnServicesReady 이벤트 발생                                   │
│            ├─ UpgradeManager.OnServicesReady()                          │
│            │   └─ InitializeDependencies()                              │
│            └─ OfflineRewardManager.OnServicesReady()                    │
│                └─ InitializeDependencies()                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│ 사용 시점 (TryUpgrade, ClaimReward 호출 시)                              │
├─────────────────────────────────────────────────────────────────────────┤
│ EnsureDependencies() 호출 (백업 lazy resolve)                           │
│    └─ _currencyService가 null이면 재시도                                │
│    └─ 폴백: 직접 싱글톤 참조                                            │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 6. 파일 변경 요약

| 파일 | 변경 유형 | 변경 내용 |
|:---|:---|:---|
| `UpgradeManager.cs` | 수정 | 필드 추가, Initialize 수정, InitializeDependencies/EnsureDependencies 추가, OnDestroy 수정, TryUpgrade 수정 |
| `OfflineRewardManager.cs` | 수정 | 필드 추가, Initialize 수정, InitializeDependencies/EnsureDependencies 추가, OnDestroy 수정, ClaimReward 수정 (보상 증발 방지) |

---

## 7. 구현 순서

```
Phase 2-A: UpgradeManager 수정
    ├── 1. _currencyService 필드 추가
    ├── 2. Initialize() 메서드 수정 (OnServicesReady 이벤트 구독)
    ├── 3. OnServicesReady() 메서드 추가
    ├── 4. InitializeDependencies() 메서드 추가
    ├── 5. EnsureDependencies() 메서드 추가
    ├── 6. OnDestroy() 메서드 수정
    └── 7. TryUpgrade() 메서드 수정

Phase 2-B: OfflineRewardManager 수정
    ├── 1. _currencyService, _playerStatService 필드 추가
    ├── 2. Initialize() 메서드 수정 (OnServicesReady 이벤트 구독)
    ├── 3. OnServicesReady() 메서드 추가
    ├── 4. InitializeDependencies() 메서드 추가
    ├── 5. EnsureDependencies() 메서드 추가
    ├── 6. OnDestroy() 메서드 수정
    └── 7. ClaimReward() 메서드 수정 (보상 증발 방지)
```

---

## 8. 테스트 체크리스트

### 8.1 UpgradeManager 테스트

- [ ] 게임 시작 직후 강화 버튼 클릭 시 정상 동작
- [ ] 강화 시 골드 차감 정상 동작
- [ ] 골드 부족 시 강화 실패 메시지 표시
- [ ] 강화 성공 시 레벨 증가 및 이벤트 발생
- [ ] 씬 전환 후 강화 기능 정상 동작

### 8.2 OfflineRewardManager 테스트

- [ ] 앱 재시작 후 오프라인 보상 팝업 표시
- [ ] **보상 수령 시 서비스 null이면 보상 유지됨 (증발 방지)**
- [ ] 보상 수령 시 골드 지급 정상 동작
- [ ] 보상 수령 시 경험치 지급 정상 동작
- [ ] 보상 수령 후 UI 갱신 정상 동작
- [ ] 씬 전환 후 보상 수령 정상 동작

### 8.3 초기화 순서 테스트

- [ ] ServiceInstaller보다 Manager가 먼저 Start() 되어도 정상 동작
- [ ] OnServicesReady 이벤트 정상 발생
- [ ] 이벤트 해제 정상 동작 (메모리 누수 없음)

---

## 9. 롤백 전략

문제 발생 시:
1. Git에서 해당 파일 복원
2. 또는 폴백 코드만 활성화 (ServiceLocator.Resolve 주석 처리)

---

## 10. 향후 개선 방향 (이행기 이후)

| 항목 | 현재 (이행기) | 목표 (완전 전환) |
|:---|:---|:---|
| 의존성 획득 | Manager 내부에서 Resolve | Bootstrapper에서 주입 |
| 폴백 | 싱글톤 폴백 유지 | 폴백 제거 |
| ServiceLocator 의존 | Manager가 직접 호출 | 순수 생성자/메서드 주입 |
| 인터페이스 크기 | 통합 인터페이스 | 필요시 ISP에 따라 분리 |

---

**작성일:** 2026-02-05
**상태:** 검토 대기 (v2 - 초기화 순서/보상 증발 문제 해결)
