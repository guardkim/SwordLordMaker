public enum EUpgradeId
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
    public static string ToKey(this EUpgradeId id)
    {
        return id switch
        {
            EUpgradeId.PlayerHealth => "Player_Health",
            EUpgradeId.PlayerMoveSpeed => "Player_MoveSpeed",
            EUpgradeId.SwordAttackDamage => "Sword_AttackDamage",
            EUpgradeId.SwordCooldown => "Sword_Cooldown",
            EUpgradeId.SwordMoveSpeed => "Sword_MoveSpeed",
            EUpgradeId.SwordCritDamage => "Sword_CritDamage",
            EUpgradeId.SwordCritChance => "Sword_CritChance",
            _ => string.Empty
        };
    }

    public static EUpgradeId FromKey(string key)
    {
        return key switch
        {
            "Player_Health" => EUpgradeId.PlayerHealth,
            "Player_MoveSpeed" => EUpgradeId.PlayerMoveSpeed,
            "Sword_AttackDamage" => EUpgradeId.SwordAttackDamage,
            "Sword_Cooldown" => EUpgradeId.SwordCooldown,
            "Sword_MoveSpeed" => EUpgradeId.SwordMoveSpeed,
            "Sword_CritDamage" => EUpgradeId.SwordCritDamage,
            "Sword_CritChance" => EUpgradeId.SwordCritChance,
            _ => EUpgradeId.PlayerHealth
        };
    }
}
