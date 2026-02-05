# Manager 계층 SOLID 원칙 검수 보고서

**검수 대상**: Assets/02.Scripts 내 모든 Manager 클래스
**검수 기준**: SOLID 원칙 (SRP, OCP, LSP, ISP, DIP)
**검수 일자**: 2026-02-04

---

## 검수 대상 Manager 목록

| 순번 | Manager 파일 | 라인 수 | 책임 영역 |
|------|-------------|---------|----------|
| 1 | GameManager.cs | 70 | 플레이어 생사 관리 |
| 2 | CurrencyManager.cs | 216 | 재화 관리 |
| 3 | UpgradeManager.cs | 173 | 강화 시스템 |
| 4 | StageManager.cs | 242 | 스테이지 관리 |
| 5 | PlayerStatManager.cs | 121 | 플레이어 스탯 |
| 6 | PlayerSessionManager.cs | 187 | 플레이어 세션/인증 |
| 7 | OfflineRewardManager.cs | 275 | 오프라인 보상 |
| 8 | EffectManager.cs | 186 | 이펙트 관리 |
| 9 | SoundManager.cs | 466 | 사운드 관리 |
| 10 | ControllerManager.cs | 144 | 검 컨트롤러 관리 |
| 11 | DamageFloaterManager.cs | 95 | 데미지 표시 |
| 12 | PopupManager.cs | 326 | 팝업 관리 |
| 13 | RedDotManager.cs | 192 | 레드닛 관리 |
| 14 | FadeManager.cs | 91 | 페이드 인/아웃 |

---

## SOLID 원칙 개요

| 원칙 | 약어 | 설명 |
|------|------|------|
| Single Responsibility | SRP | 클래스는 하나의 책임만 가짐 |
| Open/Closed | OCP | 확장에는 열려 있고, 수정에는 닫혀 있음 |
| Liskov Substitution | LSP | 자식 클래스는 부모 클래스를 완전히 대체 가능해야 함 |
| Interface Segregation | ISP | 클라이언트는 사용하지 않는 인터페이스에 의존하지 않음 |
| Dependency Inversion | DIP | 상위 모듈은 하위 모듈이 아닌 추상화에 의존 |

---

## 상세 검수 결과

### 1. GameManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ✅ **준수**
- **이유**: 플레이어 사망/부활 관리라는 하나의 명확한 책임만 가짐

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 죽음 처리 로직이 하드코딩되어 있어 새로운 죽음 유형 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `StageManager.Instance`, `PlayerHealth` 구체 클래스에 직접 의존

```csharp
// 문제 코드
if (StageManager.Instance != null)
{
    StageManager.Instance.OnPlayerDied();
}
```

**개선 제안**:
```csharp
// 인터페이스 도입
public interface IStageHandler
{
    void OnPlayerDied();
}

public class GameManager : DontDestroySingleton<GameManager>
{
    private IStageHandler _stageHandler;

    public void SetStageHandler(IStageHandler handler)
    {
        _stageHandler = handler;
    }

    private void HandlePlayerDeath()
    {
        OnPlayerDeath?.Invoke();
        _stageHandler?.OnPlayerDied();
        StartCoroutine(RespawnSequence());
    }
}
```

---

### 2. CurrencyManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 재화 로드 (`LoadCurrency`)
  - 재화 저장 (`SaveGold`, `SaveRuby`)
  - 자동 저장 코루틴 관리 (`AutoSaveGoldRoutine`)
  - 재화 변경 이벤트 처리
  - 로그인 초기화 대기

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 통화 타입 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - `PlayerSessionManager.Instance` 구체 클래스 의존
  - `Currency` 구체 클래스 의존

```csharp
// 문제 코드
PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;
```

**개선 제안**: 책임 분리
```csharp
// 1. 재화 저장 관리 분리
public interface ICurrencyPersister
{
    Task SaveAsync(Currency currency);
    Task<Currency> LoadAsync();
}

public class CurrencyManager : DontDestroySingleton<CurrencyManager>
{
    private ICurrencyPersister _persister;
    private readonly CurrencyOperations _operations;

    public CurrencyManager(ICurrencyPersister persister)
    {
        _persister = persister;
        _operations = new CurrencyOperations();
    }

    // Manager는 오케스트레이션만 담당
    public void AddGold(double amount)
    {
        _operations.AddGold(_currency, amount);
        _persister.SaveAsync(_currency);
    }
}
```

---

