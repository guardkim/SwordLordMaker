using System.Numerics;
using UnityEngine;

public record UpgradeData(
    string Id,
    string DisplayName,
    int BaseCost,
    float CostMultiplier, 
    string BonusPerLevel,
    int MaxLevel
)
{
    public int GetCost(int currentLevel)
    {
        return (int)(BaseCost * Mathf.Pow(CostMultiplier, currentLevel));
    }

    public float GetTotalBonus(int level)
    {
        return float.Parse(BonusPerLevel) * level;
    }
    public BigInteger GetTotalBigIntBonus(int level)
    {
        BigInteger bonus = BigInteger.Parse(BonusPerLevel);
        return bonus * level;
    }
    public bool IsMaxLevel(int currentLevel)
    {
        return currentLevel >= MaxLevel;
    }
}
