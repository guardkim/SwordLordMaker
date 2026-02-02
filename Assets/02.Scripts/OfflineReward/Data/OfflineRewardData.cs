public class OfflineRewardData
{
    public long LastLoginTime { get; set; }
    public double GoldPerMinute { get; set; }
    public double ExpPerMinute { get; set; }

    public OfflineRewardData(long lastLoginTime, double goldPerMinute, double expPerMinute)
    {
        LastLoginTime = lastLoginTime;
        GoldPerMinute = goldPerMinute;
        ExpPerMinute = expPerMinute;
    }
}
