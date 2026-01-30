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

    // PlayerProfile 필드
    private const string UpgradeLevelsField = "UpgradeLevels";

    private readonly string _playerName;
    private readonly BGMetaEntity _upgradeDataMeta;
    private readonly BGMetaEntity _playerProfileMeta;
    private BGEntity _playerEntity;

    private readonly Dictionary<string, UpgradeData> _upgradeDataCache;

    public UpgradeRepository(string playerName)
    {
        _playerName = playerName;
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

        _playerEntity = FindEntityByName(_playerName);

        if (_playerEntity != null)
        {
            Debug.Log($"[UpgradeRepository] PlayerEntity 찾음: {_playerName}");
        }
        else
        {
            Debug.LogWarning($"[UpgradeRepository] PlayerEntity를 찾을 수 없음: {_playerName}");
        }
    }

    private BGEntity FindEntityByName(string playerName)
    {
        if (_playerProfileMeta == null || _playerProfileMeta.CountEntities == 0)
        {
            return null;
        }

        int count = _playerProfileMeta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _playerProfileMeta.GetEntity(i);
            if (entity.Name == playerName)
            {
                return entity;
            }
        }

        return null;
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
            Debug.LogWarning($"[UpgradeRepository] PlayerEntity가 없어서 빈 레벨 반환 (playerName: {_playerName})");
            return new PlayerUpgradeLevels();
        }

        string json = _playerEntity.Get<string>(UpgradeLevelsField) ?? "";
        Debug.Log($"[UpgradeRepository] 강화 레벨 로드: {json}");
        return PlayerUpgradeLevels.FromJson(json);
    }

    public void SavePlayerLevels(PlayerUpgradeLevels levels)
    {
        if (_playerEntity == null)
        {
            Debug.LogError($"[UpgradeRepository] PlayerEntity가 없습니다. 저장 실패 (playerName: {_playerName})");
            return;
        }

        string json = levels.ToJson();
        _playerEntity.Set(UpgradeLevelsField, json);
        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log($"[UpgradeRepository] 강화 레벨 저장 완료: {json}");
    }

    private UpgradeData CreateUpgradeDataFromEntity(BGEntity entity)
    {
        return new UpgradeData(
            entity.Name,
            entity.Get<string>(DisplayNameField) ?? entity.Name,
            entity.Get<double>(BaseCostField),
            entity.Get<float>(CostMultiplierField),
            entity.Get<double>(BonusPerLevelField)
        );
    }
}
