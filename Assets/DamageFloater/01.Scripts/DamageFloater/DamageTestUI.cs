using UnityEngine;
using TMPro; // TextMeshPro UI 사용 필수
using System; // Enum 기능을 위해 필요
using System.Collections.Generic; // List 사용

public class DamageTestUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown styleDropdown; // 인스펙터에서 연결할 Dropdown

    // 현재 선택된 스타일을 저장하는 변수
    private DamageStyle currentStyle;

    private void Start()
    {
        InitDropdown();
    }

    // Dropdown 초기화 및 Enum 연동 로직
    private void InitDropdown()
    {
        if (styleDropdown == null) return;

        // 1. 기존 옵션(Item A, Item B...) 초기화
        styleDropdown.ClearOptions();

        // 2. DamageStyle Enum의 모든 이름을 문자열 배열로 가져옴
        string[] enumNames = Enum.GetNames(typeof(DamageStyle));

        // 3. Dropdown에 넣을 수 있게 List<string>으로 변환하여 추가
        List<string> options = new List<string>(enumNames);
        styleDropdown.AddOptions(options);

        // 4. 값이 변경될 때 실행될 함수 연결
        styleDropdown.onValueChanged.AddListener(OnStyleChanged);

        // 5. 초기값 설정 (현재 선택된 0번 인덱스 반영)
        OnStyleChanged(styleDropdown.value);
    }

    // Dropdown 값이 바뀔 때 호출되는 함수
    private void OnStyleChanged(int index)
    {
        // Dropdown의 index(0, 1, 2...)를 DamageStyle Enum으로 형변환
        currentStyle = (DamageStyle)index;
        
        Debug.Log($"[UI] 스타일 변경됨: {currentStyle}");
    }

    // ---------------------------------------------------------
    // 버튼(Button)에 연결하여 테스트할 함수
    // ---------------------------------------------------------
    public void OnClick_SpawnTest()
    {
        // 랜덤 데미지 생성
        int damage = UnityEngine.Random.Range(1000, 99999);
        
        // Manager를 통해 데미지 출력 (현재 선택된 currentStyle 사용)
        // 위치는 (0,0,0) 혹은 원하는 위치
        DamageFloaterManager.Instance.ShowDamage(currentStyle, damage, Vector3.zero);
    }

    public void OnClick_SpawnComboTest()
    {
        List<int> damages = new List<int> { 123, 456, 789, 1000, 5000 };

        // Manager를 통해 연타 데미지 출력
        DamageFloaterManager.Instance.ShowDamage(currentStyle, damages, Vector3.zero);
    }
}