# Phase 1: BGDatabase CodeGen 기반 Type-Safe 리팩토링

## 개요

기존 문자열 기반(String-based) 데이터 로드 방식을 BGDatabase CodeGen 클래스를 사용한 Type-Safe 방식으로 변경합니다.

### BGDatabase 스키마 현황 (BGCodeGenerate.cs 분석)

| 테이블 | 필드 | DB 타입 | 현재 코드 타입 |
|:---|:---|:---:|:---:|
| DB_EnemyStat | F_MaxHP | `System.Double` | BigInteger (문자열 파싱) |
| DB_EnemyStat | F_AttackDamage | `System.Double` | BigInteger (문자열 파싱) |
| DB_EnemyStat | F_GoldReward | `System.Double` | BigInteger (문자열 파싱) |
| DB_EnemyStat | F_MoveSpeed | `System.Single` | float |
| DB_EnemyStat | F_Exp | `System.Double` | double |
| DB_SwordStat | F_AttackDamage | `System.Double` | BigInteger (문자열 파싱) |
| DB_SwordStat | F_Cooldown | `System.Single` | float |
| DB_SwordStat | F_MoveSpeed | `System.Single` | float |
| DB_SwordStat | F_CritDamage | `System.Single` | float |
| DB_SwordStat | F_CritChance | `System.Single` | float |
| DB_BossStat | F_MaxHP | `System.Double` | BigInteger (문자열 파싱) |
| DB_BossStat | F_AttackDamage | `System.Double` | BigInteger (문자열 파싱) |
| DB_BossStat | F_GoldReward | `System.Double` | BigInteger (문자열 파싱) |
| DB_PlayerStat | F_BaseMaxHealth | `System.Double` | BigInteger (문자열 파싱) |
| DB_PlayerStat | F_BaseMoveSpeed | `System.Single` | float |
| DB_PlayerStat | F_Level | `System.Int32` | int |
| DB_PlayerStat | F_CurrentExp | `System.Double` | double |
| DB_PlayerStat | F_MaxExp | `System.Double` | double |
| DB_PlayerProfile | F_Gold | `System.Double` | BigInteger (문자열 파싱) |
| DB_PlayerProfile | F_Ruby | `System.Double` | BigInteger (문자열 파싱) |
| DB_UpgradeData | F_BaseCost | `System.Double` | string → BigInteger.Parse |
| DB_UpgradeData | F_BonusPerLevel | `System.Double` | string → float.Parse |
| DB_UpgradeData | F_CostMultiplier | `System.Single` | float |

**결론**: BGDatabase 스키마는 이미 `double`/`float` 타입으로 정의되어 있음. 코드에서 불필요하게 문자열로 읽어서 BigInteger로 파싱하고 있었음.

---

## 리팩토링 전략

### 기존 방식 (String-based + BigInteger 파싱)
```csharp
// EnemyStatRepository.cs - 기존 코드
private const string MaxHPField = "MaxHP";

private EnemyStat CreateStatFromEntity(BGEntity entity)
{
    string maxHpStr = entity.Get<string>(MaxHPField);  // 문자열로 읽기
    BigInteger maxHP = string.IsNullOrEmpty(maxHpStr)
        ? BigInteger.Zero
        : BigInteger.Parse(maxHpStr);  // BigInteger로 파싱
    // ...
}
```

### 새로운 방식 (Type-Safe CodeGen 사용)
```csharp
// EnemyStatRepository.cs - 새로운 코드
private EnemyStat CreateStatFromEntity(DB_EnemyStat dbEntity)
{
    return new EnemyStat(
        dbEntity.F_name,
        dbEntity.F_MaxHP,          // 직접 double 타입으로 접근
        dbEntity.F_AttackDamage,   // 직접 double 타입으로 접근
        dbEntity.F_MoveSpeed,
        dbEntity.F_GoldReward,     // 직접 double 타입으로 접근
        dbEntity.F_Exp
    );
}
```

### 장점
1. **타입 안전성**: 컴파일 타임에 타입 오류 검출
2. **문자열 파싱 제거**: 런타임 오버헤드 감소
3. **BigInteger 불필요**: DB 스키마가 이미 double이므로 변환 필요 없음
4. **자동 완성 지원**: IDE에서 필드명 자동 완성

