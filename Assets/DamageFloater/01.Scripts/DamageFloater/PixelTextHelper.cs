using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

[RequireComponent(typeof(TMP_Text))]
public class PixelTextHelper : MonoBehaviour
{
    private TMP_Text _tmp;

    //private const string CharMap = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?-.,:";
    private const string CharMap = "0123456789";

    [FormerlySerializedAs("useZigZag")] [Header("▼ 지그재그(ZigZag) 설정")]
    public bool UseZigZag = true; // 이 옵션을 켜면 지그재그 적용
    
    // 0.05는 폰트 크기에 따라 너무 미세할 수 있으니, 
    // 인스펙터에서 0.1 ~ 0.25 사이로 조절해보세요. (단위: em)
    public float ZigZagAmount = 0.1f;

    private void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
    }

    public void SetText(string text, TMP_SpriteAsset spriteAsset = null, string prefix = "")
    {
        if (!_tmp) _tmp = GetComponent<TMP_Text>();
        if (!_tmp) return;

        if (spriteAsset)
        {
            _tmp.spriteAsset = spriteAsset;
        }

        StringBuilder sb = new StringBuilder();

        // 접두어(Critical Hit 등)가 있으면 먼저 추가
        if (!string.IsNullOrEmpty(prefix))
        {
            sb.Append(prefix);
        }

        int charCount = 0; // 보여지는 글자 순서 카운트

        foreach (char c in text)
        {
            // 공백이나 태그 문자(<, >)는 지그재그 계산에서 제외
            if (c == ' ') { sb.Append(" "); continue; }
            if (c == '<' || c == '>') { sb.Append(c); continue; }

            // --- [지그재그 로직] ---
            if (UseZigZag)
            {
                // 짝수 인덱스(0, 2, 4...): 위로 (+)
                // 홀수 인덱스(1, 3, 5...): 아래로 (-)
                float offset = (charCount % 2 == 0) ? ZigZagAmount : -ZigZagAmount;

                sb.Append($"<voffset={offset}em>");
            }
            // ---------------------

            int index = CharMap.IndexOf(char.ToUpper(c));
            if (index != -1)
            {
                sb.Append($"<sprite={index}>");
            }
            else
            {
                sb.Append(c);
            }

            // --- [태그 닫기] ---
            if (UseZigZag)
            {
                sb.Append("</voffset>");
            }
            
            charCount++;
        }

        _tmp.text = sb.ToString();
    }

    // 테스트용 (에디터에서 값이 바뀔 때마다 실행)
    [TextArea] public string TestInput = "HELLO WORLD!";
    
    private void OnValidate()
    {
        // OnValidate는 컴포넌트가 준비되지 않았을 때도 불릴 수 있으므로 안전하게 호출
        SetText(TestInput);
    }
}