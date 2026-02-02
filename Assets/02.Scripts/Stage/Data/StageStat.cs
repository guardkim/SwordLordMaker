public class StageStat
{
    public int StageId { get; private set; }
    public string StageName { get; private set; }
    public string EnemyStatId { get; private set; }
    public string BossStatId { get; private set; }
    public float HpMultiplier { get; private set; }
    public float AttackMultiplier { get; private set; }
    public float SpeedMultiplier { get; private set; }
    public float GoldMultiplier { get; private set; }
    public float ExpMultiplier { get; private set; }

    public StageStat(
        int stageId,
        string stageName,
        string enemyStatId,
        string bossStatId,
        float hpMultiplier,
        float attackMultiplier,
        float speedMultiplier,
        float goldMultiplier,
        float expMultiplier)
    {
        StageId = stageId;
        StageName = stageName;
        EnemyStatId = enemyStatId;
        BossStatId = bossStatId;
        HpMultiplier = hpMultiplier;
        AttackMultiplier = attackMultiplier;
        SpeedMultiplier = speedMultiplier;
        GoldMultiplier = goldMultiplier;
        ExpMultiplier = expMultiplier;
    }
}
