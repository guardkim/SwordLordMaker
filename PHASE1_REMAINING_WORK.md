# Phase 1 남은 작업: BGCodeGen Type-Safe 리팩토링

## 현재 상태 요약

### 완료된 Repository (CodeGen 사용 중)
| Repository | CodeGen 클래스 | 상태 |
|:---|:---|:---:|
| `SwordStatRepository.cs` | `DB_SwordStat` | ✅ 완료 |
| `EnemyStatRepository.cs` | `DB_EnemyStat` | ✅ 완료 |
| `BossStatRepository.cs` | `DB_BossStat` | ✅ 완료 |
| `PlayerStatRepository.cs` | `DB_PlayerStat` | ✅ 완료 |
| `CurrencyRepository.cs` | `DB_PlayerProfile` | ✅ 완료 |

### 미완료 Repository (문자열 기반 접근 사용 중)
| Repository | 변환할 CodeGen 클래스 | 상태 |
|:---|:---|:---:|
| `UpgradeRepository.cs` | `DB_UpgradeData`, `DB_PlayerProfile` | ❌ 미완료 |
| `OfflineRewardRepository.cs` | `DB_PlayerProfile` | ❌ 미완료 |
| `StageRepository.cs` | `DB_StageStat` | ❌ 미완료 |

---

## 작업 1: UpgradeRepository.cs 리팩토링

### 현재 코드 (문자열 기반)
```csharp
// 문제점: 문자열 상수로 필드 접근
private const string UpgradeDataTableName = "UpgradeData";
private const string DisplayNameField = "DisplayName";
private const string BaseCostField = "BaseCost";

private readonly BGMetaEntity _upgradeDataMeta;
_upgradeDataMeta = BGRepo.I[UpgradeDataTableName];

BGEntity entity = _upgradeDataMeta.GetEntity(i);
entity.Get<string>(DisplayNameField);
entity.Get<double>(BaseCostField);
```

### 변경 후 코드 (CodeGen 사용)
```csharp
// DB_UpgradeData CodeGen 클래스 사용
public List<UpgradeData> LoadAllUpgradeData()
{
    var result = new List<UpgradeData>();
    _upgradeDataCache.Clear();

    if (DB_UpgradeData.CountEntities == 0) return result;

    int count = DB_UpgradeData.CountEntities;
    for (int i = 0; i < count; i++)
    {
        DB_UpgradeData dbEntity = DB_UpgradeData.GetEntity(i);
        UpgradeData data = CreateUpgradeDataFromEntity(dbEntity);
        _upgradeDataCache[data.Id] = data;
        result.Add(data);
    }

    return result;
}

private UpgradeData CreateUpgradeDataFromEntity(DB_UpgradeData dbEntity)
{
    return new UpgradeData(
        dbEntity.F_name,
        dbEntity.F_DisplayName ?? dbEntity.F_name,
        dbEntity.F_BaseCost,
        dbEntity.F_CostMultiplier,
        dbEntity.F_BonusPerLevel
    );
}
```

### PlayerProfile 접근도 CodeGen으로 변경
```csharp
// 현재: _playerEntity = FindEntityByName(_playerName);
// 변경: DB_PlayerProfile.GetEntity(_playerName) 사용

private DB_PlayerProfile _playerEntity;

private void InitializePlayerEntity()
{
    _playerEntity = DB_PlayerProfile.GetEntity(_playerName);

    if (_playerEntity != null)
    {
        Debug.Log($"[UpgradeRepository] PlayerEntity 찾음: {_playerName}");
    }
}

// UpgradeLevels 필드 접근
// 현재: _playerEntity.Get<string>(UpgradeLevelsField)
// 문제: F_UpgradeLevels가 double 타입임 → string으로 저장하고 있었음

// 해결방안: UpgradeLevels를 JSON 문자열로 저장하려면
// DB 스키마에 String 타입 필드가 필요하거나,
// 별도 저장 방식 고려 필요
```

### 주의사항
- `DB_PlayerProfile.F_UpgradeLevels`가 `double` 타입으로 정의되어 있음
- 현재 코드는 JSON 문자열로 저장/로드하고 있음
- **해결방안 필요**:
  - 옵션 A: DB 스키마에 String 타입 필드 추가
  - 옵션 B: 기존 문자열 기반 접근 유지 (해당 필드만)
  - 옵션 C: 저장 방식 변경 (PlayerPrefs 등)

---

## 작업 2: OfflineRewardRepository.cs 리팩토링

### 현재 코드 (문자열 기반)
```csharp
private const string TableName = "PlayerProfile";
private const string LastLoginTimeField = "LastLoginTime";

private BGMetaEntity _meta;
private BGEntity _playerEntity;

_meta = BGRepo.I[TableName];
_playerEntity = FindEntityByName(_playerName);
_playerEntity.Get<string>(LastLoginTimeField);
```