### 3. UpgradeManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 강화 시도 (`TryUpgrade`)
  - 보너스 계산/적용 (`ApplyUpgrades`)
  - 데이터 조회 (`GetCost`, `GetLevel`)

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 강화 유형 추가 시 `ApplyUpgrades` 메서드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - `CurrencyManager.Instance` 구체 클래스 의존
  - `SwordStat` 구체 클래스 의존

```csharp
// 문제 코드
if (!CurrencyManager.Instance.TrySpendGold(cost))
{
    Debug.Log($"[UpgradeManager] 골드 부족: ...");
    return false;
}
```

**개선 제안**: 책임 분리 + 전략 패턴
```csharp
// 보너스 적용 전략 분리
public interface IUpgradeApplicator
{
    void Apply(string upgradeId, int level, SwordStat targetStat);
}

public class SwordUpgradeApplicator : IUpgradeApplicator
{
    private readonly IUpgradeRepository _repository;

    public void Apply(string upgradeId, int level, SwordStat targetStat)
    {
        // 검 보너스 적용 로직
    }
}

public class PlayerUpgradeApplicator : IUpgradeApplicator
{
    private readonly IUpgradeRepository _repository;

    public void Apply(string upgradeId, int level, SwordStat targetStat)
    {
        // 플레이어 보너스 적용 로직
    }
}
```

---

### 4. StageManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 스테이지 진행 관리
  - 적 스폰 코루틴 관리
  - 보스 스폰 트리거
  - EnemySpawner 직접 제어

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 스테이지 전환 로직이 하드코딩됨

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `EnemySpawner.Instance`, `GameManager.Instance`, `SoundManager.Instance` 구체 클래스 의존

```csharp
// 문제 코드: EnemySpawner 직접 제어
EnemySpawner.Instance?.SpawnWithMultiplier(stage.EnemyStatId, stage);
EnemySpawner.Instance?.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
EnemySpawner.Instance?.ResetBossState();
EnemySpawner.Instance?.ReturnAll();
```

**개선 제안**: 책임 분리
```csharp
// 스폰 관리 분리
public interface IEnemySpawner
{
    void Spawn(string enemyStatId, StageStat stage);
    void SpawnBoss(string bossStatId, StageStat stage);
    void ResetBoss();
    void ReturnAll();
    int AliveCount { get; }
    bool IsBossAlive { get; }
    event Action<EnemyAI> OnBossDied;
}

public class StageManager : Singleton<StageManager>
{
    private IEnemySpawner _enemySpawner;

    public void SetEnemySpawner(IEnemySpawner spawner)
    {
        _enemySpawner = spawner;
    }

    // 스테이지 진행 관리에 집중
}
```

---

### 5. PlayerStatManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ⚠️ **경계 위반**
- **이유**: 경험치 관리와 레벨업 로직이 섞여 있음

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 경험치 곡선이 하드코딩됨

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `PlayerSessionManager.Instance` 구체 클래스 의존

```csharp
// 문제 코드: 경험치 곡선 하드코딩
private double CalculateMaxExp(int level)
{
    return 10.0 * System.Math.Pow(2, level - 1);
}
```

**개선 제안**:
```csharp
// 경험치 곡선 전략
public interface IExpCurve
{
    double CalculateMaxExp(int level);
}

public class ExponentialExpCurve : IExpCurve
{
    private readonly double _baseExp;
    private readonly double _multiplier;

    public double CalculateMaxExp(int level)
    {
        return _baseExp * System.Math.Pow(_multiplier, level - 1);
    }
}
```

---

### 6. PlayerSessionManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 인증/로그인 (`Login`, `Logout`)
  - 닉네임 검증 (`ValidateNickname`)
  - 플레이어 데이터 생성 (`CreatePlayerInDatabase`)
  - DB 직접 접근

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 검증 규칙 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **심각한 위반**
- **이유**: `BGRepo` 구체 클래스에 직접 의존 (Data Access Layer 침범)

```csharp
// 문제 코드: Manager가 직접 DB 접근
BGMetaEntity meta = BGRepo.I[PlayerProfileTableName];
int count = meta.CountEntities;

for (int i = 0; i < count; i++)
{
    BGEntity entity = meta.GetEntity(i);
    if (entity.Name == playerName)
        return true;
}
```

