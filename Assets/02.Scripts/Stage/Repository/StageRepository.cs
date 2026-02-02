using System.Collections.Generic;
using UnityEngine;

public class StageRepository : IStageRepository
{
    private readonly Dictionary<int, StageStat> _cache;
    private int _maxStageId;

    public StageRepository()
    {
        _cache = new Dictionary<int, StageStat>();

        if (DB_StageStat.CountEntities == 0)
        {
            Debug.LogError("[StageRepository] StageStat 테이블이 비어있습니다.");
            return;
        }

        LoadAll();
    }

    public List<StageStat> LoadAll()
    {
        var result = new List<StageStat>();
        _cache.Clear();
        _maxStageId = 0;

        int count = DB_StageStat.CountEntities;
        for (int i = 0; i < count; i++)
        {
            DB_StageStat dbEntity = DB_StageStat.GetEntity(i);
            StageStat stat = CreateStatFromEntity(dbEntity);
            _cache[stat.StageId] = stat;
            result.Add(stat);

            if (stat.StageId > _maxStageId)
            {
                _maxStageId = stat.StageId;
            }
        }

        return result;
    }

    public StageStat GetByStageId(int stageId)
    {
        if (_cache.TryGetValue(stageId, out StageStat stat))
        {
            return stat;
        }

        Debug.LogWarning($"[StageRepository] 스테이지를 찾을 수 없습니다: {stageId}");
        return null;
    }

    public int GetMaxStageId()
    {
        return _maxStageId;
    }

    private StageStat CreateStatFromEntity(DB_StageStat dbEntity)
    {
        float hpMult = dbEntity.F_HpMultiplier;
        float atkMult = dbEntity.F_AttackMultiplier;
        float spdMult = dbEntity.F_SpeedMultiplier;
        float goldMult = dbEntity.F_GoldMultiplier;
        float expMult = dbEntity.F_ExpMultiplier;

        return new StageStat(
            dbEntity.F_StageId,
            dbEntity.F_name,
            dbEntity.F_EnemyStatId,
            dbEntity.F_BossStatId ?? "",
            hpMult <= 0 ? 1f : hpMult,
            atkMult <= 0 ? 1f : atkMult,
            spdMult <= 0 ? 1f : spdMult,
            goldMult <= 0 ? 1f : goldMult,
            expMult <= 0 ? 1f : expMult
        );
    }
}
