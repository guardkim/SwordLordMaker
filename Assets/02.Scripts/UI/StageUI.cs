using TMPro;
using UnityEngine;

public class StageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _stageText;
    [SerializeField] private TextMeshProUGUI _stageNameText;

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageStarted += UpdateStageText;

            if (!string.IsNullOrEmpty(StageManager.Instance.CurrentStageName))
            {
                UpdateStageText(StageManager.Instance.CurrentStageId);
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
        if (StageManager.Instance == null)
        {
            return;
        }

        string fullName = StageManager.Instance.CurrentStageName;
        ParseStageName(fullName, out string stageNumber, out string stageName);

        if (_stageText != null)
        {
            _stageText.text = stageNumber;
        }

        if (_stageNameText != null)
        {
            _stageNameText.text = stageName;
        }
    }

    private void ParseStageName(string fullName, out string stageNumber, out string stageName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            stageNumber = "";
            stageName = "";
            return;
        }

        int spaceIndex = fullName.IndexOf(' ');
        if (spaceIndex > 0)
        {
            stageNumber = fullName.Substring(0, spaceIndex);
            stageName = fullName.Substring(spaceIndex + 1);
        }
        else
        {
            stageNumber = fullName;
            stageName = "";
        }
    }
}