**개선 제안**: 책임 분리 + Repository 패턴
```csharp
// 1. 인증 관리 분리
public interface IAuthService
{
    bool Login(string playerName);
    void Logout();
    string GetCurrentPlayerName();
}

public interface INicknameValidator
{
    NicknameValidationResult Validate(string nickname);
}

// 2. DB 접근 분리 (이미 Repository 있어야 함)
public interface IPlayerRepository
{
    bool PlayerExists(string playerName);
    void CreateProfile(string playerName);
    void CreateStat(string playerName);
}

// 3. PlayerSessionManager는 오케스트레이션만 담당
public class PlayerSessionManager : DontDestroySingleton<PlayerSessionManager>
{
    private IAuthService _authService;
    private IPlayerRepository _playerRepository;

    public void Login(string playerName)
    {
        if (!_authService.Login(playerName))
            return;

        if (!_playerRepository.PlayerExists(playerName))
        {
            _playerRepository.CreateProfile(playerName);
            _playerRepository.CreateStat(playerName);
        }
    }
}
```

---

### 7. OfflineRewardManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 오프라인 보상 계산
  - 자동 저장 코루틴 관리
  - 시간 추적

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 보상 계산 로직이 하드코딩됨

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - `CurrencyManager.Instance`, `PlayerStatManager.Instance` 구체 클래스 의존
  - `IOfflineRewardRepository` 인터페이스 의존하나 사용하지 않는 책임 포함

```csharp
// 문제 코드
public void ClaimReward()
{
    if (CurrencyManager.Instance != null)
    {
        CurrencyManager.Instance.AddGold(PendingReward.GoldReward);
    }

    if (PlayerStatManager.Instance != null)
    {
        PlayerStatManager.Instance.AddExp(PendingReward.ExpReward);
    }
}
```

**개선 제안**:
```csharp
// 보상 지급 인터페이스
public interface IRewardClaimer
{
    void Claim(OfflineRewardResult reward);
}

public class OfflineRewardClaimer : IRewardClaimer
{
    private readonly ICurrencyManager _currencyManager;
    private readonly IPlayerStatManager _statManager;

    public void Claim(OfflineRewardResult reward)
    {
        _currencyManager?.AddGold(reward.GoldReward);
        _statManager?.AddExp(reward.ExpReward);
    }
}
```

---

### 8. EffectManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 오브젝트 풀 관리
  - VFX 재생
  - 카메라 쉐이크 (`PlayHitCameraShake`, `PlayCameraShake`)

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 VFX 타입 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `QuarterViewCamera.Instance` 구체 클래스 의존

```csharp
// 문제 코드: 카메라 쉐이크 책임 섞여 있음
public void PlayHitCameraShake()
{
    if (QuarterViewCamera.Instance != null)
    {
        QuarterViewCamera.Instance.Shake();
    }
}
```

**개선 제안**: 책임 분리
```csharp
// VFX 관리와 카메라 쉐이크 분리
public interface IVfxManager
{
    void PlayHitVfx(Vector3 position);
    void PlayHitVfxByIndex(int index, Vector3 position);
    void PlaySkillVfx(Vector3 position);
}

public interface ICameraShaker
{
    void Shake();
    void Shake(float duration, float strength, int vibrato);
}

public class EffectManager : Singleton<EffectManager>
{
    private IVfxManager _vfxManager;
    private ICameraShaker _cameraShaker;

    public void PlayHitVfx(Vector3 position) => _vfxManager?.PlayHitVfx(position);
    public void Shake() => _cameraShaker?.Shake();
}
```

---

### 9. SoundManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **심각한 위반**
- **이유** (466줄, 다중 책임):
  - SFX 재생 및 관리
  - BGM 재생 및 크로스페이드
  - 오브젝트 풀 관리
  - 볼륨 제어
  - 스로틀링
  - 사운드 라이브러리 관리
  - AudioMixer 제어

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 사운드 타입 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `AudioMixer`, `SoundLibrarySO` 구체 클래스 의존

```csharp
// 문제 코드: 너무 많은 책임
public class SoundManager : DontDestroySingleton<SoundManager>
{
    private Queue<AudioSource> _sfxPool;           // 풀 관리
    private AudioSource _bgmSourceA, _bgmSourceB;   // BGM 관리
    private Dictionary<AudioClip, float> _lastPlayTimes;  // 스로틀링
    private AudioMixer _audioMixer;                 // 볼륨 제어
    private ISoundRepository _repository;           // 데이터 관리

    // 466줄의 복잡한 코드
}
```

