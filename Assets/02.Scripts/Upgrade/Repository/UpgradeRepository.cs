using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public class UpgradeRepository : IUpgradeRepository
{
    private readonly string _playerName;
    private DB_PlayerProfile _playerEntity;
    private readonly Dictionary<string, UpgradeData> _upgradeDataCache;

    public UpgradeRepository(string playerName)
    {
        _playerName = playerName;
        _upgradeDataCache = new Dictionary<string, UpgradeData>();

        if (DB_UpgradeData.CountEntities == 0)
        {
            Debug.LogError("[UpgradeRepository] UpgradeData 테이블이 비어있습니다.");
        }

        InitializePlayerEntity();
        LoadAllUpgradeData();
    }

    private void InitializePlayerEntity()
    {
        _playerEntity = DB_PlayerProfile.GetEntity(_playerName);

        if (_playerEntity != null)
        {
            Debug.Log($"[UpgradeRepository] PlayerEntity 찾음: {_playerName}");
        }
        else
        {
            Debug.LogWarning($"[UpgradeRepository] PlayerEntity를 찾을 수 없음: {_playerName}");
        }
    }

    public List<UpgradeData> LoadAllUpgradeData()
    {
        var result = new List<UpgradeData>();
        _upgradeDataCache.Clear();

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

        string json = _playerEntity.F_UpgradeLevels ?? "";
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
        _playerEntity.F_UpgradeLevels = json;
        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log($"[UpgradeRepository] 강화 레벨 저장 완료: {json}");
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
}
