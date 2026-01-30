using System;

public record UpgradeData(
    string Id,
    string DisplayName,
    double BaseCost,
    float CostMultiplier,
    double BonusPerLevel
)
{
    public double GetCost(int currentLevel)
    {
        double multiplier = Math.Pow(CostMultiplier, currentLevel);
        return BaseCost * multiplier;
    }

    public float GetTotalBonus(int level)
    {
        return (float)BonusPerLevel * level;
    }

    public double GetTotalDoubleBonus(int level)
    {
        return BonusPerLevel * level;
    }
}