**개선 제안**: 책임 분리 (가장 시급)
```csharp
// 1. SFX Manager 분리
public interface ISfxManager
{
    void Play(ESfxId sfxId);
    void Play(ESfxId sfxId, Vector3 position);
    void Play(AudioClip clip, float volume = 1.0f);
    void SetVolume(float volume);
}

public class SfxManager : ISfxManager
{
    private readonly IAudioSourcePool _pool;
    private readonly ISfxThrottler _throttler;
    private readonly ISoundLibrary _library;

    public void Play(ESfxId sfxId)
    {
        AudioClip clip = _library.GetSfxClip(sfxId);
        _throttler.Throttle(clip);
        var source = _pool.Get();
        source.Play(clip);
    }
}

// 2. BGM Manager 분리
public interface IBgmManager
{
    void Play(EBgmId bgmId);
    void Play(AudioClip clip);
    void Stop();
    void SetVolume(float volume);
}

public class BgmManager : IBgmManager
{
    private readonly IAudioSource _sourceA, _sourceB;
    private readonly IAudioMixerController _mixer;
    private readonly ISoundLibrary _library;

    public void Play(EBgmId bgmId)
    {
        AudioClip clip = _library.GetBgmClip(bgmId);
        CrossFade(clip);
    }
}

// 3. SoundManager는 오케스트레이션만 담당
public class SoundManager : DontDestroySingleton<SoundManager>
{
    private readonly ISfxManager _sfxManager;
    private readonly IBgmManager _bgmManager;
    private readonly IAudioVolumeController _volumeController;

    public void PlaySFX(ESfxId sfxId) => _sfxManager.Play(sfxId);
    public void PlayBGM(EBgmId bgmId) => _bgmManager.Play(bgmId);
    public void SetMasterVolume(float volume) => _volumeController.SetMaster(volume);
}
```

---

### 10. ControllerManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 3개의 검 컨트롤러 관리
  - 쿨타임 추적 및 발사 로직
  - 자동 발사 제어

#### OCP (개방-폐쇄 원칙)
- **평가**: ❌ **위반**
- **이유**: 새로운 검 타입 추가 시 코드 수정 필요

```csharp
// 문제 코드
switch (type)
{
    case ESwordType.Adel:
        _adelController?.Fire();
        _adelCooldownTimer = 0f;
        break;
    case ESwordType.Hypo:
        _hypoController?.Fire();
        _hypoCooldownTimer = 0f;
        break;
    case ESwordType.Pixel:
        _pixelController?.Fire();
        _pixelCooldownTimer = 0f;
        break;
}
```

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ❌ **위반**
- **이유**: `AdelFlyingSwordController`, `HypoSwordController`, `PixelSwordController` 구체 클래스 의존

**개선 제안**: 전략 패턴
```csharp
// 검 컨트롤러 인터페이스
public interface ISwordController
{
    void Fire();
    float Cooldown { get; }
}

// 검 발사 관리자
public class SwordFireManager
{
    private readonly Dictionary<ESwordType, SwordFireContext> _contexts;

    public void Fire(ESwordType type)
    {
        if (_contexts.TryGetValue(type, out var context))
        {
            context.Fire();
        }
    }

    public void UpdateAll(float deltaTime)
    {
        foreach (var context in _contexts.Values)
        {
            context.Update(deltaTime);
        }
    }
}

// 개별 검 컨텍스트
public class SwordFireContext
{
    private readonly ISwordController _controller;
    private float _cooldownTimer;

    public void Update(float deltaTime)
    {
        _cooldownTimer += deltaTime;
        if (_cooldownTimer >= _controller.Cooldown)
        {
            _controller.Fire();
            _cooldownTimer = 0f;
        }
    }
}
```

---

### 11. DamageFloaterManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ✅ **준수**
- **이유**: 데미지 플로터 인스턴싱 및 옵션 적용만 담당

#### OCP (개방-폐쇄 원칙)
- **평가**: ⚠️ **부분적 위반**
- **이유**: 새로운 스타일 추가 시 코드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ⚠️ **경계 위반**
- **이유**: `DamageFloater` 구체 클래스 의존

```csharp
// 문제 코드: 디버그용 잔여 코드
public GameObject SpawnPos; // temp
public bool IsMulti;
private readonly List<int> _tempList = new List<int>();
```

