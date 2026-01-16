using System.Numerics;

public record PlayerStat(
    string Id,
    BigInteger BaseMaxHealth,
    float BaseMoveSpeed,
    int Level,
    double CurrentExp,
    double MaxExp
);
