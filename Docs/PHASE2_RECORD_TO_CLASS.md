# Phase 2: record → class 전체 변환

## 개요

**목표**: 모든 Data 클래스에서 record 제거, class로 통일, GC 부하 최소화
**범위**: 8개 파일 (Data 클래스) + 5개 파일 (Manager/Controller)
**위험도**: 낮음
**상태**: ✅ 완료

---

## 변환 대상 파일 목록

### record → class 변환 대상 (8개)

| 파일 | 경로 | with 사용 | 비고 | 상태 |
|:---|:---|:---:|:---|:---:|
| `PlayerStat.cs` | `Player/Data/` | ✅ 3곳 | 동적 데이터 | ✅ 완료 |
| `SwordStat.cs` | `Sword/Data/` | ✅ 1곳 | **동적 취급 필요** (강화 적용) | ✅ 완료 |
| `UpgradeData.cs` | `Upgrade/Data/` | ❌ | 메서드 포함 | ✅ 완료 |
| `OfflineRewardData.cs` | `OfflineReward/Data/` | ❌ | **BigInteger 제거** | ✅ 완료 |
| `EnemyStat.cs` | `Enemy/Data/` | ❌ | IsExternalInit 제거 | ✅ 완료 |
| `BossStat.cs` | `Boss/Data/` | ❌ | 정적 데이터 | ✅ 완료 |
| `StageStat.cs` | `Stage/Data/` | ❌ | 정적 데이터 | ✅ 완료 |
| `SoundData.cs` | `Sound/Data/` | ❌ | SfxData + BgmData | ✅ 완료 |

### with 키워드 제거 및 로직 수정 대상 (5개)

| 파일 | 경로 | 수정 내용 | 상태 |
|:---|:---|:---|:---:|
| `PlayerStatManager.cs` | `Player/Manager/` | with 제거, 필드 직접 수정 | ✅ 완료 |
| `UpgradeManager.cs` | `Upgrade/Manager/` | **ApplyUpgrades() 시그니처 변경** | ✅ 완료 |
| `PixelSwordController.cs` | `DamageFloater/.../PixelHunterLike/` | baseStat 캐싱, 강화 적용 방식 변경 | ✅ 완료 |
| `HypoSwordController.cs` | `DamageFloater/.../Hypocycloid/` | baseStat 캐싱, 강화 적용 방식 변경 | ✅ 완료 |
| `AdelFlyingSwordController.cs` | `DamageFloater/.../Adel/` | baseStat 캐싱, 강화 적용 방식 변경 | ✅ 완료 |

---

## 상세 변환 코드

### 1. PlayerStat.cs

**경로**: `Assets/02.Scripts/Player/Data/PlayerStat.cs`

```csharp
public class PlayerStat
{
    public string Id { get; private set; }
    public double BaseMaxHealth { get; set; }
    public float BaseMoveSpeed { get; set; }
    public int Level { get; set; }
    public double CurrentExp { get; set; }
    public double MaxExp { get; set; }

    public PlayerStat(
        string id,
        double baseMaxHealth,
        float baseMoveSpeed,
        int level,
        double currentExp,
        double maxExp)
    {
        Id = id;
        BaseMaxHealth = baseMaxHealth;
        BaseMoveSpeed = baseMoveSpeed;
        Level = level;
        CurrentExp = currentExp;
        MaxExp = maxExp;
    }
}
```

---

### 2. SwordStat.cs (동적 데이터로 취급)

**경로**: `Assets/02.Scripts/Sword/Data/SwordStat.cs`

**중요**: 강화 시 필드가 수정되므로 모든 프로퍼티에 `set` 허용

```csharp
public class SwordStat
{
    public string Id { get; private set; }
    public double AttackDamage { get; set; }
    public float Cooldown { get; set; }
    public float MoveSpeed { get; set; }
    public float CritDamageMultiplier { get; set; }
    public float CritChance { get; set; }

    public SwordStat(
        string id,
        double attackDamage,
        float cooldown,
        float moveSpeed,
        float critDamageMultiplier,
        float critChance)
    {
        Id = id;
        AttackDamage = attackDamage;
        Cooldown = cooldown;
        MoveSpeed = moveSpeed;
        CritDamageMultiplier = critDamageMultiplier;
        CritChance = critChance;
    }

    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
}
```

