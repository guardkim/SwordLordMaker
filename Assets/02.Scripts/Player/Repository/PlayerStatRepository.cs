using System.Numerics;
using BansheeGz.BGDatabase;
using UnityEngine;

public class PlayerStatRepository : IPlayerStatRepository
{
    private const string TableName = "PlayerStat";
    private const string BaseMaxHealthField = "BaseMaxHealth";
    private const string BaseMoveSpeedField = "BaseMoveSpeed";
    private const string LevelField = "Level";
    private const string CurrentExpField = "CurrentExp";
    private const string MaxExpField = "MaxExp";

    private readonly string _playerName;
    private readonly BGMetaEntity _meta;
    private BGEntity _playerEntity;
    private PlayerStat _cachedStat;

    public PlayerStatRepository(string playerName)
    {
        _playerName = playerName;
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogWarning($"[PlayerStatRepository] 테이블을 찾을 수 없습니다: {TableName}. 기본값 사용.");
        }

        InitializePlayerEntity();
    }

    private void InitializePlayerEntity()
    {
        _playerEntity = FindEntityByName(_playerName);
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

    private BGEntity FindEntityByName(string playerName)
    {
        if (_meta == null || _meta.CountEntities == 0)
        {
            Debug.Log($"[PlayerStatRepository] FindEntityByName - 테이블 없거나 비어있음");
            return null;
        }

        int count = _meta.CountEntities;
        Debug.Log($"[PlayerStatRepository] FindEntityByName - 검색할 이름: '{playerName}', 총 엔티티 수: {count}");

        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            Debug.Log($"[PlayerStatRepository] 엔티티[{i}]: Name='{entity.Name}', Level={entity.Get<int>(LevelField)}");

            if (entity.Name == playerName)
            {
                return entity;
            }
        }

        return null;
    }

    private BGEntity CreateNewPlayerEntity()
    {
        if (_meta == null)
        {
            return null;
        }

        BGEntity entity = _meta.NewEntity();
        entity.Name = _playerName;
        entity.Set(BaseMaxHealthField, "100");
        entity.Set(BaseMoveSpeedField, 5f);
        entity.Set(LevelField, 1);
        entity.Set(CurrentExpField, 0.0);
        entity.Set(MaxExpField, 10.0);
        return entity;
    }

    public PlayerStat Load()
    {
        if (_playerEntity == null)
        {
            _cachedStat = new PlayerStat(_playerName, new BigInteger(100), 5f, 1, 0.0, 10.0);
            return _cachedStat;
        }

        string healthStr = _playerEntity.Get<string>(BaseMaxHealthField);

        BigInteger baseMaxHealth = string.IsNullOrEmpty(healthStr)
            ? new BigInteger(100)
            : BigInteger.Parse(healthStr);

        float baseMoveSpeed = _playerEntity.Get<float>(BaseMoveSpeedField);
        if (baseMoveSpeed <= 0f)
        {
            baseMoveSpeed = 5f;
        }

        int level = _playerEntity.Get<int>(LevelField);
        double currentExp = _playerEntity.Get<double>(CurrentExpField);
        double maxExp = _playerEntity.Get<double>(MaxExpField);

        // 기본값 검증: Level은 최소 1, MaxExp는 최소 10.0
        if (level < 1)
        {
            level = 1;
        }

        if (maxExp <= 0.0)
        {
            maxExp = 10.0 * System.Math.Pow(2, level - 1);
        }

        _cachedStat = new PlayerStat(
            _playerEntity.Name,
            baseMaxHealth,
            baseMoveSpeed,
            level,
            currentExp,
            maxExp
        );

        Debug.Log($"[PlayerStatRepository] 로드 완료 - Level: {level}, CurrentExp: {currentExp}, MaxExp: {maxExp}");

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

        _playerEntity.Set(BaseMaxHealthField, stat.BaseMaxHealth.ToString());
        _playerEntity.Set(BaseMoveSpeedField, stat.BaseMoveSpeed);
        _playerEntity.Set(LevelField, stat.Level);
        _playerEntity.Set(CurrentExpField, stat.CurrentExp);
        _playerEntity.Set(MaxExpField, stat.MaxExp);

        _cachedStat = stat;

        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("[PlayerStatRepository] 저장 완료");
    }
}
