using System.Collections.Generic;
using UnityEngine;
using TMPro; // UI 사용 시

public class ControllerManager : MonoBehaviour
{
    // ■ 1. 싱글톤 패턴 구현
    public static ControllerManager Instance { get; private set; }

    [Header("Controllers Assignment")]
    // Inspector에서 할당 (DIP 위반을 최소화하기 위해 인터페이스/추상클래스로 관리 가능하지만, 
    // 유니티 Inspector 편의성을 위해 구체 클래스를 필드로 두고 내부에서 추상화합니다)
    [SerializeField] private AdelFlyingSwordController _adelController;
    [SerializeField] private HypoSwordController _hypoController;
    [SerializeField] private PixelSwordController _pixelController;

    [Header("UI (Optional)")]
    public TextMeshProUGUI ModeText;

    // 내부 관리용 딕셔너리 (OCP: 새로운 검이 추가돼도 Dictionary에만 넣으면 됨)
    private Dictionary<SwordType, BaseSwordController> _controllers;
    
    // 현재 선택된 모드 기억
    private BaseSwordController _currentController;
    private SwordType _currentType;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지하고 싶다면 사용, 아니면 제거
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeControllers();
    }
    /// <summary>
    /// 테스트용 함수
    /// </summary>
    private void Update()
    {
        //TODO : Demo용 코드입니다. Manager 실 사용시에는 Update를 지워주세요
        if (ModeChange.Instance.CurrentType != EModeType.FlyingSword) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }
    private void InitializeControllers()
    {
        _controllers = new Dictionary<SwordType, BaseSwordController>
        {
            { SwordType.Adel, _adelController },
            { SwordType.Hypo, _hypoController },
            { SwordType.Pixel, _pixelController }
        };

        // 초기 상태: 모든 컨트롤러의 불필요한 연산 방지 (필요하다면)
        // 여기서는 BaseSwordController가 입력을 안 받으므로 굳이 enabled를 끌 필요는 없으나,
        // 확실한 상태 관리를 위해 StopSequence 호출 가능
        SwitchMode(SwordType.Adel);
    }

    // ■ 2. 외부에서 사용하는 API

    /// <summary>
    /// 특정 타입의 검을 즉시 발사합니다. 
    /// 사용 예: ControllerManager.Instance.Fire(SwordType.Pixel);
    /// </summary>
    public void Fire()
    {
        if(_currentController)
            _currentController.Fire();
    }

    /// <summary>
    /// 발사 없이 모드만 변경하고 싶을 때
    /// </summary>
    public void SetMode(int type)
    {
        SwitchMode((SwordType)type);
    }

    // ■ 3. 내부 로직

    private void SwitchMode(SwordType newType)
    {
        //if (_currentType == newType) return;

        // 2. 타입 변경
        _currentType = newType;

        // 3. ★ 컨트롤러 캐싱 (Dictionary 조회는 여기서만 수행)
        if (_controllers.TryGetValue(newType, out BaseSwordController newController))
        {
            _currentController = newController;
        }
        else
        {
            _currentController = null;
            Debug.LogError($"[Manager] {_currentType}에 해당하는 컨트롤러가 없습니다!");
        }

        _currentType = newType;
        UpdateUI(newType.ToString());
    }

    private void UpdateUI(string text)
    {
        if (ModeText != null)
        {
            ModeText.text = $"{text} Mode";
        }
    }
}