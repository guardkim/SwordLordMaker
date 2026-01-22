using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OfflineRewardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("▼ UI 참조")]
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private TextMeshProUGUI _offlineTimeText;
    [SerializeField] private TextMeshProUGUI _goldRewardText;
    [SerializeField] private TextMeshProUGUI _expRewardText;
    [SerializeField] private Image _claimButtonImage;

    private void Start()
    {
        if (_popupPanel != null)
        {
            _popupPanel.SetActive(false);
        }

        if (OfflineRewardManager.Instance != null)
        {
            OfflineRewardManager.Instance.OnOfflineRewardReady += OnOfflineRewardReady;

            if (OfflineRewardManager.Instance.HasPendingReward)
            {
                OnOfflineRewardReady(OfflineRewardManager.Instance.PendingReward);
            }
        }
    }

    private void OnDestroy()
    {
        if (OfflineRewardManager.HasInstance)
        {
            OfflineRewardManager.Instance.OnOfflineRewardReady -= OnOfflineRewardReady;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OfflineRewardManager.Instance != null)
        {
            OfflineRewardManager.Instance.ClaimReward();
        }

        HidePopup();
    }

    private void OnOfflineRewardReady(OfflineRewardResult reward)
    {
        if (reward == null) return;

        UpdateUI(reward);
        ShowPopup();
    }

    private void UpdateUI(OfflineRewardResult reward)
    {
        if (_offlineTimeText != null)
        {
            _offlineTimeText.text = $"오프라인 시간: {reward.GetFormattedDuration()}";
        }

        if (_goldRewardText != null)
        {
            _goldRewardText.text = FormatNumber(reward.GoldReward);
        }

        if (_expRewardText != null)
        {
            _expRewardText.text = FormatNumber(reward.ExpReward);
        }
    }

    private string FormatNumber(System.Numerics.BigInteger value)
    {
        if (value >= 1_000_000_000_000)
        {
            return $"{(double)value / 1_000_000_000_000:F1}T";
        }
        if (value >= 1_000_000_000)
        {
            return $"{(double)value / 1_000_000_000:F1}B";
        }
        if (value >= 1_000_000)
        {
            return $"{(double)value / 1_000_000:F1}M";
        }
        if (value >= 1_000)
        {
            return $"{(double)value / 1_000:F1}K";
        }
        return value.ToString();
    }

    private string FormatNumber(double value)
    {
        if (value >= 1_000_000_000_000)
        {
            return $"{value / 1_000_000_000_000:F1}T";
        }
        if (value >= 1_000_000_000)
        {
            return $"{value / 1_000_000_000:F1}B";
        }
        if (value >= 1_000_000)
        {
            return $"{value / 1_000_000:F1}M";
        }
        if (value >= 1_000)
        {
            return $"{value / 1_000:F1}K";
        }
        return value.ToString("F0");
    }

    private void ShowPopup()
    {
        if (_popupPanel != null)
        {
            _popupPanel.SetActive(true);
        }
    }

    private void HidePopup()
    {
        if (_popupPanel != null)
        {
            _popupPanel.SetActive(false);
        }
    }
}