---

## 수정 파일 목록 (총 18개)

### Data 계층 (5개) - BigInteger → double
1. `Sword/Data/SwordStat.cs`
2. `Enemy/Data/EnemyStat.cs`
3. `Boss/Data/BossStat.cs`
4. `Player/Data/PlayerStat.cs`
5. `Currency/Data/Currency.cs`

### Repository 계층 (5개) - **핵심 변경: CodeGen 클래스 사용**
6. `Sword/Repository/SwordStatRepository.cs`
7. `Enemy/Repository/EnemyStatRepository.cs`
8. `Boss/Repository/BossStatRepository.cs`
9. `Player/Repository/PlayerStatRepository.cs`
10. `Currency/Repository/CurrencyRepository.cs`

### Interface (2개)
11. `Interface/IDamageable.cs`
12. `Currency/Data/ICurrencyRepository.cs`

### Manager 계층 (2개)
13. `Currency/Manager/CurrencyManager.cs`
14. `Upgrade/Manager/UpgradeManager.cs`

### Util (1개)
15. `Currency/Util/CurrencyFormatter.cs`

### 기타 (3개)
16. `Upgrade/Data/UpgradeData.cs`
17. `Enemy/EnemyAI.cs`
18. `Enemy/UI/EnemyHPBar.cs`

---

## 상세 코드 변경

### 1. SwordStat.cs
**경로**: `Assets/02.Scripts/Sword/Data/SwordStat.cs`

```csharp
// ========== 변경 전 ==========
using System.Numerics;

public record SwordStat(
    string Id,
    BigInteger AttackDamage,
    float Cooldown,
    float MoveSpeed,
    float CritDamageMultiplier,
    float CritChance
)
{
    public BigInteger CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return new BigInteger((double)AttackDamage * CritDamageMultiplier);
    }
};

// ========== 변경 후 ==========
public record SwordStat(
    string Id,
    double AttackDamage,
    float Cooldown,
    float MoveSpeed,
    float CritDamageMultiplier,
    float CritChance
)
{
    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
};
```

---

### 2. EnemyStat.cs
**경로**: `Assets/02.Scripts/Enemy/Data/EnemyStat.cs`

```csharp
// ========== 변경 전 ==========
using System.Numerics;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

public record EnemyStat(
    string Id,
    BigInteger MaxHP,
    BigInteger AttackDamage,
    float MoveSpeed,
    BigInteger GoldReward,
    double Exp
);

// ========== 변경 후 ==========
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

public record EnemyStat(
    string Id,
    double MaxHP,
    double AttackDamage,
    float MoveSpeed,
    double GoldReward,
    double Exp
);
```

---

### 3. BossStat.cs
**경로**: `Assets/02.Scripts/Boss/Data/BossStat.cs`

```csharp
// ========== 변경 전 ==========
using System.Numerics;

public record BossStat(
    string Id,
    BigInteger MaxHP,
    BigInteger AttackDamage,
    float MoveSpeed,
    BigInteger GoldReward,
    double Exp
);

// ========== 변경 후 ==========
public record BossStat(
    string Id,
    double MaxHP,
    double AttackDamage,
    float MoveSpeed,
    double GoldReward,
    double Exp
);
```

---

### 4. PlayerStat.cs
**경로**: `Assets/02.Scripts/Player/Data/PlayerStat.cs`

```csharp
// ========== 변경 전 ==========
using System.Numerics;

public record PlayerStat(
    string Id,
    BigInteger BaseMaxHealth,
    float BaseMoveSpeed,
    int Level,
    double CurrentExp,
    double MaxExp
);

// ========== 변경 후 ==========
public record PlayerStat(
    string Id,
    double BaseMaxHealth,
    float BaseMoveSpeed,
    int Level,
    double CurrentExp,
    double MaxExp
);
```

---

### 5. Currency.cs
**경로**: `Assets/02.Scripts/Currency/Data/Currency.cs`

