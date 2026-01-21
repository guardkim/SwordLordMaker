using System;

public class UpgradeRedDotCondition : IRedDotCondition
{
    private readonly RedDotKey _key;
    private readonly string _upgradeId;

    public RedDotKey Key => _key;
    public event Action OnConditionChanged;

    public UpgradeRedDotCondition(RedDotKey key, string upgradeId)
    {
        _key = key;
        _upgradeId = upgradeId;

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += HandleUpgraded;
        }
    }

    public bool CheckCondition()
    {
        if (UpgradeManager.Instance == null || CurrencyManager.Instance == null)
        {
            return false;
        }

        var cost = UpgradeManager.Instance.GetCost(_upgradeId);
        var currentGold = CurrencyManager.Instance.Gold;

        return currentGold >= cost;
    }

    private void HandleCurrencyChanged(CurrencyType type, System.Numerics.BigInteger amount)
    {
        if (type == CurrencyType.Gold)
        {
            OnConditionChanged?.Invoke();
        }
    }

    private void HandleUpgraded(string upgradeId, int newLevel)
    {
        if (upgradeId == _upgradeId)
        {
            OnConditionChanged?.Invoke();
        }
    }

    public void Dispose()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded -= HandleUpgraded;
        }
    }
}
