using System;
using System.Numerics;

public record UpgradeData(
    string Id,
    string DisplayName,
    string BaseCost,
    float CostMultiplier,
    string BonusPerLevel
)
{
    public BigInteger GetCost(int currentLevel)
    {
        BigInteger baseCost = BigInteger.Parse(BaseCost);
        double multiplier = Math.Pow(CostMultiplier, currentLevel);
        return new BigInteger(multiplier) * baseCost;
    }

    public float GetTotalBonus(int level)
    {
        return float.Parse(BonusPerLevel) * level;
    }

    public BigInteger GetTotalBigIntBonus(int level)
    {
        if (BigInteger.TryParse(BonusPerLevel, out BigInteger intBonus))
        {
            return intBonus * level;
        }

        if (double.TryParse(BonusPerLevel, out double floatBonus))
        {
            return new BigInteger(floatBonus * level);
        }

        return BigInteger.Zero;
    }
}
