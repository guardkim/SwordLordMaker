using System.Collections.Generic;
using UnityEngine;

public class SwordStatRepository : ISwordStatRepository
{
    private readonly Dictionary<string, SwordStat> _cache;

    public SwordStatRepository()
    {
        _cache = new Dictionary<string, SwordStat>();

        if (DB_SwordStat.CountEntities == 0)
        {
            Debug.LogError("[SwordStatRepository] SwordStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<SwordStat> LoadAll()
    {
        var result = new List<SwordStat>();
        _cache.Clear();

        int count = DB_SwordStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_SwordStat dbEntity = DB_SwordStat.GetEntity(i);
            SwordStat stat = CreateStatFromEntity(dbEntity);
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

    private SwordStat CreateStatFromEntity(DB_SwordStat dbEntity)
    {
        float critDamageMultiplier = dbEntity.F_CritDamage;
        if (critDamageMultiplier <= 0f) critDamageMultiplier = 2.0f;

        return new SwordStat(
            dbEntity.F_name,
            dbEntity.F_AttackDamage,
            dbEntity.F_Cooldown,
            dbEntity.F_MoveSpeed,
            critDamageMultiplier,
            dbEntity.F_CritChance
        );
    }
}
