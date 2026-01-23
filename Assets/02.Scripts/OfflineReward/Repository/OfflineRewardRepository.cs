using System.Threading.Tasks;
using BansheeGz.BGDatabase;
using UnityEngine;

public class OfflineRewardRepository : IOfflineRewardRepository
{
    private const string TableName = "PlayerProfile";
    private const string LastLoginTimeField = "LastLoginTime";

    private readonly string _playerName;
    private BGMetaEntity _meta;
    private BGEntity _playerEntity;

    public OfflineRewardRepository(string playerName)
    {
        _playerName = playerName;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _meta = BGRepo.I[TableName];
        if (_meta == null)
        {
            Debug.LogError($"[OfflineRewardRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        _playerEntity = FindEntityByName(_playerName);
        if (_playerEntity == null)
        {
            Debug.LogWarning($"[OfflineRewardRepository] 플레이어 엔티티를 찾을 수 없습니다: {_playerName}");
        }
    }

    private BGEntity FindEntityByName(string playerName)
    {
        if (_meta == null || _meta.CountEntities == 0)
        {
            return null;
        }

        int count = _meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            if (entity.Name == playerName)
            {
                return entity;
            }
        }

        return null;
    }

    public Task<long> LoadLastLoginTimeAsync()
    {
        if (_playerEntity == null)
        {
            return Task.FromResult(0L);
        }

        string timeStr = _playerEntity.Get<string>(LastLoginTimeField) ?? "0";
        long lastLoginTime = long.TryParse(timeStr, out var t) ? t : 0L;

        return Task.FromResult(lastLoginTime);
    }

    public Task SaveLastLoginTimeAsync(long unixTimestamp)
    {
        if (_playerEntity == null)
        {
            return Task.CompletedTask;
        }

        _playerEntity.Set(LastLoginTimeField, unixTimestamp.ToString());
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
