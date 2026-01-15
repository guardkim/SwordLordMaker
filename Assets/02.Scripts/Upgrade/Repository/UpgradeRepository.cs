using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public class UpgradeRepository : IUpgradeRepository
{
    private const string UpgradeDataTableName = "UpgradeData";
    private const string PlayerProfileTableName = "PlayerProfile";

    // UpgradeData 필드
    private const string DisplayNameField = "DisplayName";
    private const string BaseCostField = "BaseCost";
    private const string CostMultiplierField = "CostMultiplier";
    private const string BonusPerLevelField = "BonusPerLevel";
    private const string MaxLevelField = "MaxLevel";

    // PlayerProfile 필드
    private const string UpgradeLevelsField = "UpgradeLevels";

    private readonly BGMetaEntity _upgradeDataMeta;
    private readonly BGMetaEntity _playerProfileMeta;
    private BGEntity _playerEntity;

    private readonly Dictionary<string, UpgradeData> _upgradeDataCache;

    public UpgradeRepository()
    {
        _upgradeDataCache = new Dictionary<string, UpgradeData>();

        _upgradeDataMeta = BGRepo.I[UpgradeDataTableName];
        _playerProfileMeta = BGRepo.I[PlayerProfileTableName];

        if (_upgradeDataMeta == null)
        {
            Debug.LogError($"[UpgradeRepository] 테이블을 찾을 수 없습니다: {UpgradeDataTableName}");
        }

        if (_playerProfileMeta == null)
        {
            Debug.LogError($"[UpgradeRepository] 테이블을 찾을 수 없습니다: {PlayerProfileTableName}");
        }

        InitializePlayerEntity();
        LoadAllUpgradeData();
    }

    private void InitializePlayerEntity()
    {
        if (_playerProfileMeta == null) return;

        if (_playerProfileMeta.CountEntities > 0)
        {
            _playerEntity = _playerProfileMeta.GetEntity(0);
        }
    }

    public List<UpgradeData> LoadAllUpgradeData()
    {
        var result = new List<UpgradeData>();
        _upgradeDataCache.Clear();

        if (_upgradeDataMeta == null) return result;

        int count = _upgradeDataMeta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _upgradeDataMeta.GetEntity(i);
            UpgradeData data = CreateUpgradeDataFromEntity(entity);
            _upgradeDataCache[data.Id] = data;
            result.Add(data);
        }

        return result;
    }

    public UpgradeData GetUpgradeData(string id)
    {
        if (_upgradeDataCache.TryGetValue(id, out UpgradeData data))
        {
            return data;
        }

        Debug.LogWarning($"[UpgradeRepository] 강화 데이터를 찾을 수 없습니다: {id}");
        return null;
    }

    public PlayerUpgradeLevels LoadPlayerLevels()
    {
        if (_playerEntity == null)
        {
            return new PlayerUpgradeLevels();
        }

        string json = _playerEntity.Get<string>(UpgradeLevelsField) ?? "";
        return PlayerUpgradeLevels.FromJson(json);
    }

    public void SavePlayerLevels(PlayerUpgradeLevels levels)
    {
        if (_playerEntity == null)
        {
            Debug.LogError("[UpgradeRepository] PlayerEntity가 없습니다.");
            return;
        }

        string json = levels.ToJson();
        _playerEntity.Set(UpgradeLevelsField, json);
        BGRepo.I.Save();
    }

    private UpgradeData CreateUpgradeDataFromEntity(BGEntity entity)
    {
        return new UpgradeData(
            entity.Name,
            entity.Get<string>(DisplayNameField) ?? entity.Name,
            entity.Get<string>(BaseCostField) ?? "100",
            entity.Get<float>(CostMultiplierField),
            entity.Get<string>(BonusPerLevelField),
            entity.Get<int>(MaxLevelField)
        );
    }
}
