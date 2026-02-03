using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DamageFloater))]
public class DamageFloaterEditor : Editor
{
    private DamageFloater floater;
    private EDamageStyle previewStyle = EDamageStyle.Basic;
    
    // 에디터 애니메이션 관리 변수
    private double lastEditorTime;
    private bool isAnimating = false;

    private void OnEnable()
    {
        floater = (DamageFloater)target;
        EditorApplication.update += UpdateAnimation;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateAnimation;
    }

    public override void OnInspectorGUI()
    {
        // 1. 기본 인스펙터 그리기 (FloaterOption 포함)
        base.OnInspectorGUI();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("----- Editor Tools -----", EditorStyles.boldLabel);

        // 2. 매니저 연동 기능 (씬에 매니저가 있을 경우)
        DamageFloaterManager manager = FindObjectOfType<DamageFloaterManager>();
        if (manager != null)
        {
            EditorGUILayout.HelpBox("Manager found! You can load options directly.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load 'Single' Option"))
            {
                Undo.RecordObject(floater, "Load Single Option");
                floater.ApplyOption(manager.SingleFloaterOption);
                EditorUtility.SetDirty(floater);
                Debug.Log("[DamageFloater] Loaded Single Option from Manager.");
            }

            if (GUILayout.Button("Load 'Multi' Option"))
            {
                Undo.RecordObject(floater, "Load Multi Option");
                floater.ApplyOption(manager.MultiFloaterOption);
                EditorUtility.SetDirty(floater);
                Debug.Log("[DamageFloater] Loaded Multi Option from Manager.");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Add 'DamageFloaterManager' to the scene to load presets.", MessageType.None);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("----- Preview -----", EditorStyles.boldLabel);

        // 3. 미리보기 설정
        previewStyle = (EDamageStyle)EditorGUILayout.EnumPopup("Preview Style", previewStyle);

        // 텍스트 템플릿의 입력값 표시
        if (floater.TextTemplate != null)
        {
            var helper = floater.TextTemplate.GetComponent<PixelTextHelper>();
            if (helper != null)
            {
                EditorGUILayout.LabelField("Input Text:", helper.TestInput, EditorStyles.miniLabel);
            }
        }

        // 4. 미리보기 버튼
        if (GUILayout.Button("Play Preview", GUILayout.Height(30)))
        {
            PlayPreview();
        }
    }

    private void PlayPreview()
    {
        string inputText = "1234 5678";

        // 템플릿에서 테스트용 텍스트 가져오기
        if (floater.TextTemplate != null)
        {
            var helper = floater.TextTemplate.GetComponent<PixelTextHelper>();
            if (helper != null)
            {
                inputText = helper.TestInput;
            }
        }

        // DOTween 용량 확보 및 실행
        DG.Tweening.DOTween.SetTweensCapacity(500, 50);
        
        // 현재 인스펙터에 설정된 옵션(currentOption)을 사용하여 재생됨
        floater.ShowDamage(inputText, previewStyle);

        // 시간 초기화
        lastEditorTime = EditorApplication.timeSinceStartup;
        isAnimating = true;
    }

    private void UpdateAnimation()
    {
        // 애니메이션 중일 때만 수동 업데이트
        if (isAnimating && floater != null)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastEditorTime);
            lastEditorTime = currentTime;

            floater.Editor_ManualUpdate(deltaTime);
            SceneView.RepaintAll(); // 화면 갱신
        }
    }
}