**개선 제안**: 디버그 코드 제거
```csharp
public class DamageFloaterManager : Singleton<DamageFloaterManager>
{
    [SerializeField] private GameObject _damageFloaterPrefab;

    public void ShowDamage(EDamageStyle style, double damage, Vector3 spawnPoint, bool isCrit)
    {
        if (_damageFloaterPrefab == null) return;

        GameObject obj = Instantiate(_damageFloaterPrefab, spawnPoint, Quaternion.identity);
        DamageFloater floater = obj.GetComponent<DamageFloater>();

        if (floater != null)
        {
            floater.ApplyOption(SingleFloaterOption);
            string formattedDamage = CurrencyFormatter.FormatAbbreviated(damage);
            floater.ShowFormattedDamage(formattedDamage, style, isCrit);
        }
    }
}
```

---

### 12. PopupManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 팝업 스택 관리
  - 팝업 등록/해제
  - SortingOrder 계산
  - Blocker 관리
  - Priority 스택 관리

#### OCP (개방-폐쇄 원칙)
- **평가**: ✅ **준수**
- **이유**: 새로운 PopupType 추가 시 enum만 추가하면 됨

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ⚠️ **경계 위반**
- **이유**: `PopupBase` 추상 클래스 의존하나 `PopupBlocker` 구체 클래스 직접 생성

```csharp
// 문제 코드
private PopupBlocker GetBlockerFromPool()
{
    if (_blockerPool.Count > 0)
    {
        return _blockerPool.Pop();
    }

    if (_blockerPrefab != null)
    {
        return Instantiate(_blockerPrefab, transform);
    }

    GameObject blockerObj = new GameObject("PopupBlocker");
    blockerObj.transform.SetParent(transform, false);
    Canvas canvas = blockerObj.AddComponent<Canvas>();
    blockerObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    return blockerObj.AddComponent<PopupBlocker>();
}
```

**개선 제안**: Blocker 팩토리 분리
```csharp
// Blocker 팩토리 인터페이스
public interface IBlockerFactory
{
    PopupBlocker Create(Transform parent);
}

public class PopupManager : DontDestroySingleton<PopupManager>
{
    private readonly IBlockerFactory _blockerFactory;

    public PopupManager(IBlockerFactory blockerFactory)
    {
        _blockerFactory = blockerFactory;
    }

    private PopupBlocker GetBlockerFromPool()
    {
        if (_blockerPool.Count > 0)
        {
            return _blockerPool.Pop();
        }

        return _blockerFactory.Create(transform);
    }
}
```

---

### 13. RedDotManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ❌ **위반**
- **이유**:
  - 노드 트리 구축 (`BuildTree`)
  - 조건 등록 및 평가
  - 이벤트 전파

#### OCP (개방-폐쇄 원칙)
- **평가**: ❌ **위반**
- **이유**: 새로운 RedDotKey 추가 시 `BuildTree` 메서드 수정 필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ⚠️ **경계 위반**
- **이유**: `RedDotNode`, `IRedDotCondition` 의존하나 하드코딩된 조건 등록

```csharp
// 문제 코드: 하드코딩된 트리 구축
private void BuildTree()
{
    var mainMenu = CreateNode(RedDotKey.MainMenu);
    var upgrade = CreateNode(RedDotKey.Upgrade);
    mainMenu.AddChild(upgrade);

    var upgradePlayer = CreateNode(RedDotKey.UpgradePlayer);
    upgrade.AddChild(upgradePlayer);

    // ... 반복
}
```

**개선 제안**: 설정 기반 트리 구축
```csharp
// 트리 구성 설정
[System.Serializable]
public class RedDotTreeConfig
{
    public RedDotKey Root;
    public RedDotNodeConfig[] Nodes;
}

[System.Serializable]
public class RedDotNodeConfig
{
    public RedDotKey Key;
    public RedDotKey Parent;
    public string ConditionType;
    public string ConditionData;
}

// 설정 기반으로 트리 구축
public class RedDotManager : DontDestroySingleton<RedDotManager>
{
    private readonly RedDotTreeConfig _config;

    private void BuildTree()
    {
        foreach (var nodeConfig in _config.Nodes)
        {
            var node = CreateNode(nodeConfig.Key);

            if (nodeConfig.Parent != RedDotKey.None)
            {
                var parentNode = _nodes[nodeConfig.Parent];
                parentNode.AddChild(node);
            }

            // 조건 등록
            RegisterCondition(nodeConfig.Key, nodeConfig);
        }
    }
}
```

---

### 14. FadeManager.cs

