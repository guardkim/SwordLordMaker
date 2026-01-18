using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BossSpawnUI : MonoBehaviour, IPointerClickHandler
{
    [Header("▼ UI 참조")]
    [SerializeField] private Image _bossEnterButtonImage;
    [SerializeField] private TMPro.TextMeshProUGUI _buttonText;

    [Header("▼ 색상 설정")]
    [SerializeField] private Color _enabledColor = Color.white;
    [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool _interactable = true;

    private void Start()
    {
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
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnBossSpawned -= OnBossSpawned;
            StageManager.Instance.OnStageStarted -= OnStageStarted;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable)
        {
            return;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.SpawnBoss();
        }
        else
        {
            Debug.LogWarning("[BossSpawnUI] StageManager not found.");
        }
    }

    private void SetInteractable(bool interactable)
    {
        _interactable = interactable;

        if (_bossEnterButtonImage != null)
        {
            _bossEnterButtonImage.color = _interactable ? _enabledColor : _disabledColor;
        }
    }

    private void OnBossSpawned(EnemyAI boss)
    {
        SetInteractable(false);

        if (_buttonText != null)
        {
            _buttonText.text = "보스 전투 중";
        }
    }

    private void OnStageStarted(int stageId)
    {
        SetInteractable(true);

        if (_buttonText != null)
        {
            _buttonText.text = "보스입장";
        }
    }
}