```csharp
// ========== 변경 전 ==========
using System;
using System.Numerics;

public class Currency
{
    public void Add(CurrencyType type, BigInteger amount) { ... }
    public bool TrySpend(CurrencyType type, BigInteger amount) { ... }
    public BigInteger Get(CurrencyType type) { ... }
}

// ========== 변경 후 ==========
using System;

public class Currency
{
    private double _gold;
    private double _ruby;

    public double Gold => _gold;
    public double Ruby => _ruby;

    public event Action<CurrencyType, double> OnChanged;

    public Currency(double gold, double ruby)
    {
        _gold = gold;
        _ruby = ruby;
    }

    public void Add(CurrencyType type, double amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case CurrencyType.Gold:
                _gold += amount;
                OnChanged?.Invoke(CurrencyType.Gold, _gold);
                break;
            case CurrencyType.Ruby:
                _ruby += amount;
                OnChanged?.Invoke(CurrencyType.Ruby, _ruby);
                break;
        }
    }

    public bool TrySpend(CurrencyType type, double amount)
    {
        if (amount <= 0) return false;

        switch (type)
        {
            case CurrencyType.Gold:
                if (_gold < amount) return false;
                _gold -= amount;
                OnChanged?.Invoke(CurrencyType.Gold, _gold);
                return true;

            case CurrencyType.Ruby:
                if (_ruby < amount) return false;
                _ruby -= amount;
                OnChanged?.Invoke(CurrencyType.Ruby, _ruby);
                return true;

            default:
                return false;
        }
    }

    public double Get(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => _gold,
            CurrencyType.Ruby => _ruby,
            _ => 0
        };
    }
}
```

---

### 6. SwordStatRepository.cs (핵심 변경)
**경로**: `Assets/02.Scripts/Sword/Repository/SwordStatRepository.cs`

```csharp
// ========== 변경 전 ==========
using System.Collections.Generic;
using System.Numerics;
using BansheeGz.BGDatabase;
using UnityEngine;

public class SwordStatRepository : ISwordStatRepository
{
    private const string TableName = "SwordStat";
    private const string AttackDamageField = "AttackDamage";
    private const string CooldownField = "Cooldown";
    // ... 필드 상수들

    private readonly BGMetaEntity _meta;
    private readonly Dictionary<string, SwordStat> _cache;

    public SwordStatRepository()
    {
        _cache = new Dictionary<string, SwordStat>();
        _meta = BGRepo.I[TableName];
        // ...
    }

    private SwordStat CreateStatFromEntity(BGEntity entity)
    {
        string attackDamageStr = entity.Get<string>(AttackDamageField);
        BigInteger attackDamage = string.IsNullOrEmpty(attackDamageStr)
            ? BigInteger.Zero
            : BigInteger.Parse(attackDamageStr);

        return new SwordStat(
            entity.Name,
            attackDamage,
            entity.Get<float>(CooldownField),
            // ...
        );
    }
}

// ========== 변경 후 (CodeGen 사용) ==========
using System.Collections.Generic;
using UnityEngine;

public class SwordStatRepository : ISwordStatRepository
{
    private readonly Dictionary<string, SwordStat> _cache;

    public SwordStatRepository()
    {
        _cache = new Dictionary<string, SwordStat>();

        if (DB_SwordStat.CountEntities == 0)
        {
            Debug.LogError("[SwordStatRepository] SwordStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<SwordStat> LoadAll()
    {
        var result = new List<SwordStat>();
        _cache.Clear();

        int count = DB_SwordStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_SwordStat dbEntity = DB_SwordStat.GetEntity(i);
            SwordStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public SwordStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out SwordStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[SwordStatRepository] 데이터를 찾을 수 없습니다: {id}");
        return null;
    }

    private SwordStat CreateStatFromEntity(DB_SwordStat dbEntity)
    {
        float critDamageMultiplier = dbEntity.F_CritDamage;
        if (critDamageMultiplier <= 0f) critDamageMultiplier = 2.0f;

        return new SwordStat(
            dbEntity.F_name,
            dbEntity.F_AttackDamage,      // 직접 double 접근
            dbEntity.F_Cooldown,
            dbEntity.F_MoveSpeed,
            critDamageMultiplier,
            dbEntity.F_CritChance
        );
    }
}
```

---

### 7. EnemyStatRepository.cs (핵심 변경)
**경로**: `Assets/02.Scripts/Enemy/Repository/EnemyStatRepository.cs`

