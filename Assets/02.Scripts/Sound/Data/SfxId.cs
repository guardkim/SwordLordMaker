public enum SfxId
{
    None = 0,

    // 검 효과음
    SwordAttack,
    SwordHit,

    // 몬스터
    MonsterDead,

    // 전투 효과음
    Hit,
    CriticalHit,
    SwordSwing,
    SwordImpact,
    Explosion,

    // 플레이어
    PlayerHit,
    PlayerDeath,
    Footstep,

    // UI
    ButtonClick,
    PopupOpen,
    PopupClose,
    Purchase,
    Upgrade,
    Error,

    // 보상
    GoldPickup,
    ExpPickup,
    ItemPickup,
    LevelUp
}

public static class SfxIdExtensions
{
    public static string ToKey(this SfxId id)
    {
        return id.ToString();
    }
}
