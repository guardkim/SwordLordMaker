using System.Numerics;

public record BossStat(
    string Id,
    BigInteger MaxHP,
    BigInteger AttackDamage,
    float MoveSpeed,
    BigInteger GoldReward,
    double Exp
);