#### SRP (단일 책임 원칙)
- **평가**: ✅ **준수**
- **이유**: Fade in/out 관리만 담당

#### OCP (개방-폐쇄 원칙)
- **평가**: ✅ **준수**
- **이유**: 새로운 Fade 타입 추가 시 기존 코드 수정 불필요

#### LSP (리스코프 치환 원칙)
- **평가**: N/A (상속 관계 없음)

#### ISP (인터페이스 분리 원칙)
- **평가**: N/A (인터페이스 없음)

#### DIP (의존성 역전 원칙)
- **평가**: ⚠️ **경계 위반**
- **이유**: `FadeUI` 구체 클래스 의존

```csharp
// 문제 코드
private void FindAndBindFadeUI()
{
    FadeUI fadeUI = FindFirstObjectByType<FadeUI>();
    if (fadeUI != null)
    {
        BindFadeUI(fadeUI);
    }
}
```

**개선 제안**:
```csharp
// Fade UI 인터페이스
public interface IFadeUI
{
    Image Image { get; }
}

public class FadeManager : DontDestroySingleton<FadeManager>
{
    private IFadeUI _fadeUI;

    public void BindFadeUI(IFadeUI fadeUI)
    {
        _fadeUI = fadeUI;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var fadeUI = FindFirstObjectByType<FadeUI>();
        if (fadeUI != null)
        {
            BindFadeUI(fadeUI);
        }
    }
}
```

---

## 종합 분석

### SOLID 원칙 준수 현황

| 원칙 | 준수 | 부분적 | 위반 | 비고 |
|------|------|--------|------|------|
| **SRP** | 2 | 1 | 11 | 79% 위반 |
| **OCP** | 2 | 10 | 2 | 86% 부분적 위반 |
| **LSP** | - | - | - | 상속 관계 없음 |
| **ISP** | - | - | - | 인터페이스 거의 없음 |
| **DIP** | - | 2 | 12 | 86% 위반 |

### 주요 문제 요약

#### 1. 단일 책임 원칙 (SRP) 위반 - 심각

**위반 사례**:
- `SoundManager`: 466줄, 6개 이상의 책임 (SFX, BGM, 풀링, 볼륨, 스로틀링, 라이브러리)
- `CurrencyManager`: 로드, 저장, 자동 저장, 이벤트, 초기화 대기
- `UpgradeManager`: 강화, 보너스 계산, 데이터 조회
- `StageManager`: 스테이지 진행, 적 스폰, 보스 스폰

**영향**:
- 클래스가 너무 커짐 (SoundManager 466줄)
- 유지보수 어려움
- 테스트 불가능

#### 2. 의존성 역전 원칙 (DIP) 위반 - 심각

**위반 패턴**:
```csharp
// Manager 간 직접 참조
CurrencyManager.Instance
PlayerSessionManager.Instance
StageManager.Instance
EnemySpawner.Instance
GameManager.Instance
```

**영향**:
- 강한 결합도 (Tight Coupling)
- 테스트 불가능
- 유연성 저하

#### 3. 인터페이스 부재 - 심각

**현황**:
- 14개 Manager 중 1개도 인터페이스가 없음
- `ICurrencyRepository`, `IUpgradeRepository`, `IStageRepository` 등 Repository 인터페이스는 있으나 Manager 자체에 대한 인터페이스 없음

**영향**:
- 확장 불가능
- 대체 불가능
- 테스트 더블 불가능

#### 4. 하드코딩된 로직 - 심각

**사례**:
- 경험치 곡선: `10.0 * Math.Pow(2, level - 1)`
- 레드닛 트리: `BuildTree()` 메서드 내 하드코딩
- 검 컨트롤러: switch 문으로 3가지 타입 하드코딩

---

## 개선 우선순위

### 긴급 (즉시 개선 필요)

#### 1. SoundManager 책임 분리
- **문제**: 466줄, 6개 이상의 책임
- **해결**:
  - `SfxManager` 분리
  - `BgmManager` 분리
  - `AudioVolumeController` 분리
- **예상 효과**: 각 클래스 ~100줄로 축소

#### 2. PlayerSessionManager 책임 분리 + DB 접근 분리
- **문제**: Manager가 BGDatabase에 직접 접근
- **해결**:
  - `IAuthService` 분리
  - `INicknameValidator` 분리
  - `IPlayerRepository` 통해 DB 접근
- **예상 효과**: Data Access Layer 침범 해결