---

### 3. UpgradeData.cs

**경로**: `Assets/02.Scripts/Upgrade/Data/UpgradeData.cs`

```csharp
using System;

public class UpgradeData
{
    public string Id { get; private set; }
    public string DisplayName { get; private set; }
    public double BaseCost { get; private set; }
    public float CostMultiplier { get; private set; }
    public double BonusPerLevel { get; private set; }

    public UpgradeData(
        string id,
        string displayName,
        double baseCost,
        float costMultiplier,
        double bonusPerLevel)
    {
        Id = id;
        DisplayName = displayName;
        BaseCost = baseCost;
        CostMultiplier = costMultiplier;
        BonusPerLevel = bonusPerLevel;
    }

    public double GetCost(int currentLevel)
    {
        double multiplier = Math.Pow(CostMultiplier, currentLevel);
        return BaseCost * multiplier;
    }

    public float GetTotalBonus(int level)
    {
        return (float)BonusPerLevel * level;
    }

    public double GetTotalDoubleBonus(int level)
    {
        return BonusPerLevel * level;
    }
}
```

---

### 4. OfflineRewardData.cs (BigInteger 제거 포함)

**경로**: `Assets/02.Scripts/OfflineReward/Data/OfflineRewardData.cs`

```csharp
public class OfflineRewardData
{
    public long LastLoginTime { get; set; }
    public double GoldPerMinute { get; set; }
    public double ExpPerMinute { get; set; }

    public OfflineRewardData(long lastLoginTime, double goldPerMinute, double expPerMinute)
    {
        LastLoginTime = lastLoginTime;
        GoldPerMinute = goldPerMinute;
        ExpPerMinute = expPerMinute;
    }
}
```

---

### 5. EnemyStat.cs (IsExternalInit 제거)

**경로**: `Assets/02.Scripts/Enemy/Data/EnemyStat.cs`

```csharp
public class EnemyStat
{
    public string Id { get; private set; }
    public double MaxHP { get; private set; }
    public double AttackDamage { get; private set; }
    public float MoveSpeed { get; private set; }
    public double GoldReward { get; private set; }
    public double Exp { get; private set; }

    public EnemyStat(
        string id,
        double maxHP,
        double attackDamage,
        float moveSpeed,
        double goldReward,
        double exp)
    {
        Id = id;
        MaxHP = maxHP;
        AttackDamage = attackDamage;
        MoveSpeed = moveSpeed;
        GoldReward = goldReward;
        Exp = exp;
    }
}
```

---

### 6. BossStat.cs

**경로**: `Assets/02.Scripts/Boss/Data/BossStat.cs`

```csharp
public class BossStat
{
    public string Id { get; private set; }
    public double MaxHP { get; private set; }
    public double AttackDamage { get; private set; }
    public float MoveSpeed { get; private set; }
    public double GoldReward { get; private set; }
    public double Exp { get; private set; }

    public BossStat(
        string id,
        double maxHP,
        double attackDamage,
        float moveSpeed,
        double goldReward,
        double exp)
    {
        Id = id;
        MaxHP = maxHP;
        AttackDamage = attackDamage;
        MoveSpeed = moveSpeed;
        GoldReward = goldReward;
        Exp = exp;
    }
}
```

---

### 7. StageStat.cs

**경로**: `Assets/02.Scripts/Stage/Data/StageStat.cs`

```csharp
public class StageStat
{
    public int StageId { get; private set; }
    public string StageName { get; private set; }
    public string EnemyStatId { get; private set; }
    public string BossStatId { get; private set; }
    public float HpMultiplier { get; private set; }
    public float AttackMultiplier { get; private set; }
    public float SpeedMultiplier { get; private set; }
    public float GoldMultiplier { get; private set; }
    public float ExpMultiplier { get; private set; }

    public StageStat(
        int stageId,
        string stageName,
        string enemyStatId,
        string bossStatId,
        float hpMultiplier,
        float attackMultiplier,
        float speedMultiplier,
        float goldMultiplier,
        float expMultiplier)
    {
        StageId = stageId;
        StageName = stageName;
        EnemyStatId = enemyStatId;
        BossStatId = bossStatId;
        HpMultiplier = hpMultiplier;
        AttackMultiplier = attackMultiplier;
        SpeedMultiplier = speedMultiplier;
        GoldMultiplier = goldMultiplier;
        ExpMultiplier = expMultiplier;
    }
}
```

