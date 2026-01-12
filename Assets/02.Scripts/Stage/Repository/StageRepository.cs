using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGDatabase;
using UnityEngine;

public class StageRepository : IStageRepository
{
    private const string TableName = "StageStat";
    // name 필드는 BGDatabase 기본 필드 (entity.Name으로 접근) - StageName으로 사용
    private const string StageIdField = "StageId";
    private const string EnemyStatIdField = "EnemyStatId";
    private const string SpawnCountField = "SpawnCount";

    private readonly BGMetaEntity _meta;
    private readonly Dictionary<int, StageStat> _cache;
    private int _maxStageId;

    public StageRepository()
    {
        _cache = new Dictionary<int, StageStat>();
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogError($"[StageRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        LoadAll();
    }

    public List<StageStat> LoadAll()
    {
        var result = new List<StageStat>();
        _cache.Clear();
        _maxStageId = 0;

        if (_meta == null)
        {
            return result;
        }

        int count = _meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            StageStat stat = CreateStatFromEntity(entity);
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

    private StageStat CreateStatFromEntity(BGEntity entity)
    {
        return new StageStat(
            entity.Get<int>(StageIdField),
            entity.Name,  // BGDatabase 기본 name 필드를 StageName으로 사용
            entity.Get<string>(EnemyStatIdField),
            entity.Get<int>(SpawnCountField)
        );
    }
}
