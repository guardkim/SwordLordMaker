using System;
using System.Numerics;

public record UpgradeData(
    string Id,
    string DisplayName,
    string BaseCost,
    float CostMultiplier,
    string BonusPerLevel,
    int MaxLevel
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
        // BonusPerLevel이 정수 또는 소수일 수 있음
        if (BigInteger.TryParse(BonusPerLevel, out BigInteger intBonus))
        {
            return intBonus * level;
        }

        // 소수점 값인 경우 double로 파싱 후 변환
        if (double.TryParse(BonusPerLevel, out double floatBonus))
        {
            return new BigInteger(floatBonus * level);
        }

        return BigInteger.Zero;
    }

    public bool IsMaxLevel(int currentLevel)
    {
        return currentLevel >= MaxLevel;
    }
}
