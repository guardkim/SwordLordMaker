using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("▼ 강화 ID")]
    [SerializeField] private UpgradeId _upgradeId;

    [Header("▼ UI 요소")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _bonusText;
    [SerializeField] private Image _upgradeButtonImage;
    [SerializeField] private Color _enabledColor = Color.white;
    [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool _interactable = true;
    private UpgradeData _upgradeData;
    private string _upgradeKey;

    public UpgradeId UpgradeId => _upgradeId;

    private void Start()
    {
        Initialize(_upgradeId);
    }

    private void OnDestroy()
    {
        if (UpgradeManager.HasInstance)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized -= OnUpgradeManagerInitialized;
        }

        if (CurrencyManager.HasInstance)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable)
        {
            return;
        }

        OnUpgradeClicked();
    }

    public void Initialize(UpgradeId upgradeId)
    {
        _upgradeId = upgradeId;
        _upgradeKey = upgradeId.ToKey();

        if (UpgradeManager.Instance == null)
        {
            return;
        }

        UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
        UpgradeManager.Instance.OnInitialized += OnUpgradeManagerInitialized;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        // 아직 초기화 안 됐으면 이벤트로 대기
        if (!UpgradeManager.Instance.IsReady)
        {
            return;
        }

        _upgradeData = UpgradeManager.Instance.GetUpgradeData(_upgradeKey);
        Refresh();
    }

    private void OnUpgradeManagerInitialized()
    {
        _upgradeData = UpgradeManager.Instance.GetUpgradeData(_upgradeKey);
        Refresh();
    }

    private void OnUpgradeChanged(string upgradeKey, int newLevel)
    {
        if (upgradeKey == _upgradeKey)
        {
            Refresh();
        }
    }

    private void OnCurrencyChanged(CurrencyType type, double amount)
    {
        if (type == CurrencyType.Gold)
        {
            RefreshButtonState();
        }
    }

    public void Refresh()
    {
        if (_upgradeData == null || UpgradeManager.Instance == null)
        {
            return;
        }

        int currentLevel = UpgradeManager.Instance.GetLevel(_upgradeKey);
        double cost = _upgradeData.GetCost(currentLevel);
        double totalBonus = _upgradeData.GetTotalBonus(currentLevel);
        double nextBonus = _upgradeData.BonusPerLevel;

        if (_nameText != null)
        {
            _nameText.text = _upgradeData.DisplayName;
        }

        if (_levelText != null)
        {
            _levelText.text = $"Lv.{currentLevel}";
        }

        if (_costText != null)
        {
            _costText.text = $"{CurrencyFormatter.FormatKorean(cost)} G";
        }

        if (_bonusText != null)
        {
            float totalBonusFloat = _upgradeData.GetTotalBonus(currentLevel);
            float nextBonusFloat = (float)_upgradeData.BonusPerLevel;

            string totalBonusStr = FormatBonus(_upgradeId, totalBonusFloat, totalBonus);
            string nextBonusStr = FormatBonus(_upgradeId, nextBonusFloat, nextBonus);

            _bonusText.text = $"+{totalBonusStr} (+{nextBonusStr})";
        }

        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (UpgradeManager.Instance == null)
        {
            return;
        }

        double cost = UpgradeManager.Instance.GetCost(_upgradeKey);
        bool canAfford = CurrencyManager.Instance != null &&
                         CurrencyManager.Instance.Gold >= cost;

        _interactable = canAfford;

        if (_upgradeButtonImage != null)
        {
            _upgradeButtonImage.color = _interactable ? _enabledColor : _disabledColor;
        }
    }

    private void OnUpgradeClicked()
    {
        if (UpgradeManager.Instance == null)
        {
            return;
        }

        UpgradeManager.Instance.TryUpgrade(_upgradeKey);
    }

 

 

    private string FormatBonus(UpgradeId upgradeId, float floatValue, double bigIntValue)
    {
        switch (upgradeId)
        {
            case UpgradeId.SwordCritChance:
                return $"{floatValue * 100f:F1}%";

            case UpgradeId.SwordCritDamage:
                return $"{floatValue:F1}배";

            case UpgradeId.SwordCooldown:
                return $"{floatValue:F2}초";

            case UpgradeId.SwordMoveSpeed:
            case UpgradeId.PlayerMoveSpeed:
                return $"{floatValue:F1}배";

            case UpgradeId.PlayerHealth:
            case UpgradeId.SwordAttackDamage:
            default:
                return CurrencyFormatter.FormatKorean(bigIntValue);
        }
    }
}
