using UnityEngine;
using UnityEngine.UI;

public class BossSpawnUI : MonoBehaviour
{
    [Header("▼ UI 참조")]
    [SerializeField] private Button _bossEnterButton;
    [SerializeField] private TMPro.TextMeshProUGUI _buttonText;

    private void Start()
    {
        if (_bossEnterButton == null)
        {
            Debug.LogError("[BossSpawnUI] Boss Enter Button is not assigned.");
            return;
        }

        _bossEnterButton.onClick.AddListener(OnBossEnterButtonClicked);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnBossSpawned += OnBossSpawned;
            StageManager.Instance.OnStageStarted += OnStageStarted;
        }
        else
        {
            Debug.LogWarning("[BossSpawnUI] StageManager not found.");
        }
    }

    private void OnDestroy()
    {
        if (_bossEnterButton != null)
        {
            _bossEnterButton.onClick.RemoveListener(OnBossEnterButtonClicked);
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnBossSpawned -= OnBossSpawned;
            StageManager.Instance.OnStageStarted -= OnStageStarted;
        }
    }

    private void OnBossEnterButtonClicked()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SpawnBoss();
        }
        else
        {
            Debug.LogWarning("[BossSpawnUI] StageManager not found.");
        }
    }

    private void OnBossSpawned(EnemyAI boss)
    {
        if (_bossEnterButton != null)
        {
            _bossEnterButton.interactable = false;
        }

        if (_buttonText != null)
        {
            _buttonText.text = "보스 전투 중";
        }
    }

    private void OnStageStarted(int stageId)
    {
        if (_bossEnterButton != null)
        {
            _bossEnterButton.interactable = true;
        }

        if (_buttonText != null)
        {
            _buttonText.text = "보스입장";
        }
    }
}
