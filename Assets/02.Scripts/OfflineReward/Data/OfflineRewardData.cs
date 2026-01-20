using System.Numerics;

public record OfflineRewardData(
    long LastLoginTime,
    BigInteger GoldPerMinute,
    double ExpPerMinute
);
