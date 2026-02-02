using System.Threading.Tasks;
using BansheeGz.BGDatabase;
using UnityEngine;

public class OfflineRewardRepository : IOfflineRewardRepository
{
    private readonly string _playerName;
    private DB_PlayerProfile _playerEntity;

    public OfflineRewardRepository(string playerName)
    {
        _playerName = playerName;
        InitializeDatabase();
    }

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

    public void ForceSaveToDisk()
    {
        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
