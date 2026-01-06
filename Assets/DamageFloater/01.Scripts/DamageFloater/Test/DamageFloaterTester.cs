using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamageFloaterTester : MonoBehaviour
{
    public static DamageStyle CurrentStyle;
     [Header("--- UI: Mode Control ---")]
    public Toggle SeparateToggle;        // 옵션 분리 체크박스
    public GameObject ModeButtonGroup;   // Single/Multi 버튼 그룹
    public Button BtnSingle;             // Single 선택 버튼
    public Button BtnMulti;              // Multi 선택 버튼
    public Image ImgSingleInfo;          // 버튼 색상 변경용
    public Image ImgMultiInfo;

    [Header("--- UI: Style Dropdown ---")]
    public TMP_Dropdown StyleDropdown;   // 데미지 스타일 선택

    [Header("--- UI: Sliders ---")]
    public Slider SlStartScale;
    public TextMeshProUGUI TxtStartScale;
    public Slider SlPopScale;  
    public TextMeshProUGUI TxtPopScale;
    public Slider SlNormalScale;  
    public TextMeshProUGUI TxtNormalScale;
    
    public Slider SlDuration;  
    public TextMeshProUGUI TxtDuration;
    public Slider SlDelay;     
    public TextMeshProUGUI TxtDelay;
    public Slider SlSpacing;    
    public TextMeshProUGUI TxtSpacing;
    public Slider SlDrift;     
    public TextMeshProUGUI TxtDrift;

    // 내부 상태 변수
    private bool _isSeparateMode; 
    private bool _isEditingSingle = true; 

    // 델리게이트 (값 변경 로직 재사용을 위해 정의)
    private delegate void OptionModifier(ref FloaterOption option, float value);

    private void Start()
    {
        // 1. 드롭다운 초기화
        InitializeDropdown();

        // 2. 이벤트 리스너 연결
        SeparateToggle.onValueChanged.AddListener(OnToggleSeparate);
        BtnSingle.onClick.AddListener(() => SetEditTarget(true));
        BtnMulti.onClick.AddListener(() => SetEditTarget(false));

        // 슬라이더 연결 (람다식 사용)
        SlStartScale.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.startScale = val, TxtStartScale));
        SlPopScale.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.popScale = val, TxtPopScale));
        SlNormalScale.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.normalScale = val, TxtNormalScale));
        
        SlDuration.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.singleNumberDuration = val, TxtDuration));
        SlDelay.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.delayBetweenNumbers = val, TxtDelay));
        SlSpacing.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.lineSpacing = val, TxtSpacing));
        SlDrift.onValueChanged.AddListener(v => ApplySliderChange(v, (ref FloaterOption o, float val) => o.driftDistance = val, TxtDrift));

        // 3. 초기 UI 동기화
        OnToggleSeparate(SeparateToggle.isOn);
        RefreshUIFromManager();
    }


    // ---------------------------------------------------------
    // ■ 드롭다운 (스타일) 로직
    // ---------------------------------------------------------
    private void InitializeDropdown()
    {
        if (StyleDropdown == null) return;

        StyleDropdown.ClearOptions();
        string[] styleNames = Enum.GetNames(typeof(DamageStyle));
        List<string> options = new List<string>(styleNames);
        StyleDropdown.AddOptions(options);

        StyleDropdown.onValueChanged.AddListener(OnStyleChanged);
    }

    private void OnStyleChanged(int index)
    {
        if (DamageFloaterManager.Instance == null) return;
        
        DamageStyle selectedStyle = (DamageStyle)index;
        CurrentStyle = selectedStyle;
        // [수정됨] Instance 직접 사용
        if (!_isSeparateMode || (_isSeparateMode && _isEditingSingle))
        {
            FloaterOption opt = DamageFloaterManager.Instance.SingleFloaterOption;
            opt.damageStyle = selectedStyle;
            DamageFloaterManager.Instance.SetSingleOption(opt);
        }

        if (!_isSeparateMode || (_isSeparateMode && !_isEditingSingle))
        {
            FloaterOption opt = DamageFloaterManager.Instance.MultiFloaterOption;
            opt.damageStyle = selectedStyle;
            DamageFloaterManager.Instance.SetMultiOption(opt);
        }
    }

    // ---------------------------------------------------------
    // ■ 슬라이더 값 변경 로직
    // ---------------------------------------------------------
    private void ApplySliderChange(float value, OptionModifier modifier, TextMeshProUGUI textLabel)
    {
        if (DamageFloaterManager.Instance == null) return;

        if (textLabel != null) textLabel.text = value.ToString("F2");

        // Single 옵션 적용
        if (!_isSeparateMode || (_isSeparateMode && _isEditingSingle))
        {
            FloaterOption opt = DamageFloaterManager.Instance.SingleFloaterOption;
            modifier(ref opt, value);
            DamageFloaterManager.Instance.SetSingleOption(opt);
        }

        // Multi 옵션 적용
        if (!_isSeparateMode || (_isSeparateMode && !_isEditingSingle))
        {
            FloaterOption opt = DamageFloaterManager.Instance.MultiFloaterOption;
            modifier(ref opt, value);
            DamageFloaterManager.Instance.SetMultiOption(opt);
        }
    }

    // ---------------------------------------------------------
    // ■ UI 모드 및 갱신 로직
    // ---------------------------------------------------------
    private void OnToggleSeparate(bool isOn)
    {
        _isSeparateMode = isOn;
        if (ModeButtonGroup != null) ModeButtonGroup.SetActive(isOn);

        if (!isOn)
        {
            _isEditingSingle = true; // 체크 해제 시 Single 기준으로 UI 통일
            RefreshUIFromManager();
        }
        else
        {
            UpdateButtonColors();
            RefreshUIFromManager();
        }
    }

    private void SetEditTarget(bool isSingle)
    {
        _isEditingSingle = isSingle;
        UpdateButtonColors();
        RefreshUIFromManager();
    }

    private void UpdateButtonColors()
    {
        if (ImgSingleInfo) ImgSingleInfo.color = _isEditingSingle ? Color.green : Color.white;
        if (ImgMultiInfo) ImgMultiInfo.color = !_isEditingSingle ? Color.green : Color.white;
    }

    // 매니저 값 -> UI로 반영 (슬라이더 및 드롭다운 위치 동기화)
    private void RefreshUIFromManager()
    {
        if (DamageFloaterManager.Instance == null) return;

        // [수정됨] Instance 직접 사용
        FloaterOption targetOpt = _isEditingSingle ? 
            DamageFloaterManager.Instance.SingleFloaterOption : 
            DamageFloaterManager.Instance.MultiFloaterOption;

        // 1. 슬라이더 동기화 (SetValueWithoutNotify로 이벤트 루프 방지)
        if (SlStartScale) SlStartScale.SetValueWithoutNotify(targetOpt.startScale);
        if (SlPopScale) SlPopScale.SetValueWithoutNotify(targetOpt.popScale);
        if (SlNormalScale) SlNormalScale.SetValueWithoutNotify(targetOpt.normalScale);
        if (SlDuration) SlDuration.SetValueWithoutNotify(targetOpt.singleNumberDuration);
        if (SlDelay) SlDelay.SetValueWithoutNotify(targetOpt.delayBetweenNumbers);
        if (SlSpacing) SlSpacing.SetValueWithoutNotify(targetOpt.lineSpacing);
        if (SlDrift) SlDrift.SetValueWithoutNotify(targetOpt.driftDistance);

        // 2. 텍스트 동기화
        UpdateText(TxtStartScale, targetOpt.startScale);
        UpdateText(TxtPopScale, targetOpt.popScale);
        UpdateText(TxtNormalScale, targetOpt.normalScale);
        UpdateText(TxtDuration, targetOpt.singleNumberDuration);
        UpdateText(TxtDelay, targetOpt.delayBetweenNumbers);
        UpdateText(TxtSpacing, targetOpt.lineSpacing);
        UpdateText(TxtDrift, targetOpt.driftDistance);

        // 3. 드롭다운 동기화
        if (StyleDropdown != null)
        {
            StyleDropdown.SetValueWithoutNotify((int)targetOpt.damageStyle);
        }
    }

    private void UpdateText(TextMeshProUGUI txt, float val)
    {
        if(txt != null) txt.text = val.ToString("F2");
    }
}