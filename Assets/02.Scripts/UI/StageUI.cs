using TMPro;
using UnityEngine;

public class StageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _stageText;

    private void Start()
    {
        // 이벤트 구독
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += UpdateStageText;

            // 이미 스테이지가 시작된 경우 즉시 업데이트
            if (!string.IsNullOrEmpty(StageManager.Instance.CurrentStageName))
            {
                _stageText.text = StageManager.Instance.CurrentStageName;
            }
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted -= UpdateStageText;
        }
    }

    private void UpdateStageText(int stageId)
    {
        if (_stageText != null && StageManager.Instance != null)
        {
            _stageText.text = StageManager.Instance.CurrentStageName;
        }
    }
}
