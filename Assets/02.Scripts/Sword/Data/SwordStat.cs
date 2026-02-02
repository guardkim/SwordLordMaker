public class SwordStat
{
    public string Id { get; private set; }
    public double AttackDamage { get; set; }
    public float Cooldown { get; set; }
    public float MoveSpeed { get; set; }
    public float CritDamageMultiplier { get; set; }
    public float CritChance { get; set; }

    public SwordStat(
        string id,
        double attackDamage,
        float cooldown,
        float moveSpeed,
        float critDamageMultiplier,
        float critChance)
    {
        Id = id;
        AttackDamage = attackDamage;
        Cooldown = cooldown;
        MoveSpeed = moveSpeed;
        CritDamageMultiplier = critDamageMultiplier;
        CritChance = critChance;
    }

    public SwordStat(SwordStat other)
    {
        Id = other.Id;
        AttackDamage = other.AttackDamage;
        Cooldown = other.Cooldown;
        MoveSpeed = other.MoveSpeed;
        CritDamageMultiplier = other.CritDamageMultiplier;
        CritChance = other.CritChance;
    }

    public void CopyFrom(SwordStat source)
    {
        AttackDamage = source.AttackDamage;
        Cooldown = source.Cooldown;
        MoveSpeed = source.MoveSpeed;
        CritDamageMultiplier = source.CritDamageMultiplier;
        CritChance = source.CritChance;
    }

    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
}