---

### 8. SoundData.cs (SfxData + BgmData)

**경로**: `Assets/02.Scripts/Sound/Data/SoundData.cs`

```csharp
public class SfxData
{
    public string Id { get; private set; }
    public float Volume { get; private set; }
    public bool UseRandomPitch { get; private set; }

    public SfxData(string id, float volume, bool useRandomPitch)
    {
        Id = id;
        Volume = volume;
        UseRandomPitch = useRandomPitch;
    }
}

public class BgmData
{
    public string Id { get; private set; }
    public float Volume { get; private set; }

    public BgmData(string id, float volume)
    {
        Id = id;
        Volume = volume;
    }
}
```

---

## Manager/Controller 수정

### 9. PlayerStatManager.cs

**경로**: `Assets/02.Scripts/Player/Manager/PlayerStatManager.cs`

#### 변경 사항

| 라인 | 변경 전 | 변경 후 |
|:---:|:---|:---|
| 2 | `using System.Numerics;` | 제거 |
| 67-70 | `_baseStat = _baseStat with { ... }` | `_baseStat.CurrentExp += exp;` |
| 89-94 | `_baseStat = _baseStat with { ... }` | 개별 필드 직접 수정 |
| 123-128 | `_baseStat = _baseStat with { ... }` | 개별 필드 직접 수정 |

#### 수정된 메서드

```csharp
public void AddExp(double exp)
{
    if (_baseStat == null) return;

    _baseStat.CurrentExp += exp;
    OnExpChanged?.Invoke(_baseStat.CurrentExp, _baseStat.MaxExp);
    CheckLevelUp();
}

private void CheckLevelUp()
{
    if (_baseStat == null) return;

    if (_baseStat.CurrentExp + ExpCompareEpsilon >= _baseStat.MaxExp)
    {
        int newLevel = _baseStat.Level + 1;
        double newMaxExp = CalculateMaxExp(newLevel);

        _baseStat.CurrentExp -= _baseStat.MaxExp;
        _baseStat.Level = newLevel;
        _baseStat.MaxExp = newMaxExp;

        // ... 이벤트 발생 및 저장 로직
        CheckLevelUp();
    }
}

public void SaveExp(int level, double currentExp, double maxExp)
{
    if (_baseStat != null)
    {
        _baseStat.Level = level;
        _baseStat.CurrentExp = currentExp;
        _baseStat.MaxExp = maxExp;
        _repository.Save(_baseStat);
    }
}
```

---

### 10. UpgradeManager.cs (핵심 변경)

**경로**: `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs`

#### 변경 전 (새 객체 반환 - GC 부하)

```csharp
public SwordStat ApplyUpgrades(SwordStat baseStat)
{
    if (_repository == null) return baseStat;

    return baseStat with
    {
        AttackDamage = baseStat.AttackDamage + GetDoubleBonus(...),
        Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(...)),
        // ...
    };
}
```

#### 변경 후 (기존 객체 수정 - GC 없음)