#### 3. Manager 간 결합도 감소
- **문제**: `XxxManager.Instance` 직접 참조
- **해결**:
  - 필요한 인터페이스 도입
  - 생성자/설정 메서드로 주입
  - 이벤트 기반 통신으로 전환
- **예상 효과**: 테스트 가능성 확보, 유연성 향상

### 중간 (개선 권장)

#### 4. UpgradeManager 책임 분리
- **문제**: 강화, 보너스 계산, 데이터 조회
- **해결**: `IUpgradeApplicator` 전략 패턴 도입

#### 5. StageManager 책임 분리
- **문제**: 스테이지 진행, 적 스폰
- **해결**: `IEnemySpawner` 인터페이스 도입

#### 6. CurrencyManager 책임 분리
- **문제**: 로드, 저장, 자동 저장, 이벤트
- **해결**: `ICurrencyPersister` 분리

### 낮음 (프로덕션 필수 아님)

#### 7. 하드코딩된 로직 설정 파일화
- 경험치 곡선
- 레드닛 트리
- 검 컨트롤러

#### 8. 인터페이스 도입
- 각 Manager에 인터페이스 정의
- 테스트 더블 활성화

---

## 추천 리팩토링 접근법

### 단계 1: 인터페이스 정의
```
ICurrencyManager
IUpgradeManager
IStageManager
ISoundManager
...
```

### 단계 2: 책임 분리
```
SoundManager → SfxManager + BgmManager + VolumeController
CurrencyManager → CurrencyManager + CurrencyPersister
PlayerSessionManager → AuthService + PlayerRepository
```

### 단계 3: 의존성 주입
```csharp
// 기존
CurrencyManager.Instance.TrySpendGold(cost);

// 개선
public class UpgradeManager
{
    private ICurrencyManager _currencyManager;

    public UpgradeManager(ICurrencyManager currencyManager)
    {
        _currencyManager = currencyManager;
    }

    private bool TrySpendGold(double cost)
    {
        return _currencyManager.TrySpendGold(cost);
    }
}
```

### 단계 4: 이벤트 기반 통신
```csharp
// 기존: 직접 참조
if (StageManager.Instance != null)
{
    StageManager.Instance.OnPlayerDied();
}

// 개선: 이벤트 기반
public interface IPlayerDeathHandler
{
    void OnPlayerDeath();
}

public class StageManager : IPlayerDeathHandler
{
    public void OnPlayerDeath()
    {
        // 플레이어 사망 처리
    }
}

public class GameManager
{
    private List<IPlayerDeathHandler> _deathHandlers;

    private void HandlePlayerDeath()
    {
        foreach (var handler in _deathHandlers)
        {
            handler.OnPlayerDeath();
        }
    }
}
```

---

## 결론

### 전체 평가: **60/100점**

| 평가 항목 | 점수 | 비고 |
|----------|------|------|
| SRP 준수 | 14/100 | 11개 Manager 위반 |
| OCP 준수 | 50/100 | 10개 Manager 부분적 위반 |
| LSP 준수 | N/A | 상속 관계 없음 |
| ISP 준수 | 0/100 | 인터페이스 부재 |
| DIP 준수 | 14/100 | 12개 Manager 위반 |

### 핵심 문제점
1. **Manager가 너무 많은 책임을 가짐** (SRP 위반 79%)
2. **Manager 간 강한 결합** (DIP 위반 86%)
3. **인터페이스 부재로 확장/대체 불가**
4. **하드코딩된 로직으로 유연성 저하**

### 긍정적 요소
1. **싱글톤 패턴 일관성**: 모든 Manager가 `DontDestroySingleton<T>` 사용
2. **이벤트 기반 UI 통신**: Manager가 이벤트를 발행하고 UI가 구독하는 구조 (DDD 4계층 준수)
3. **Repository 패턴 사용**: Data Layer에 인터페이스 정의

### 향후 방향
1. **단계적 책임 분리**: SoundManager부터 시작하여 하나씩 분리
2. **의존성 주입 프레임워크 도입 고려** (Zenject/VContainer)
3. **인터페이스 우선 설계**: 새로운 기능 개발 시 인터페이스부터 정의
4. **통합 테스트 작성**: 의존성 주입 후 테스트 가능성 확보

---

**검수자**: AI Agent (Sisyphus - Ultrawork Mode)
**검수 일자**: 2026-02-04
**문서 버전**: 1.0
