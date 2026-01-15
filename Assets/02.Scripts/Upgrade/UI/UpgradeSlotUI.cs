using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSlotUI : MonoBehaviour
{
    [Header("▼ 강화 ID")]
    [SerializeField] private string _upgradeId;

    [Header("▼ UI 요소")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _bonusText;
    [SerializeField] private Button _upgradeButton;

    private UpgradeData _upgradeData;

    public string UpgradeId => _upgradeId;

    private void Start()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        Initialize(_upgradeId);
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }

    public void Initialize(string upgradeId)
    {
        _upgradeId = upgradeId;

        if (UpgradeManager.Instance == null) return;

        _upgradeData = UpgradeManager.Instance.GetUpgradeData(upgradeId);

        UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        Refresh();
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        if (upgradeId == _upgradeId)
        {
            Refresh();
        }
    }

    private void OnCurrencyChanged(CurrencyType type, System.Numerics.BigInteger amount)
    {
        if (type == CurrencyType.Gold)
        {
            RefreshButtonState();
        }
    }

    public void Refresh()
    {
        if (_upgradeData == null || UpgradeManager.Instance == null) return;

        int currentLevel = UpgradeManager.Instance.GetLevel(_upgradeId);
        int maxLevel = _upgradeData.MaxLevel;
        BigInteger cost = _upgradeData.GetCost(currentLevel);
        BigInteger totalBonus = _upgradeData.GetTotalBigIntBonus(currentLevel);
        BigInteger nextBonus = ParseBonusPerLevel(_upgradeData.BonusPerLevel);
        bool isMaxLevel = currentLevel >= maxLevel;

        // 이름
        if (_nameText != null)
        {
            _nameText.text = _upgradeData.DisplayName;
        }

        // 레벨
        if (_levelText != null)
        {
            _levelText.text = isMaxLevel ? $"Lv.{currentLevel} (MAX)" : $"Lv.{currentLevel} / {maxLevel}";
        }

        // 비용
        if (_costText != null)
        {
            _costText.text = isMaxLevel ? "-" : $"{CurrencyFormatter.FormatAbbreviated(cost)} G";
        }

        // 보너스
        if (_bonusText != null)
        {
            string totalBonusStr = CurrencyFormatter.FormatAbbreviated(totalBonus);
            if (isMaxLevel)
            {
                _bonusText.text = $"+{totalBonusStr}";
            }
            else
            {
                string nextBonusStr = CurrencyFormatter.FormatAbbreviated(nextBonus);
                _bonusText.text = $"+{totalBonusStr} (+{nextBonusStr})";
            }
        }

        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (_upgradeButton == null || UpgradeManager.Instance == null) return;

        bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(_upgradeId);
        BigInteger cost = UpgradeManager.Instance.GetCost(_upgradeId);
        bool canAfford = CurrencyManager.Instance != null &&
                         CurrencyManager.Instance.Gold >= cost;

        _upgradeButton.interactable = !isMaxLevel && canAfford;
    }

    private void OnUpgradeClicked()
    {
        if (UpgradeManager.Instance == null) return;

        UpgradeManager.Instance.TryUpgrade(_upgradeId);
    }

    private BigInteger ParseBonusPerLevel(string value)
    {
        if (BigInteger.TryParse(value, out BigInteger intBonus))
        {
            return intBonus;
        }

        if (double.TryParse(value, out double floatBonus))
        {
            return new BigInteger(floatBonus);
        }

        return BigInteger.Zero;
    }
}
