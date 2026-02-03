using UnityEngine;

[System.Serializable]
public struct FloaterOption
{
    // ----------------------------------------------------------
    // [Header("설명")]을 쓰면 인스펙터에 글자가 바로 나옵니다.
    // ----------------------------------------------------------

    [Header("■ 기본 설정 --------------------------------")]
    [Header("숫자 표기 (None:1234, Comma:1,234, Korean:1억)")]
    public NumberFormat numberFormat;

    [Header("데미지 표시타입 Basic,Blade,Volcano,Blade2,Volcano2,Volcano3 ")]
    public EDamageStyle DamageStyle;

    [Header("지속 시간 (숫자가 화면에 떠있는 총 시간)")]
    public float singleNumberDuration; 

    [Header("등장 간격 (연타 시 다음 숫자 나올때까지 대기)")]
    public float delayBetweenNumbers;  

    [Header("줄 간격 (숫자가 위로 쌓일 때 간격)")]
    public float lineSpacing;          

    [Header("상승 거리 (등장 후 위로 흐르는 거리)")]
    public float driftDistance;        

    [Space(10)] // 여백 추가
    [Header("■ 크기 연출 --------------------------------")]
    [Header("시작 크기 (0:안보임, 0.5:반쯤 큼, 1:정상)")]
    public float startScale;           

    [Header("팝핑 크기 (펑! 하고 커질 때의 최대 크기)")]
    public float popScale;             

    [Header("정상 크기 (애니메이션 끝난 후 유지 크기)")]
    public float normalScale;          

    [Space(10)]
    [Header("■ 기타 설정 --------------------------------")]
    [Header("Blade2 피벗 (0.5, 0.5가 정중앙)")]
    public Vector2 blade2Pivot;

    // 기본값 (변경 없음)
    public static FloaterOption Default => new FloaterOption()
    {
        numberFormat = NumberFormat.None,
        singleNumberDuration = 1.0f,
        delayBetweenNumbers = 0.1f,
        lineSpacing = 0.7f,
        driftDistance = 1.0f,
        startScale = 0.5f,
        popScale = 1.5f,
        normalScale = 1.0f,
        blade2Pivot = new Vector2(0.5f, 0.5f)
    };
}