using System;
using UnityEngine;
using UnityEngine.UI;


// TODO : Demo 용 클래스와 Enum입니다, 실 사용시 삭제해주세요
public enum EModeType
{
    DamageFloater,
    FlyingSword,
    Count
}
public class ModeChange : MonoBehaviour
{
    public static ModeChange Instance;
    public EModeType CurrentType => _currentType; 
    private EModeType _currentType;
    private Toggle _toggle;

    private void Start()
    {
        if (Instance == null) Instance = this;
        _toggle = GetComponent<Toggle>();
        
    }

    public void ChangeMode()
    {
        if (_toggle.isOn) _currentType = EModeType.FlyingSword;
        else _currentType = EModeType.DamageFloater;
    }
}
