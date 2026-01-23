using System.Collections;
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
        Debug.Log($"[OfflineRewardUI] Start() 호출됨, _popupPanel: {(_popupPanel != null ? "있음" : "NULL")}");

        if (_popupPanel != null)
        {
            _popupPanel.SetActive(false);
        }

        if (OfflineRewardManager.Instance != null)
        {
            Debug.Log($"[OfflineRewardUI] 이벤트 구독, HasPendingReward: {OfflineRewardManager.Instance.HasPendingReward}");
            OfflineRewardManager.Instance.OnOfflineRewardReady += OnOfflineRewardReady;

            if (OfflineRewardManager.Instance.HasPendingReward)
            {
                Debug.Log("[OfflineRewardUI] Start에서 즉시 팝업 표시 시도");
                OnOfflineRewardReady(OfflineRewardManager.Instance.PendingReward);
            }
        }
        else
        {
            Debug.LogWarning("[OfflineRewardUI] OfflineRewardManager.Instance가 NULL!");
        }

        StartCoroutine(DelayedPendingCheck());
    }

    private IEnumerator DelayedPendingCheck()
    {
        yield return new WaitForSeconds(0.5f);

        if (OfflineRewardManager.Instance != null &&
            OfflineRewardManager.Instance.HasPendingReward &&
            _popupPanel != null && !_popupPanel.activeSelf)
        {
            Debug.Log("[OfflineRewardUI] 지연 체크로 팝업 표시");
            OnOfflineRewardReady(OfflineRewardManager.Instance.PendingReward);
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
        Debug.Log($"[OfflineRewardUI] OnOfflineRewardReady 호출됨, reward: {(reward != null ? "있음" : "NULL")}");

        if (reward == null) return;

        UpdateUI(reward);
        ShowPopup();
    }

    private void UpdateUI(OfflineRewardResult reward)
    {
        if (_offlineTimeText != null)
        {
            _offlineTimeText.text = $"오프라인 시간 \n {reward.GetFormattedDuration()}";
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
        Debug.Log($"[OfflineRewardUI] ShowPopup() 호출됨, _popupPanel: {(_popupPanel != null ? _popupPanel.name : "NULL")}");

        if (_popupPanel != null)
        {
            _popupPanel.SetActive(true);
            Debug.Log($"[OfflineRewardUI] 팝업 활성화 완료, activeSelf: {_popupPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[OfflineRewardUI] _popupPanel이 NULL이라 팝업을 표시할 수 없음!");
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
