using System;

public class UpgradeData
{
    public string Id { get; private set; }
    public string DisplayName { get; private set; }
    public double BaseCost { get; private set; }
    public float CostMultiplier { get; private set; }
    public double BonusPerLevel { get; private set; }

    public UpgradeData(
        string id,
        string displayName,
        double baseCost,
        float costMultiplier,
        double bonusPerLevel)
    {
        Id = id;
        DisplayName = displayName;
        BaseCost = baseCost;
        CostMultiplier = costMultiplier;
        BonusPerLevel = bonusPerLevel;
    }

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