```csharp
// ========== 변경 전 ==========
private EnemyStat CreateStatFromEntity(BGEntity entity)
{
    string maxHpStr = entity.Get<string>(MaxHPField);
    string atkStr = entity.Get<string>(AttackDamageField);
    string goldStr = entity.Get<string>(GoldRewardField);

    BigInteger maxHP = string.IsNullOrEmpty(maxHpStr) ? BigInteger.Zero : BigInteger.Parse(maxHpStr);
    BigInteger attackDamage = string.IsNullOrEmpty(atkStr) ? BigInteger.Zero : BigInteger.Parse(atkStr);
    BigInteger goldReward = string.IsNullOrEmpty(goldStr) ? BigInteger.Zero : BigInteger.Parse(goldStr);

    return new EnemyStat(entity.Name, maxHP, attackDamage, ...);
}

// ========== 변경 후 (CodeGen 사용) ==========
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatRepository : IEnemyStatRepository
{
    private readonly Dictionary<string, EnemyStat> _cache;

    public EnemyStatRepository()
    {
        _cache = new Dictionary<string, EnemyStat>();

        if (DB_EnemyStat.CountEntities == 0)
        {
            Debug.LogError("[EnemyStatRepository] EnemyStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<EnemyStat> LoadAll()
    {
        var result = new List<EnemyStat>();
        _cache.Clear();

        int count = DB_EnemyStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_EnemyStat dbEntity = DB_EnemyStat.GetEntity(i);
            EnemyStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public EnemyStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out EnemyStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[EnemyStatRepository] 스탯을 찾을 수 없습니다: {id}");
        return null;
    }

    private EnemyStat CreateStatFromEntity(DB_EnemyStat dbEntity)
    {
        return new EnemyStat(
            dbEntity.F_name,
            dbEntity.F_MaxHP,           // 직접 double 접근
            dbEntity.F_AttackDamage,    // 직접 double 접근
            dbEntity.F_MoveSpeed,
            dbEntity.F_GoldReward,      // 직접 double 접근
            dbEntity.F_Exp
        );
    }
}
```

---

### 8. BossStatRepository.cs (핵심 변경)
**경로**: `Assets/02.Scripts/Boss/Repository/BossStatRepository.cs`

```csharp
// ========== 변경 후 (CodeGen 사용) ==========
using System.Collections.Generic;
using UnityEngine;

public class BossStatRepository : IBossStatRepository
{
    private readonly Dictionary<string, BossStat> _cache;

    public BossStatRepository()
    {
        _cache = new Dictionary<string, BossStat>();

        if (DB_BossStat.CountEntities == 0)
        {
            Debug.LogWarning("[BossStatRepository] BossStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<BossStat> LoadAll()
    {
        var result = new List<BossStat>();
        _cache.Clear();

        int count = DB_BossStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_BossStat dbEntity = DB_BossStat.GetEntity(i);
            BossStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public BossStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out BossStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[BossStatRepository] 보스 스탯을 찾을 수 없습니다: {id}");
        return null;
    }

    private BossStat CreateStatFromEntity(DB_BossStat dbEntity)
    {
        return new BossStat(
            dbEntity.F_name,
            dbEntity.F_MaxHP,           // 직접 double 접근
            dbEntity.F_AttackDamage,    // 직접 double 접근
            dbEntity.F_MoveSpeed,
            dbEntity.F_GoldReward,      // 직접 double 접근
            dbEntity.F_Exp
        );
    }
}
```

---

### 9. PlayerStatRepository.cs (핵심 변경)
**경로**: `Assets/02.Scripts/Player/Repository/PlayerStatRepository.cs`

