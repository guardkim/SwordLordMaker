using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Serialization;


public class DamageFloater : MonoBehaviour
{
    [Header("▼ 렌더링 설정")]
    public string SortingLayerName = "DamageUI"; // 유니티에서 설정한 Sorting Layer 이름 입력
    
    [Header("▼ 참조 객체")]
    public GameObject TextTemplate;

    // [추가됨] 폰트 및 크리티컬 설정
    [Header("▼ 폰트 설정 (Sprite Assets)")]
    public TMP_SpriteAsset NonCritFont;     // 일반 데미지용 에셋
    public TMP_SpriteAsset CritFont;        // 크리티컬 데미지용 에셋
    
    [Header("▼ 현재 적용된 옵션 (Current Option)")]
    public FloaterOption CurrentOption = FloaterOption.Default;

    [FormerlySerializedAs("critHeaderString")]
    [Header("▼ 크리티컬 설정")]
    [Tooltip("크리티컬일 때 숫자 앞에 붙을 태그 (예: <sprite name=\"Critical\">)")]
    public string CritHeaderString = "<sprite name=\"Critical\">";
    
    private readonly List<GameObject> _activeTexts = new List<GameObject>();
    private Sequence _mySequence;
    private int[] _currentDamages;

    // Z-Order 문제 해결을 위한 전역 변수
    private static int _globalSortingOrder = 1000;

    // 빌보드 처리용
    private Transform _cameraTransform;

