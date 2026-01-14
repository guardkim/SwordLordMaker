using UnityEngine;

public record UpgradeData(
    string Id,
    string DisplayName,
    int BaseCost,
    float CostMultiplier,
    float BonusPerLevel,
    int MaxLevel
)
{
    public int GetCost(int currentLevel)
    {
        return (int)(BaseCost * Mathf.Pow(CostMultiplier, currentLevel));
    }

    public float GetTotalBonus(int level)
    {
        return BonusPerLevel * level;
    }

    public bool IsMaxLevel(int currentLevel)
    {
        return currentLevel >= MaxLevel;
    }
}