```csharp
// ========== 변경 후 (CodeGen 사용) ==========
using UnityEngine;

public class PlayerStatRepository : IPlayerStatRepository
{
    private readonly string _playerName;
    private DB_PlayerStat _playerEntity;
    private PlayerStat _cachedStat;

    public PlayerStatRepository(string playerName)
    {
        _playerName = playerName;
        InitializePlayerEntity();
    }

    private void InitializePlayerEntity()
    {
        _playerEntity = DB_PlayerStat.GetEntity(_playerName);

        if (_playerEntity != null)
        {
            Debug.Log($"[PlayerStatRepository] PlayerEntity 찾음: {_playerName}");
        }
        else
        {
            Debug.Log($"[PlayerStatRepository] PlayerEntity 없음, 새로 생성: {_playerName}");
            _playerEntity = CreateNewPlayerEntity();
        }
    }

    private DB_PlayerStat CreateNewPlayerEntity()
    {
        DB_PlayerStat entity = DB_PlayerStat.NewEntity(e =>
        {
            e.F_name = _playerName;
            e.F_BaseMaxHealth = 100;
            e.F_BaseMoveSpeed = 5f;
            e.F_Level = 1;
            e.F_CurrentExp = 0;
            e.F_MaxExp = 10;
        });
        return entity;
    }

    public PlayerStat Load()
    {
        if (_playerEntity == null)
        {
            _cachedStat = new PlayerStat(_playerName, 100, 5f, 1, 0, 10);
            return _cachedStat;
        }

        double baseMaxHealth = _playerEntity.F_BaseMaxHealth;
        if (baseMaxHealth <= 0) baseMaxHealth = 100;

        float baseMoveSpeed = _playerEntity.F_BaseMoveSpeed;
        if (baseMoveSpeed <= 0f) baseMoveSpeed = 5f;

        int level = _playerEntity.F_Level;
        if (level < 1) level = 1;

        double maxExp = _playerEntity.F_MaxExp;
        if (maxExp <= 0) maxExp = 10.0 * System.Math.Pow(2, level - 1);

        _cachedStat = new PlayerStat(
            _playerEntity.F_name,
            baseMaxHealth,
            baseMoveSpeed,
            level,
            _playerEntity.F_CurrentExp,
            maxExp
        );

        return _cachedStat;
    }

    public void Save(PlayerStat stat)
    {
        if (_playerEntity == null)
        {
            Debug.LogWarning("[PlayerStatRepository] PlayerEntity가 없어 저장할 수 없습니다.");
            return;
        }

        _playerEntity.F_BaseMaxHealth = stat.BaseMaxHealth;
        _playerEntity.F_BaseMoveSpeed = stat.BaseMoveSpeed;
        _playerEntity.F_Level = stat.Level;
        _playerEntity.F_CurrentExp = stat.CurrentExp;
        _playerEntity.F_MaxExp = stat.MaxExp;

        _cachedStat = stat;

        BansheeGz.BGDatabase.BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
```

---

### 10. CurrencyRepository.cs (핵심 변경)
**경로**: `Assets/02.Scripts/Currency/Repository/CurrencyRepository.cs`

```csharp
// ========== 변경 후 (CodeGen 사용) ==========
using System.Threading.Tasks;
using BansheeGz.BGDatabase;
using UnityEngine;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly string _playerName;
    private DB_PlayerProfile _playerEntity;

    public CurrencyRepository(string playerName)
    {
        _playerName = playerName;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _playerEntity = DB_PlayerProfile.GetEntity(_playerName);

        if (_playerEntity == null)
        {
            _playerEntity = CreateNewPlayerEntity();
        }
    }

    private DB_PlayerProfile CreateNewPlayerEntity()
    {
        DB_PlayerProfile entity = DB_PlayerProfile.NewEntity(e =>
        {
            e.F_name = _playerName;
            e.F_Gold = 0;
            e.F_Ruby = 0;
        });
        return entity;
    }

    public Task<Currency> LoadAsync()
    {
        if (_playerEntity == null)
        {
            return Task.FromResult(new Currency(0, 0));
        }

        double gold = _playerEntity.F_Gold;
        double ruby = _playerEntity.F_Ruby;

        return Task.FromResult(new Currency(gold, ruby));
    }

    public Task SaveAsync(Currency currency)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Gold = currency.Gold;
        _playerEntity.F_Ruby = currency.Ruby;

        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        return Task.CompletedTask;
    }

    public Task SaveGoldAsync(double gold)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Gold = gold;
        ForceSaveToDisk();
        return Task.CompletedTask;
    }

    public Task SaveRubyAsync(double ruby)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Ruby = ruby;
        ForceSaveToDisk();
        return Task.CompletedTask;
    }

    public void ForceSaveToDisk()
    {
        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
```

---

### 11. IDamageable.cs
**경로**: `Assets/02.Scripts/Interface/IDamageable.cs`

```csharp
// ========== 변경 후 ==========
public interface IDamageable
{
    void TakeDamage(double damage, bool isCrit);
}
```

