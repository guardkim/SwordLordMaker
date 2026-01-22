using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BossSpawnUI : MonoBehaviour, IPointerClickHandler
{
    [Header("▼ UI 참조")]
    [SerializeField] private Image _bossEnterButtonImage;
    [SerializeField] private TextMeshProUGUI _buttonText;

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
        if (StageManager.HasInstance)
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

        // [수정됨] 클릭 시 바로 소환하지 않고, 화면을 먼저 어둡게(FadeOut) 만듭니다.
        if (StageManager.Instance != null && FadeManager.Instance != null)
        {
            // 중복 클릭 방지
            SetInteractable(false); 

            // 1. 화면 어두워지기 시작
            FadeManager.Instance.FadeOut(() => 
            {
                // 2. 화면이 다 어두워진 뒤(콜백) 보스 소환 요청
                StageManager.Instance.SpawnBoss();
            });
        }
        else
        {
            Debug.LogWarning("[BossSpawnUI] StageManager not found.");
        }
    }

    private void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        if (_bossEnterButtonImage)
        {
            _bossEnterButtonImage.color = _interactable ? _enabledColor : _disabledColor;
        }
    }

    private void OnBossSpawned(EnemyAI boss)
    {
        if(boss != null) boss.enabled = false; 

        if (_buttonText != null) _buttonText.text = "보스 전투 중";
        
        FadeManager.Instance.FadeIn(() => 
        {
            if (boss != null)
            {
                boss.enabled = true; 
            }
        });
    }

    private void OnStageStarted(int stageId)
    {
        SetInteractable(true);

        if (_buttonText)
        {
            _buttonText.text = "보스입장";
        }
    }
}
