public enum BgmId
{
    None = 0,

    // 메인
    Title,
    Main,

    // 전투
    Battle,
    BossBattle,

    // 기타
    Victory,
    Defeat
}

public static class BgmIdExtensions
{
    public static string ToKey(this BgmId id)
    {
        return id.ToString();
    }
}
