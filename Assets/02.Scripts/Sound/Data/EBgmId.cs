public enum EBgmId
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
    public static string ToKey(this EBgmId id)
    {
        return id.ToString();
    }
}
