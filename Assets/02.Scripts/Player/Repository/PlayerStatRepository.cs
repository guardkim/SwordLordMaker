using System.Numerics;
using BansheeGz.BGDatabase;
using UnityEngine;

public class PlayerStatRepository : IPlayerStatRepository
{
    private const string TableName = "PlayerStat";
    private const string BaseMaxHealthField = "BaseMaxHealth";
    private const string BaseMoveSpeedField = "BaseMoveSpeed";

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
            // 기본값 반환
            _cachedStat = new PlayerStat("Default", new BigInteger(100), 5f);
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

        _cachedStat = new PlayerStat(
            entity.Name,
            baseMaxHealth,
            baseMoveSpeed
        );

        return _cachedStat;
    }
}
