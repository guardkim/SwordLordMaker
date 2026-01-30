using BansheeGz.BGDatabase;
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

        Debug.Log($"[PlayerStatRepository] 로드 완료 - Level: {level}, CurrentExp: {_cachedStat.CurrentExp}, MaxExp: {maxExp}");

        return _cachedStat;
    }

    public void Save(PlayerStat stat)
    {
        if (_playerEntity == null)
        {
            Debug.LogWarning("[PlayerStatRepository] PlayerEntity가 없어 저장할 수 없습니다.");
            return;
        }

        Debug.Log($"[PlayerStatRepository] 저장 시작 - Level: {stat.Level}, CurrentExp: {stat.CurrentExp}, MaxExp: {stat.MaxExp}");

        _playerEntity.F_BaseMaxHealth = stat.BaseMaxHealth;
        _playerEntity.F_BaseMoveSpeed = stat.BaseMoveSpeed;
        _playerEntity.F_Level = stat.Level;
        _playerEntity.F_CurrentExp = stat.CurrentExp;
        _playerEntity.F_MaxExp = stat.MaxExp;

        _cachedStat = stat;

        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("[PlayerStatRepository] 저장 완료");
    }
}
