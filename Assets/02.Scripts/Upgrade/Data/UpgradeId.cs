public enum UpgradeId
{
    PlayerHealth = 0,
    PlayerMoveSpeed = 1,
    SwordAttackDamage = 2,
    SwordCooldown = 3,
    SwordMoveSpeed = 4,
    SwordCritDamage = 5,
    SwordCritChance = 6
}

public static class UpgradeIdExtensions
{
    public static string ToKey(this UpgradeId id)
    {
        return id switch
        {
            UpgradeId.PlayerHealth => "Player_Health",
            UpgradeId.PlayerMoveSpeed => "Player_MoveSpeed",
            UpgradeId.SwordAttackDamage => "Sword_AttackDamage",
            UpgradeId.SwordCooldown => "Sword_Cooldown",
            UpgradeId.SwordMoveSpeed => "Sword_MoveSpeed",
            UpgradeId.SwordCritDamage => "Sword_CritDamage",
            UpgradeId.SwordCritChance => "Sword_CritChance",
            _ => string.Empty
        };
    }

    public static UpgradeId FromKey(string key)
    {
        return key switch
        {
            "Player_Health" => UpgradeId.PlayerHealth,
            "Player_MoveSpeed" => UpgradeId.PlayerMoveSpeed,
            "Sword_AttackDamage" => UpgradeId.SwordAttackDamage,
            "Sword_Cooldown" => UpgradeId.SwordCooldown,
            "Sword_MoveSpeed" => UpgradeId.SwordMoveSpeed,
            "Sword_CritDamage" => UpgradeId.SwordCritDamage,
            "Sword_CritChance" => UpgradeId.SwordCritChance,
            _ => UpgradeId.PlayerHealth
        };
    }
}
