using System.Collections.Generic;
using UnityEngine;

public class EnemyStatRepository : IEnemyStatRepository
{
    private readonly Dictionary<string, EnemyStat> _cache;

    public EnemyStatRepository()
    {
        _cache = new Dictionary<string, EnemyStat>();

        if (DB_EnemyStat.CountEntities == 0)
        {
            Debug.LogError("[EnemyStatRepository] EnemyStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<EnemyStat> LoadAll()
    {
        var result = new List<EnemyStat>();
        _cache.Clear();

        int count = DB_EnemyStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_EnemyStat dbEntity = DB_EnemyStat.GetEntity(i);
            EnemyStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.Id] = stat;
            result.Add(stat);
        }

        return result;
    }

    public EnemyStat GetById(string id)
    {
        if (_cache.TryGetValue(id, out EnemyStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[EnemyStatRepository] 스탯을 찾을 수 없습니다: {id}");
        return null;
    }

    private EnemyStat CreateStatFromEntity(DB_EnemyStat dbEntity)
    {
        return new EnemyStat(
            dbEntity.F_name,
            dbEntity.F_MaxHP,
            dbEntity.F_AttackDamage,
            dbEntity.F_MoveSpeed,
            dbEntity.F_GoldReward,
            dbEntity.F_Exp
        );
    }
}
