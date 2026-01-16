using System.Collections.Generic;
using System.Numerics;
using BansheeGz.BGDatabase;
using UnityEngine;

public class BossStatRepository : IBossStatRepository
{
    private const string TableName = "BossStat";
    private const string MaxHPField = "MaxHP";
    private const string AttackDamageField = "AttackDamage";
    private const string MoveSpeedField = "MoveSpeed";
    private const string GoldRewardField = "GoldReward";

    private readonly BGMetaEntity _meta;
    private readonly Dictionary<string, BossStat> _cache;
    private const string ExpField = "Exp";

    public BossStatRepository()
    {
        _cache = new Dictionary<string, BossStat>();
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogWarning($"[BossStatRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        LoadAll();
    }

    public List<BossStat> LoadAll()
    {
        var result = new List<BossStat>();
        _cache.Clear();

        if (_meta == null) return result;

        int count = _meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            BossStat stat = CreateStatFromEntity(entity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public BossStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out BossStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[BossStatRepository] 보스 스탯을 찾을 수 없습니다: {id}");
        return null;
    }

    private BossStat CreateStatFromEntity(BGEntity entity)
    {
        string maxHpStr = entity.Get<string>(MaxHPField);
        string atkStr = entity.Get<string>(AttackDamageField);
        string goldStr = entity.Get<string>(GoldRewardField);
        double exp = entity.Get<double>(ExpField);

        BigInteger maxHP = string.IsNullOrEmpty(maxHpStr) ? BigInteger.Zero : BigInteger.Parse(maxHpStr);
        BigInteger attackDamage = string.IsNullOrEmpty(atkStr) ? BigInteger.Zero : BigInteger.Parse(atkStr);
        BigInteger goldReward = string.IsNullOrEmpty(goldStr) ? BigInteger.Zero : BigInteger.Parse(goldStr);

        return new BossStat(
            entity.Name,
            maxHP,
            attackDamage,
            entity.Get<float>(MoveSpeedField),
            goldReward,
            exp
        );
    }
}