    private void Awake()
    {
        if (TextTemplate != null) TextTemplate.SetActive(false);
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            transform.rotation = _cameraTransform.rotation;
        }
    }

    private void OnDestroy()
    {
        _mySequence?.Kill();
    }

    public void ApplyOption(FloaterOption option)
    {
        this.CurrentOption = option;
    }

    // [수정됨] isCrit 매개변수 추가 (기본값 false)
    public void ShowDamage(string damageString, DamageStyle style, bool isCrit = false)
    {
        if (string.IsNullOrEmpty(damageString)) return;

        List<int> damageList = new List<int>();
        string[] parts = damageString.Split(' ');

        foreach (var part in parts)
        {
            if (int.TryParse(part, out int result)) damageList.Add(result);
        }

        if (damageList.Count > 0)
        {
            ShowDamage(damageList.ToArray(), style, isCrit);
        }
    }

    // [수정됨] 배열 버전도 isCrit 추가
    public void ShowDamage(int[] damages, DamageStyle style, bool isCrit = false)
    {
        _currentDamages = damages;
        InitTexts(damages, style, isCrit); // InitTexts로 전달
        PlayAnimation(style);
    }

    // BigInteger용 이미 포맷된 문자열 표시 (축약 표기: 1.5A, 999B 등)
    public void ShowFormattedDamage(string formattedText, DamageStyle style, bool isCrit = false)
    {
        _currentDamages = new[] { 0 };
        InitFormattedText(formattedText, style, isCrit);
        PlayAnimation(style);
    }

    // ----------------------------------------------------------------
    // 초기화 및 텍스트 생성
    // ----------------------------------------------------------------

    // BigInteger 포맷된 문자열용 초기화
    private void InitFormattedText(string formattedText, DamageStyle style, bool isCrit)
    {
        ClearTexts();

        _globalSortingOrder += 20;
        int currentBaseOrder = _globalSortingOrder;

        TMP_SpriteAsset targetFont = isCrit ? CritFont : NonCritFont;
        string prefix = isCrit ? CritHeaderString : "";

        GameObject newObj = Instantiate(TextTemplate, transform);
        newObj.SetActive(true);
        newObj.transform.SetAsLastSibling();

        var rend = newObj.GetComponent<Renderer>();
        if (rend) rend.sortingOrder = currentBaseOrder;

        var rect = newObj.GetComponent<RectTransform>();
        if (rect) rect.pivot = new Vector2(0.5f, 0.5f);

        var tmp = newObj.GetComponent<TMP_Text>();
        if (tmp)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.alpha = 0;
        }

        var helper = newObj.GetComponent<PixelTextHelper>();
        if (helper)
        {
            helper.SetText(formattedText, targetFont, prefix);
        }

        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localScale = Vector3.zero;

        _activeTexts.Add(newObj);
    }

    // [수정됨] 폰트 적용 로직 추가
    private void InitTexts(int[] damages, DamageStyle style, bool isCrit)
    {
        ClearTexts();

        _globalSortingOrder += 20; 
        int currentBaseOrder = _globalSortingOrder;

        int spawnCount = (style == DamageStyle.Volcano2) ? 1 : damages.Length;

        // 사용할 폰트와 접두어 결정
        TMP_SpriteAsset targetFont = isCrit ? CritFont : NonCritFont;
        string prefix = isCrit ? CritHeaderString : "";

        for (int I = 0; I < spawnCount; I++)
        {
            GameObject newObj = Instantiate(TextTemplate, transform);
            newObj.SetActive(true);
            newObj.transform.SetAsLastSibling();

            var rend = newObj.GetComponent<Renderer>();
            if (rend) rend.sortingOrder = currentBaseOrder + I;

            var rect = newObj.GetComponent<RectTransform>();
            if (rect) rect.pivot = new Vector2(0.5f, 0.5f);
            
            var tmp = newObj.GetComponent<TMP_Text>();
            if (tmp)
            {
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.alpha = 0; 
            }

            // [핵심] Helper에 폰트와 접두어 정보 전달
            var helper = newObj.GetComponent<PixelTextHelper>();
            if (helper) 
            {
               
                helper.SetText(GetFormattedDamage(damages[I]), targetFont, prefix);
            }

            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localScale = Vector3.zero;
            
            _activeTexts.Add(newObj);
        }
    }

    private string GetFormattedDamage(int damage)
    {
        switch (CurrentOption.numberFormat)
        {
            case NumberFormat.Comma: return damage.ToString("N0");
            case NumberFormat.Korean: return FormatKorean(damage);
            default: return damage.ToString();
        }
    }

    private string FormatKorean(long number)
    {
        if (number == 0) return "0";
        string[] units = { "", "만", "억", "조" };
        StringBuilder sb = new StringBuilder();
        int unitIndex = 0;
        while (number > 0)
        {
            long part = number % 10000;
            if (part > 0) sb.Insert(0, part.ToString() + units[unitIndex]);
            number /= 10000;
            unitIndex++;
        }
        return sb.ToString();
    }

    private void ClearTexts()
    {
        foreach (var obj in _activeTexts)
        {
            if (obj && obj != TextTemplate)
            {
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
        }
        _activeTexts.Clear();
    }

    // ----------------------------------------------------------------
    // 애니메이션 재생
    // ----------------------------------------------------------------
    private void PlayAnimation(DamageStyle style)
    {
        _mySequence?.Kill();
        _mySequence = DOTween.Sequence();

        switch (style)
        {
            case DamageStyle.Basic: AnimateBasic(); break;
            case DamageStyle.Blade: AnimateBlade(); break; 
            case DamageStyle.Volcano: AnimateVolcano(); break;
            case DamageStyle.Blade2: AnimateBlade(); break; // Blade1과 동일 로직
            case DamageStyle.Volcano2: AnimateVolcano2(); break;
            case DamageStyle.Volcano3: AnimateVolcano3(); break;
            case DamageStyle.Volcano4: AnimateVolcano4(); break;
        }

        if (Application.isPlaying)
        {
            _mySequence.OnComplete(() => Destroy(gameObject));
        }
    }

    private void AnimateBasic()
    {
        for (int I = 0; I < _activeTexts.Count; I++)
        {
            Transform T = _activeTexts[I].transform;
            TMP_Text tmp = T.GetComponent<TMP_Text>();

            float startY = I * CurrentOption.lineSpacing;
            float zPos = -I * 0.01f; 

            T.localPosition = new Vector3(0, startY, zPos);
            T.localScale = Vector3.one * CurrentOption.normalScale;
            
            if (tmp) tmp.alpha = 0;

            float startTime = I * CurrentOption.delayBetweenNumbers;

            if (tmp) _mySequence.Insert(startTime, tmp.DOFade(1, 0.1f));
            _mySequence.Insert(startTime, T.DOLocalMoveY(startY + CurrentOption.driftDistance, CurrentOption.singleNumberDuration).SetEase(Ease.Linear));

            if (!tmp) continue;
            float fadeOutStart = startTime + CurrentOption.singleNumberDuration - 0.3f;
            _mySequence.Insert(fadeOutStart, tmp.DOFade(0, 0.3f));
        }
    }

    private void AnimateBlade()
    {
        for (int I = 0; I < _activeTexts.Count; I++)
        {
            Transform T = _activeTexts[I].transform;
            TMP_Text tmp = T.GetComponent<TMP_Text>();

            float zPos = -I * 0.01f;
            T.localPosition = new Vector3(0, 0, zPos);
            T.localScale = Vector3.zero; 
            if (tmp) tmp.alpha = 1;

            float startTime = I * CurrentOption.delayBetweenNumbers;

            _mySequence.InsertCallback(startTime, () => {
                T.localScale = Vector3.one * CurrentOption.startScale;
            });

            _mySequence.Insert(startTime, T.DOScale(CurrentOption.popScale, 0.2f).SetEase(Ease.OutBack));
            _mySequence.Insert(startTime + 0.2f, T.DOScale(CurrentOption.normalScale, 0.1f));
            _mySequence.Insert(startTime, T.DOLocalMoveY(CurrentOption.driftDistance, CurrentOption.singleNumberDuration).SetEase(Ease.Linear));

            if (tmp)
            {
                float fadeOutStart = startTime + CurrentOption.singleNumberDuration - 0.3f;
                _mySequence.Insert(fadeOutStart, tmp.DOFade(0, 0.3f));
            }
        }
    }

    private void AnimateVolcano()
    {
        for (int I = 0; I < _activeTexts.Count; I++)
        {
            Transform T = _activeTexts[I].transform;
            TMP_Text tmp = T.GetComponent<TMP_Text>();

            float startY = I * CurrentOption.lineSpacing;
            float zPos = -I * 0.01f;

            T.localPosition = new Vector3(0, startY, zPos);
            T.localScale = Vector3.zero;
            if (tmp) tmp.alpha = 1;

            float startTime = I * CurrentOption.delayBetweenNumbers;

            _mySequence.InsertCallback(startTime, () => {
                T.localScale = Vector3.one * CurrentOption.startScale;
            });

            _mySequence.Insert(startTime, T.DOScale(CurrentOption.popScale, 0.2f).SetEase(Ease.OutBack));
            _mySequence.Insert(startTime + 0.2f, T.DOScale(CurrentOption.normalScale, 0.1f));
            _mySequence.Insert(startTime, T.DOLocalMoveY(startY + CurrentOption.driftDistance, CurrentOption.singleNumberDuration).SetEase(Ease.Linear));

            if (tmp)
            {
                float fadeOutStart = startTime + CurrentOption.singleNumberDuration - 0.3f;
                _mySequence.Insert(fadeOutStart, tmp.DOFade(0, 0.3f));
            }
        }
    }

    private void AnimateVolcano2()
    {
        // Volcano2: 단일 객체 텍스트 업데이트 방식 (자동으로 이전 숫자 사라짐)
        if (_activeTexts.Count == 0 || _currentDamages == null) return;

        GameObject obj = _activeTexts[0];
        Transform T = obj.transform;
        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        PixelTextHelper helper = obj.GetComponent<PixelTextHelper>();

        T.localPosition = Vector3.zero;
        T.localScale = Vector3.one * CurrentOption.startScale;
        if (tmp) tmp.alpha = 1;

        int count = _currentDamages.Length;
        float stepTime = CurrentOption.delayBetweenNumbers;
        float countingPhaseDuration = count * stepTime;

        _mySequence.Insert(0, T.DOLocalMoveY(CurrentOption.driftDistance, countingPhaseDuration).SetEase(Ease.Linear));

        for (int I = 0; I < count; I++)
        {
            float time = I * stepTime;
            int dmg = _currentDamages[I];

            _mySequence.InsertCallback(time, () => {
                if(helper) helper.SetText(GetFormattedDamage(dmg));
            });

            _mySequence.Insert(time, T.DOScale(CurrentOption.popScale, 0.1f).SetEase(Ease.OutBack));
            _mySequence.Insert(time + 0.1f, T.DOScale(CurrentOption.normalScale, 0.1f));
        }

        float finishStartTime = countingPhaseDuration;
        float finishDuration = 0.6f;
        float directionX = (Random.value > 0.5f) ? 1.0f : -1.0f;
        Vector3 finalOffset = new Vector3(directionX * 1.5f, 1.5f, 0);

        _mySequence.Insert(finishStartTime, T.DOLocalMove(finalOffset, finishDuration).SetRelative().SetEase(Ease.OutQuad));
        _mySequence.Insert(finishStartTime, T.DOScale(CurrentOption.normalScale * 0.8f, finishDuration));

        if (tmp)
        {
            _mySequence.Insert(finishStartTime, tmp.DOFade(0, finishDuration));
        }
    }

    private void AnimateVolcano3()
    {
        // Volcano3: 여러 객체가 차례로 등장하며 이전 숫자는 사라짐
        float lastFinishTime = 0f;

        for (int I = 0; I < _activeTexts.Count; I++)
        {
            Transform T = _activeTexts[I].transform;
            TMP_Text tmp = T.GetComponent<TMP_Text>();
            
            // 1. 초기 위치 및 상태 설정
            float startY = I * CurrentOption.lineSpacing; // 위로 조금씩 쌓이는 위치 (필요 없으면 0으로 고정)
            float zPos = -I * 0.01f; // 겹침 방지

            T.localPosition = new Vector3(0, startY, zPos);
            T.localScale = Vector3.zero; // 처음엔 안 보임
            if (tmp != null) tmp.alpha = 1;

            // 2. 등장 타이밍 계산
            float startTime = I * CurrentOption.delayBetweenNumbers;

            // [복구됨] 이전 인덱스 끄기 로직 ---------------------------------------
            if (I > 0)
            {
                // 바로 직전의 숫자 가져오기
                var prevT = _activeTexts[I - 1].transform;
                var prevTmp = _activeTexts[I - 1].GetComponent<TMP_Text>();

                // 현재 숫자(i)가 등장하는 시간(startTime)에 
                // 이전 숫자(i-1)는 0.1초 동안 빠르게 작아지며 사라짐
                _mySequence.Insert(startTime, prevT.DOScale(0, 0.1f));
                
                if (prevTmp)
                {
                    _mySequence.Insert(startTime, prevTmp.DOFade(0, 0.1f));
                }
            }
            // ------------------------------------------------------------------

            // 3. 현재 숫자 등장 애니메이션
            // 등장 시작 시점에 크기 0에서 시작하도록 확실히 콜백 지정
            _mySequence.InsertCallback(startTime, () => {
                T.localScale = Vector3.zero; 
            });

            // 팝핑 (커졌다가 정상 크기로)
            _mySequence.Insert(startTime, T.DOScale(CurrentOption.popScale, 0.1f).SetEase(Ease.OutBack));
            _mySequence.Insert(startTime + 0.1f, T.DOScale(CurrentOption.normalScale, 0.1f));

            // 위로 흐르기
            float driftDuration = (I == _activeTexts.Count - 1) ? 0.5f : CurrentOption.singleNumberDuration;
            _mySequence.Insert(startTime, T.DOLocalMoveY(startY + CurrentOption.driftDistance, driftDuration).SetEase(Ease.Linear));
            
            // 마지막 숫자 끝나는 시간 기록
            if (I == _activeTexts.Count - 1)
            {
                lastFinishTime = startTime + 0.2f;
            }
        }

        // 4. 마지막 숫자는 특별한 마무리 연출 (날아가며 사라짐)
        if (_activeTexts.Count > 0)
        {
            GameObject lastObj = _activeTexts[_activeTexts.Count - 1];
            Transform lastT = lastObj.transform;
            TMP_Text lastTmp = lastObj.GetComponent<TMP_Text>();

            float flyStartTime = lastFinishTime; 
            float flyDuration = 0.6f;

            // 랜덤 방향으로 튕겨 나감
            float directionX = (Random.value > 0.5f) ? 1.0f : -1.0f;
            Vector3 finalOffset = new Vector3(directionX * 1.5f, 1.5f, 0);

            _mySequence.Insert(flyStartTime, lastT.DOLocalMove(finalOffset, flyDuration).SetRelative().SetEase(Ease.OutQuad));
            _mySequence.Insert(flyStartTime, lastT.DOScale(CurrentOption.normalScale * 0.8f, flyDuration)); // 약간 작아짐
            
            if (lastTmp)
            {
                _mySequence.Insert(flyStartTime, lastTmp.DOFade(0, flyDuration));
            }
        }
    }
    private void AnimateVolcano4()
    {
        for (int I = 0; I < _activeTexts.Count; I++)
        {
            var T = _activeTexts[I].transform;
            var tmp = T.GetComponent<TMP_Text>();

            float startY = I * CurrentOption.lineSpacing;
            float zPos = -I * 0.01f;

            T.localPosition = new Vector3(0, startY, zPos);
            T.localScale = Vector3.zero;
            if (tmp) tmp.alpha = 1;

            float startTime = I * CurrentOption.delayBetweenNumbers;

            _mySequence.InsertCallback(startTime, () => {
                T.localScale = Vector3.one * CurrentOption.startScale;
            });

            _mySequence.Insert(startTime, T.DOScale(CurrentOption.popScale, 0.1f).SetEase(Ease.OutBack));
            _mySequence.Insert(startTime + 0.1f, T.DOScale(CurrentOption.normalScale, 0.1f));
            _mySequence.Insert(startTime, T.DOLocalMoveY(startY + CurrentOption.driftDistance, CurrentOption.singleNumberDuration).SetEase(Ease.Linear));

            if (tmp != null)
            {
                float fadeOutStart = startTime + CurrentOption.singleNumberDuration - 0.3f;
                _mySequence.Insert(fadeOutStart, tmp.DOFade(0, 0.3f));
            }
        }
    }
#if UNITY_EDITOR
    public void Editor_ManualUpdate(float deltaTime)
    {
        if (_mySequence != null && _mySequence.IsActive())
        {
            _mySequence.ManualUpdate(deltaTime, deltaTime);
        }
    }
#endif
}