---

### 12. ICurrencyRepository.cs
**경로**: `Assets/02.Scripts/Currency/Data/ICurrencyRepository.cs`

```csharp
// ========== 변경 후 ==========
using System.Threading.Tasks;

public interface ICurrencyRepository
{
    Task<Currency> LoadAsync();
    Task SaveAsync(Currency currency);
    Task SaveGoldAsync(double gold);
    Task SaveRubyAsync(double ruby);
    void ForceSaveToDisk();
}
```

---

### 13. CurrencyManager.cs
**경로**: `Assets/02.Scripts/Currency/Manager/CurrencyManager.cs`

주요 변경:
- `using System.Numerics;` 제거
- 모든 `BigInteger` → `double`
- `BigInteger.Zero` → `0`
- `event Action<CurrencyType, BigInteger>` → `event Action<CurrencyType, double>`

---

### 14. UpgradeManager.cs
**경로**: `Assets/02.Scripts/Upgrade/Manager/UpgradeManager.cs`

주요 변경:
- `using System.Numerics;` 제거
- `GetBigIntBonus()` → `GetDoubleBonus()` (이름 변경)
- 반환 타입 `BigInteger` → `double`
- `BigInteger.Zero` → `0`

---

### 15. CurrencyFormatter.cs
**경로**: `Assets/02.Scripts/Currency/Util/CurrencyFormatter.cs`

주요 변경:
- `using System.Numerics;` 제거, `using System;` 추가
- 모든 메서드 파라미터 `BigInteger` → `double`
- 정수 출력 시 `(long)` 캐스팅 사용

---

### 16. UpgradeData.cs
**경로**: `Assets/02.Scripts/Upgrade/Data/UpgradeData.cs`

```csharp
// ========== 변경 후 ==========
using System;

public record UpgradeData(
    string Id,
    string DisplayName,
    double BaseCost,        // string → double (DB 스키마와 일치)
    float CostMultiplier,
    double BonusPerLevel    // string → double (DB 스키마와 일치)
)
{
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

### 17. EnemyAI.cs
**경로**: `Assets/02.Scripts/Enemy/EnemyAI.cs`

주요 변경:
- `using System.Numerics;` 제거
- `BigInteger _currentHealth` → `double _currentHealth`
- `TakeDamage(BigInteger)` → `TakeDamage(double)`
- `BigInteger.Zero` → `0`
- `MultiplyBigInteger()` 메서드 제거 (단순 곱셈으로 대체)

---

### 18. EnemyHPBar.cs
**경로**: `Assets/02.Scripts/Enemy/UI/EnemyHPBar.cs`

```csharp
// ========== 변경 후 ==========
public void Initialize(double maxHP) { ... }
public void UpdateHP(double currentHP) { ... }
```

---

## 추가 확인 필요 파일

아래 파일들도 BigInteger를 사용하므로 확인 후 수정 필요:

1. `OfflineReward/Manager/OfflineRewardManager.cs`
2. `OfflineReward/UI/OfflineRewardUI.cs`
3. `OfflineReward/Data/OfflineRewardData.cs`
4. `Currency/UI/CurrencyUI.cs`
5. `Upgrade/UI/UpgradeSlotUI.cs`
6. `Player/PlayerHealth.cs`
7. `Player/PlayerHealthUI.cs`
8. `UI/RedDot/Conditions/UpgradeRedDotCondition.cs`
9. `Enemy/Manager/EnemySpawner.cs`

---

## 작업 순서

1. **Data 계층** 먼저 수정 (의존성 없음)
2. **Interface** 수정
3. **Repository 계층** 수정 (CodeGen 클래스 사용)
4. **Manager 계층** 수정
5. **Util** 수정
6. **UI/기타** 수정
7. Unity 에디터 빌드 테스트

---

## 검증 체크리스트

- [ ] Unity 에디터 컴파일 에러 없음
- [ ] 플레이 모드 진입 가능
- [ ] 데미지 표시 정상
- [ ] 재화 획득/소비 정상
- [ ] 강화 비용 계산 정상
- [ ] 적 사망 시 보상 정상
- [ ] HP 바 표시 정상
- [ ] BGDatabase 데이터 정상 로드 확인