### 변경 후 코드 (CodeGen 사용)
```csharp
private DB_PlayerProfile _playerEntity;

private void InitializeDatabase()
{
    _playerEntity = DB_PlayerProfile.GetEntity(_playerName);

    if (_playerEntity == null)
    {
        Debug.LogWarning($"[OfflineRewardRepository] 플레이어 엔티티를 찾을 수 없습니다: {_playerName}");
    }
}

public Task<long> LoadLastLoginTimeAsync()
{
    if (_playerEntity == null)
    {
        return Task.FromResult(0L);
    }

    // F_LastLoginTime이 String 타입이므로 직접 접근
    string timeStr = _playerEntity.F_LastLoginTime ?? "0";
    long lastLoginTime = long.TryParse(timeStr, out var t) ? t : 0L;

    return Task.FromResult(lastLoginTime);
}

public Task SaveLastLoginTimeAsync(long unixTimestamp)
{
    if (_playerEntity == null)
    {
        return Task.CompletedTask;
    }

    _playerEntity.F_LastLoginTime = unixTimestamp.ToString();
    ForceSaveToDisk();
    return Task.CompletedTask;
}
```

### 제거할 코드
- `_meta` 필드 제거
- `FindEntityByName()` 메서드 제거 (CodeGen의 `GetEntity(string)` 사용)
- 문자열 상수들 제거

---

## 작업 3: StageRepository.cs 리팩토링

### 현재 코드 (문자열 기반)
```csharp
private const string TableName = "StageStat";
private const string StageIdField = "StageId";
private const string EnemyStatIdField = "EnemyStatId";
// ... 필드 상수들

private readonly BGMetaEntity _meta;
_meta = BGRepo.I[TableName];

BGEntity entity = _meta.GetEntity(i);
entity.Get<int>(StageIdField);
entity.Get<string>(EnemyStatIdField);
```

### 변경 후 코드 (CodeGen 사용)
```csharp
using System.Collections.Generic;
using UnityEngine;

public class StageRepository : IStageRepository
{
    private readonly Dictionary<int, StageStat> _cache;
    private int _maxStageId;

    public StageRepository()
    {
        _cache = new Dictionary<int, StageStat>();

        if (DB_StageStat.CountEntities == 0)
        {
            Debug.LogError("[StageRepository] StageStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<StageStat> LoadAll()
    {
        var result = new List<StageStat>();
        _cache.Clear();
        _maxStageId = 0;

        int count = DB_StageStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_StageStat dbEntity = DB_StageStat.GetEntity(i);
            StageStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.StageId] = stat;
            result.Add(stat);

            if (stat.StageId > _maxStageId)
            {
                _maxStageId = stat.StageId;
            }
        }

        return result;
    }

    public StageStat GetByStageId(int stageId)
    {
        if (_cache.TryGetValue(stageId, out StageStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[StageRepository] 스테이지를 찾을 수 없습니다: {stageId}");
        return null;
    }

    public int GetMaxStageId()
    {
        return _maxStageId;
    }

    private StageStat CreateStatFromEntity(DB_StageStat dbEntity)
    {
        float hpMult = dbEntity.F_HpMultiplier;
        float atkMult = dbEntity.F_AttackMultiplier;
        float spdMult = dbEntity.F_SpeedMultiplier;
        float goldMult = dbEntity.F_GoldMultiplier;
        float expMult = dbEntity.F_ExpMultiplier;

        return new StageStat(
            dbEntity.F_StageId,
            dbEntity.F_name,
            dbEntity.F_EnemyStatId,
            dbEntity.F_BossStatId ?? "",
            hpMult <= 0 ? 1f : hpMult,
            atkMult <= 0 ? 1f : atkMult,
            spdMult <= 0 ? 1f : spdMult,
            goldMult <= 0 ? 1f : goldMult,
            expMult <= 0 ? 1f : expMult
        );
    }
}
```

### 제거할 코드
- 모든 문자열 상수 (`TableName`, `StageIdField` 등)
- `_meta` 필드
- `BGRepo.I[TableName]` 접근

---

## 작업 순서

### Step 1: StageRepository.cs (가장 단순)
- 위험도: 낮음
- 의존성: 없음
- 예상 작업량: 작음

### Step 2: OfflineRewardRepository.cs
- 위험도: 낮음
- 의존성: `DB_PlayerProfile`
- 예상 작업량: 작음

### Step 3: UpgradeRepository.cs (가장 복잡)
- 위험도: 중간
- 의존성: `DB_UpgradeData`, `DB_PlayerProfile`
- **주의**: `F_UpgradeLevels` 타입 불일치 문제 해결 필요
- 예상 작업량: 중간

---

## 검증 체크리스트

### 컴파일 확인
- [ ] Unity 에디터 컴파일 에러 없음

### 기능 테스트
- [ ] 스테이지 진행 정상 (StageRepository)
- [ ] 오프라인 보상 정상 (OfflineRewardRepository)
- [ ] 강화 시스템 정상 (UpgradeRepository)
  - [ ] 강화 데이터 로드
  - [ ] 강화 레벨 저장/로드
  - [ ] 강화 비용 계산

---

## 결정 필요 사항

### UpgradeRepository의 UpgradeLevels 저장 방식

**현재 상황:**
- `DB_PlayerProfile.F_UpgradeLevels`가 `double` 타입
- 코드에서는 JSON 문자열로 저장/로드 중
- 타입 불일치 발생

**옵션:**
1. **옵션 A**: BGDatabase 스키마에 `UpgradeLevelsJson` (String 타입) 필드 추가
2. **옵션 B**: 기존 문자열 기반 접근 유지 (`entity.Get<string>`)
3. **옵션 C**: PlayerPrefs로 저장 방식 변경

**권장**: 옵션 B (기존 방식 유지) - 스키마 변경 없이 안전하게 진행