```csharp
// baseStat: DB에서 로드한 원본 데이터
// targetStat: 강화가 적용될 대상 객체
public void ApplyUpgrades(SwordStat baseStat, SwordStat targetStat)
{
    if (_repository == null)
    {
        // repository가 없으면 원본 값 그대로 복사
        targetStat.AttackDamage = baseStat.AttackDamage;
        targetStat.Cooldown = baseStat.Cooldown;
        targetStat.MoveSpeed = baseStat.MoveSpeed;
        targetStat.CritDamageMultiplier = baseStat.CritDamageMultiplier;
        targetStat.CritChance = baseStat.CritChance;
        return;
    }

    targetStat.AttackDamage = baseStat.AttackDamage + GetDoubleBonus(UpgradeId.SwordAttackDamage.ToKey());
    targetStat.Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(UpgradeId.SwordCooldown.ToKey()));
    targetStat.MoveSpeed = baseStat.MoveSpeed + GetBonus(UpgradeId.SwordMoveSpeed.ToKey());
    targetStat.CritDamageMultiplier = baseStat.CritDamageMultiplier + GetBonus(UpgradeId.SwordCritDamage.ToKey());
    targetStat.CritChance = Mathf.Min(1f, baseStat.CritChance + GetBonus(UpgradeId.SwordCritChance.ToKey()));
}
```

**시그니처 변경**: `SwordStat ApplyUpgrades(SwordStat)` → `void ApplyUpgrades(SwordStat, SwordStat)`

---

### 11-13. SwordController 3개 (공통 패턴)

**파일 목록**:
- `PixelSwordController.cs`
- `HypoSwordController.cs`
- `AdelFlyingSwordController.cs`

#### 현재 문제점

```csharp
private void LoadAndApplyUpgrades()
{
    var repository = new SwordStatRepository();  // 매번 새 Repository 생성 (문제!)
    SwordStat baseStat = repository.GetById(_swordStatId);
    _swordStat = UpgradeManager.Instance.ApplyUpgrades(baseStat);  // 매번 새 객체 생성 (문제!)
}
```

#### 변경 후 (캐싱 + 필드 수정)

```csharp
[Header("■ Sword Stat")]
[SerializeField] private string _swordStatId = "PIXEL_SWORD";  // 각 Controller별로 다름

private SwordStat _baseStat;    // 원본 데이터 (DB에서 로드, 변경 안 함)
private SwordStat _swordStat;   // 강화 적용된 데이터 (필드만 수정)
private bool _isInitialized;

public SwordStat SwordStat => _swordStat;

private void Awake()
{
    LoadBaseStat();
    ApplyUpgrades();
}

private void LoadBaseStat()
{
    if (_isInitialized) return;

    var repository = new SwordStatRepository();
    _baseStat = repository.GetById(_swordStatId);

    // 강화 적용용 객체 생성 (게임 시작 시 1회만)
    _swordStat = new SwordStat(
        _baseStat.Id,
        _baseStat.AttackDamage,
        _baseStat.Cooldown,
        _baseStat.MoveSpeed,
        _baseStat.CritDamageMultiplier,
        _baseStat.CritChance
    );

    _isInitialized = true;
}

private void ApplyUpgrades()
{
    if (_baseStat == null || _swordStat == null) return;

    if (UpgradeManager.Instance != null)
    {
        UpgradeManager.Instance.ApplyUpgrades(_baseStat, _swordStat);
    }
    else
    {
        // Manager 없으면 원본 값 복사
        _swordStat.AttackDamage = _baseStat.AttackDamage;
        _swordStat.Cooldown = _baseStat.Cooldown;
        _swordStat.MoveSpeed = _baseStat.MoveSpeed;
        _swordStat.CritDamageMultiplier = _baseStat.CritDamageMultiplier;
        _swordStat.CritChance = _baseStat.CritChance;
    }
}

private void OnUpgradeManagerInitialized()
{
    ApplyUpgrades();  // Repository 재로드 없음, 필드만 수정
    UpdateActiveSwords();
}

private void OnUpgradeChanged(string upgradeId, int newLevel)
{
    if (upgradeId.StartsWith("Sword_"))
    {
        ApplyUpgrades();  // Repository 재로드 없음, 필드만 수정
        UpdateActiveSwords();
    }
}

private void UpdateActiveSwords()
{
    foreach (var sword in _activeSwords)
    {
        if (sword != null)
        {
            sword.InitializeStat(_swordStat);
        }
    }
}
```

---

## 작업 순서

