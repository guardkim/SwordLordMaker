using System.Collections.Generic;
using System.Numerics;
using BansheeGz.BGDatabase;
using UnityEngine;

public class SwordStatRepository : ISwordStatRepository
{
    private const string TableName = "SwordStat";
    private const string AttackDamageField = "AttackDamage";
    private const string CooldownField = "Cooldown";
    private const string MoveSpeedField = "MoveSpeed";
    private const string CritDamageField = "CritDamage";
    private const string CritChanceField = "CritChance";

    private readonly BGMetaEntity _meta;
    private readonly Dictionary<string, SwordStat> _cache;

    public SwordStatRepository()
    {
        _cache = new Dictionary<string, SwordStat>();
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogError($"[SwordStatRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        LoadAll();
    }

    public List<SwordStat> LoadAll()
    {
        var result = new List<SwordStat>();
        _cache.Clear();

        if (_meta == null) return result;

        int count = _meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            SwordStat stat = CreateStatFromEntity(entity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public SwordStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out SwordStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[SwordStatRepository] 데이터를 찾을 수 없습니다: {id}");
        return null;
    }

    private SwordStat CreateStatFromEntity(BGEntity entity)
    {
        string attackDamageStr = entity.Get<string>(AttackDamageField);
        string critDamageStr = entity.Get<string>(CritDamageField);
        
        BigInteger attackDamage = string.IsNullOrEmpty(attackDamageStr) 
            ? BigInteger.Zero 
            : BigInteger.Parse(attackDamageStr);

        BigInteger critDamage = string.IsNullOrEmpty(critDamageStr) 
            ? BigInteger.Zero 
            : BigInteger.Parse(critDamageStr);
        
        return new SwordStat(
            entity.Name,
            attackDamage,
            entity.Get<float>(CooldownField),
            entity.Get<float>(MoveSpeedField),
            critDamage,
            entity.Get<float>(CritChanceField)
        );
    }
}
