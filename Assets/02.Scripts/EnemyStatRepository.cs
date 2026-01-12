using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public class EnemyStatRepository : IEnemyStatRepository
{
    private const string TableName = "EnemyStat";
    // name 필드는 BGDatabase 기본 필드 (entity.Name으로 접근)
    private const string MaxHPField = "MaxHP";
    private const string AttackDamageField = "AttackDamage";
    private const string MoveSpeedField = "MoveSpeed";
    private const string GoldRewardField = "GoldReward";

    private readonly BGMetaEntity _meta;
    private readonly Dictionary<string, EnemyStat> _cache;

    public EnemyStatRepository()
    {
        _cache = new Dictionary<string, EnemyStat>();
        _meta = BGRepo.I[TableName];

        if (_meta == null)
        {
            Debug.LogError($"[EnemyStatRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        LoadAll();
    }

    public List<EnemyStat> LoadAll()
    {
        var result = new List<EnemyStat>();
        _cache.Clear();

        if (_meta == null)
        {
            return result;
        }

        int count = _meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _meta.GetEntity(i);
            EnemyStat stat = CreateStatFromEntity(entity);
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

    private EnemyStat CreateStatFromEntity(BGEntity entity)
    {
        return new EnemyStat(
            entity.Name,  // BGDatabase 기본 name 필드를 Id로 사용
            entity.Get<int>(MaxHPField),
            entity.Get<int>(AttackDamageField),
            entity.Get<float>(MoveSpeedField),
            entity.Get<int>(GoldRewardField)
        );
    }
}
