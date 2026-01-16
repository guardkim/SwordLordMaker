using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerProfileUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _playerIdText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _expText;
    [SerializeField] private Slider _expSlider;

    private void Start()
    {
        InitializePlayerId();
        InitializeLevel();
        InitializeExp();
    }

    private void InitializePlayerId()
    {
        if (PlayerSessionManager.Instance == null)
        {
            Debug.LogError("[PlayerProfileUI] PlayerSessionManager not found.");
            if (_playerIdText != null)
            {
                _playerIdText.text = "N/A";
            }
            return;
        }

        if (_playerIdText != null)
        {
            _playerIdText.text = PlayerSessionManager.Instance.CurrentPlayerName;
        }
    }

    private void InitializeLevel()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("[PlayerProfileUI] UpgradeManager not found.");
            if (_levelText != null)
            {
                _levelText.text = "0";
            }
            return;
        }

        UpdateLevelDisplay();
    }

    private void InitializeExp()
    {
        if (PlayerStatManager.Instance == null)
        {
            Debug.LogError("[PlayerProfileUI] PlayerStatManager not found.");
            if (_expText != null)
            {
                _expText.text = "0/0";
            }
            return;
        }

        UpdateExpDisplay();
    }

    private void UpdateLevelDisplay()
    {
        if (_levelText == null)
        {
            return;
        }

        if (PlayerStatManager.Instance == null)
        {
            return;
        }

        int level = PlayerStatManager.Instance.Level;
        _levelText.text = $"{level}";
    }

    private void UpdateExpDisplay()
    {
        if (_expText == null)
        {
            return;
        }

        if (PlayerStatManager.Instance == null)
        {
            return;
        }

        double currentExp = PlayerStatManager.Instance.CurrentExp;
        double maxExp = PlayerStatManager.Instance.MaxExp;
        _expText.text = $"{currentExp:F0}/{maxExp:F0}";
        _expSlider.value = (float)(currentExp / maxExp);
    }

    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += HandleUpgraded;
        }

        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.OnLevelUp += HandleLevelUp;
            PlayerStatManager.Instance.OnExpChanged += HandleExpChanged;
        }
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded -= HandleUpgraded;
        }

        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.OnLevelUp -= HandleLevelUp;
            PlayerStatManager.Instance.OnExpChanged -= HandleExpChanged;
        }
    }

    private void HandleUpgraded(string upgradeId, int level)
    {
        UpdateLevelDisplay();
    }

    private void HandleLevelUp(int newLevel)
    {
        UpdateLevelDisplay();
        UpdateExpDisplay();
    }

    private void HandleExpChanged(double currentExp, double maxExp)
    {
        UpdateExpDisplay();
    }
}
