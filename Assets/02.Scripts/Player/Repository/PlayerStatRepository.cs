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

    private readonly BGMetaEntity _meta;
    private PlayerStat _cachedStat;

    public PlayerStatRepository()
    {
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogWarning($"[PlayerStatRepository] 테이블을 찾을 수 없습니다: {TableName}. 기본값 사용.");
        }

        Load();
    }

    public PlayerStat Load()
    {
        if (_meta == null || _meta.CountEntities == 0)
        {
            _cachedStat = new PlayerStat("Default", new BigInteger(100), 5f, 1, 0.0, 10.0);
            return _cachedStat;
        }

        BGEntity entity = _meta.GetEntity(0);
        string healthStr = entity.Get<string>(BaseMaxHealthField);

        BigInteger baseMaxHealth = string.IsNullOrEmpty(healthStr)
            ? new BigInteger(100)
            : BigInteger.Parse(healthStr);

        float baseMoveSpeed = entity.Get<float>(BaseMoveSpeedField);
        if (baseMoveSpeed <= 0f)
        {
            baseMoveSpeed = 5f;
        }

        int level = entity.Get<int>(LevelField);
        double currentExp = entity.Get<double>(CurrentExpField);
        double maxExp = entity.Get<double>(MaxExpField);

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
            entity.Name,
            baseMaxHealth,
            baseMoveSpeed,
            level,
            currentExp,
            maxExp
        );

        return _cachedStat;
    }

    public void Save(PlayerStat stat)
    {
        if (_meta == null || _meta.CountEntities == 0)
        {
            Debug.LogWarning("[PlayerStatRepository] 테이블이 없어 저장할 수 없습니다.");
            return;
        }

        BGEntity entity = _meta.GetEntity(0);
        entity.Set(BaseMaxHealthField, stat.BaseMaxHealth.ToString());
        entity.Set(BaseMoveSpeedField, stat.BaseMoveSpeed);
        entity.Set(LevelField, stat.Level);
        entity.Set(CurrentExpField, stat.CurrentExp);
        entity.Set(MaxExpField, stat.MaxExp);

        _cachedStat = stat;
    }
}