### Step 1: Data 클래스 변환 (8개)
1. `PlayerStat.cs` → class 변환
2. `SwordStat.cs` → class 변환 (모든 필드 set 허용)
3. `UpgradeData.cs` → class 변환
4. `OfflineRewardData.cs` → class 변환 + BigInteger 제거
5. `EnemyStat.cs` → class 변환 + IsExternalInit 제거
6. `BossStat.cs` → class 변환
7. `StageStat.cs` → class 변환
8. `SoundData.cs` → class 변환 (SfxData, BgmData)

### Step 2: Manager 수정 (2개)
9. `PlayerStatManager.cs` → with 제거 + using 제거
10. `UpgradeManager.cs` → ApplyUpgrades 시그니처 변경

### Step 3: Controller 수정 (3개)
11. `PixelSwordController.cs` → baseStat 캐싱 + ApplyUpgrades 호출 방식 변경
12. `HypoSwordController.cs` → baseStat 캐싱 + ApplyUpgrades 호출 방식 변경
13. `AdelFlyingSwordController.cs` → baseStat 캐싱 + ApplyUpgrades 호출 방식 변경

### Step 4: 검증
14. Unity 에디터 컴파일 확인
15. 플레이 테스트

---

## 성능 개선 효과

### 강화 버튼 연타 시나리오 (1초에 5회 강화)

#### 변경 전
| 항목 | 생성 횟수 |
|:---|:---:|
| `SwordStatRepository` | 5개 |
| `SwordStat` (baseStat) | 5개 |
| `SwordStat` (upgraded) | 5개 |
| **총 힙 할당** | **15개 객체** |

#### 변경 후
| 항목 | 생성 횟수 |
|:---|:---:|
| `SwordStatRepository` | 0개 (캐싱) |
| `SwordStat` (baseStat) | 0개 (캐싱) |
| `SwordStat` (upgraded) | 0개 (필드 수정) |
| **총 힙 할당** | **0개 객체** |

---

## 검증 체크리스트

### 컴파일 확인
- [ ] Unity 에디터 컴파일 에러 없음

### 기능 테스트
- [ ] 플레이어 경험치 획득/레벨업 정상
- [ ] 검 데이터 로드 정상
- [ ] **강화 적용 정상 (연타 테스트)**
- [ ] 강화 후 검 스탯 즉시 반영
- [ ] 적/보스 스탯 로드 정상
- [ ] 스테이지 데이터 로드 정상
- [ ] 사운드 재생 정상
- [ ] 오프라인 보상 정상

### 성능 테스트
- [ ] Profiler에서 강화 시 GC Alloc 없음 확인

---

## 구현 완료 요약 (2026-02-02)

### 변경된 파일 (13개)

**Data 클래스 (8개)**
1. `PlayerStat.cs` - record → class, `set` 허용
2. `SwordStat.cs` - record → class, `set` 허용, `CalculateDamage()` 메서드 유지
3. `UpgradeData.cs` - record → class, `private set` 유지
4. `OfflineRewardData.cs` - record → class, `BigInteger` → `double` 변환
5. `EnemyStat.cs` - record → class, `IsExternalInit` 제거
6. `BossStat.cs` - record → class
7. `StageStat.cs` - record → class
8. `SoundData.cs` - SfxData, BgmData 모두 record → class

**Manager (2개)**
9. `PlayerStatManager.cs` - `with` 키워드 제거, `using System.Numerics` 제거
10. `UpgradeManager.cs` - `ApplyUpgrades(SwordStat, SwordStat)` 시그니처 변경

**Controller (3개)**
11. `PixelSwordController.cs` - `_baseStat`, `_swordStat` 캐싱 패턴 적용
12. `HypoSwordController.cs` - `_baseStat`, `_swordStat` 캐싱 패턴 적용
13. `AdelFlyingSwordController.cs` - `_baseStat`, `_swordStat` 캐싱 패턴 적용

---

## 추가 참고사항

### 정적 데이터 vs 동적 데이터

| 유형 | 클래스 | 특징 | setter |
|:---|:---|:---|:---:|
| **동적** | `PlayerStat` | 런타임에 값 변경 | `set` |
| **동적** | `SwordStat` | 강화 시 값 변경 | `set` |
| **정적** | 나머지 전부 | DB 로드 후 변경 없음 | `private set` |
