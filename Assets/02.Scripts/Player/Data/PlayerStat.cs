public class PlayerStat
{
    public string Id { get; private set; }
    public double BaseMaxHealth { get; set; }
    public float BaseMoveSpeed { get; set; }
    public int Level { get; set; }
    public double CurrentExp { get; set; }
    public double MaxExp { get; set; }

    public PlayerStat(
        string id,
        double baseMaxHealth,
        float baseMoveSpeed,
        int level,
        double currentExp,
        double maxExp)
    {
        Id = id;
        BaseMaxHealth = baseMaxHealth;
        BaseMoveSpeed = baseMoveSpeed;
        Level = level;
        CurrentExp = currentExp;
        MaxExp = maxExp;
    }
}
