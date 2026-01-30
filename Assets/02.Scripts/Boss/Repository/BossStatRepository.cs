using System.Collections.Generic;
using UnityEngine;

public class BossStatRepository : IBossStatRepository
{
    private readonly Dictionary<string, BossStat> _cache;

    public BossStatRepository()
    {
        _cache = new Dictionary<string, BossStat>();

        if (DB_BossStat.CountEntities == 0)
        {
            Debug.LogWarning("[BossStatRepository] BossStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<BossStat> LoadAll()
    {
        var result = new List<BossStat>();
        _cache.Clear();

        int count = DB_BossStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_BossStat dbEntity = DB_BossStat.GetEntity(i);
            BossStat stat = CreateStatFromEntity(dbEntity);
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

    private BossStat CreateStatFromEntity(DB_BossStat dbEntity)
    {
        return new BossStat(
            dbEntity.F_name,
            dbEntity.F_MaxHP,
            dbEntity.F_AttackDamage,
            dbEntity.F_MoveSpeed,
            dbEntity.F_GoldReward,
            dbEntity.F_Exp
        );
    }
}
