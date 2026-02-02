public class EnemyStat
{
    public string Id { get; private set; }
    public double MaxHP { get; private set; }
    public double AttackDamage { get; private set; }
    public float MoveSpeed { get; private set; }
    public double GoldReward { get; private set; }
    public double Exp { get; private set; }

    public EnemyStat(
        string id,
        double maxHP,
        double attackDamage,
        float moveSpeed,
        double goldReward,
        double exp)
    {
        Id = id;
        MaxHP = maxHP;
        AttackDamage = attackDamage;
        MoveSpeed = moveSpeed;
        GoldReward = goldReward;
        Exp = exp;
    }
}
