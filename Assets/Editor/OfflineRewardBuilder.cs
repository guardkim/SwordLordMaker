using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================
// Offline Reward UI Builder
// ============================================
// LayerLab GUI Pro 에셋을 사용하여 오프라인 리워드 팝업 UI 생성
// OfflineRewardUI 컴포넌트의 SerializeField 필드 자동 연결
// ============================================

public class OfflineRewardBuilder
{
    // ============================================
    // LAYERLAB GUI PRO 에셋 경로
    // ============================================
    // 주의: 에셋 경로를 확인해주세요. 에셋이 설치되지 않은 경우 기본 UI가 생성됩니다.

    private const string POPUP_PREFAB_PATH = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_01_Basic.prefab";
    private const string CLAIM_BUTTON_PREFAB_PATH = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_01_Green.prefab";
    private const string GOLD_ITEM_FRAME_PATH = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame_01.prefab";
    private const string EXP_ITEM_FRAME_PATH = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame_01.prefab";

    // ============================================
    // 아이콘 이미지 경로
    // ============================================

    private const string GOLD_ICON_PATH = "Assets/04.Images/Icon/Economy_Coin_02_Gold.png";
    private const string EXP_ICON_PATH = "Assets/Freeui/ZOSMA/Main/EXP.png";

    // ============================================
    // GAMEOBJECT 이름 상수
    // ============================================

    private const string ROOT_OBJECT_NAME = "Offline_Reward_Popup";
    private const string POPUP_PANEL_NAME = "PopupPanel";
    private const string TITLE_PANEL_NAME = "TitlePanel";
    private const string TITLE_TEXT_NAME = "TitleText";
    private const string CONTENT_AREA_NAME = "ContentArea";
    private const string OFFLINE_TIME_TEXT_NAME = "OfflineTimeText";
    private const string GOLD_REWARD_GROUP_NAME = "GoldRewardGroup";
    private const string GOLD_REWARD_TEXT_NAME = "GoldRewardText";
    private const string GOLD_REWARD_TEXT_BG_NAME = "GoldRewardTextBackground";
    private const string EXP_REWARD_GROUP_NAME = "ExpRewardGroup";
    private const string EXP_REWARD_TEXT_NAME = "ExpRewardText";
    private const string EXP_REWARD_TEXT_BG_NAME = "ExpRewardTextBackground";
    private const string BUTTONS_AREA_NAME = "ButtonsArea";
    private const string CLAIM_BUTTON_NAME = "ClaimButton";

    // ============================================
    // MAIN 메서드: BuildOfflineRewardPopup
    // ============================================

    [MenuItem("Tools/Build Offline Reward Popup")]
    public static void BuildOfflineRewardPopup()
    {
        // 1. Canvas 확인 및 생성
        Canvas canvas = FindOrCreateCanvas();

        // 2. 루트 GameObject 생성
        GameObject rootGO = CreateRootGameObject(canvas);

        // 3. OfflineRewardUI 컴포넌트 추가
        OfflineRewardUI uiComponent = rootGO.AddComponent<OfflineRewardUI>();

        // 4. SerializedObject로 필드 관리
        SerializedObject serializedObject = new SerializedObject(uiComponent);

        // 5. 팝업 패널 생성 및 연결
        GameObject popupPanel = CreatePopupPanel(rootGO);
        serializedObject.FindProperty("_popupPanel").objectReferenceValue = popupPanel;

        // 6. 타이틀 패널 생성
        CreateTitlePanel(popupPanel);

        // 7. 콘텐츠 영역 생성
        GameObject contentArea = CreateContentArea(popupPanel);

        // 8. 오프라인 시간 텍스트 생성 및 연결
        TextMeshProUGUI offlineTimeText = CreateOfflineTimeText(contentArea);
        serializedObject.FindProperty("_offlineTimeText").objectReferenceValue = offlineTimeText;

        // 9. 골드 보상 그룹 및 텍스트 생성 및 연결
        GameObject goldRewardGroup = CreateRewardGroup(contentArea, "Gold");
        TextMeshProUGUI goldRewardText = goldRewardGroup.transform.Find(GOLD_REWARD_TEXT_NAME).GetComponent<TextMeshProUGUI>();
        serializedObject.FindProperty("_goldRewardText").objectReferenceValue = goldRewardText;

        // 10. 경험치 보상 그룹 및 텍스트 생성 및 연결
        GameObject expRewardGroup = CreateRewardGroup(contentArea, "Exp");
        TextMeshProUGUI expRewardText = expRewardGroup.transform.Find(EXP_REWARD_TEXT_NAME).GetComponent<TextMeshProUGUI>();
        serializedObject.FindProperty("_expRewardText").objectReferenceValue = expRewardText;

        // 11. 버튼 영역 생성
        GameObject buttonsArea = CreateButtonsArea(popupPanel);

        // 12. Claim Button 생성 및 연결
        GameObject claimButtonGO = CreateClaimButton(buttonsArea);
        Button claimButton = claimButtonGO.GetComponent<Button>();
        serializedObject.FindProperty("_claimButton").objectReferenceValue = claimButton;

        // 13. 변경사항 적용
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(uiComponent);
        EditorUtility.SetDirty(rootGO);

        // 14. 완료 로그
        Debug.Log($"[OfflineRewardBuilder] 오프라인 리워드 팝업이 성공적으로 생성되었습니다!");
    }

    // ============================================
    // 메서드: FindOrCreateCanvas
    // ============================================

    private static Canvas FindOrCreateCanvas()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        Canvas canvas = null;

        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        return canvas;
    }

    // ============================================
    // 메서드: CreateRootGameObject
    // ============================================

    private static GameObject CreateRootGameObject(Canvas canvas)
    {
        GameObject rootGO = new GameObject(ROOT_OBJECT_NAME);
        rootGO.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = rootGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        Undo.RegisterCreatedObjectUndo(rootGO, "Create Offline Reward Popup Root");

        return rootGO;
    }

    // ============================================
    // 메서드: CreatePopupPanel
    // ============================================

    private static GameObject CreatePopupPanel(GameObject root)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(POPUP_PREFAB_PATH);

        GameObject popupPanel;

        if (prefab != null)
        {
            popupPanel = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            Debug.LogWarning($"[OfflineRewardBuilder] Popup prefab를 찾을 수 없습니다: {POPUP_PREFAB_PATH}");
            Debug.LogWarning("[OfflineRewardBuilder] 기본 패널을 생성합니다.");
            popupPanel = CreateDefaultPanel(root);
            popupPanel.name = POPUP_PANEL_NAME;
            return popupPanel;
        }

        popupPanel.name = POPUP_PANEL_NAME;
        popupPanel.transform.SetParent(root.transform, false);

        RectTransform rectTransform = popupPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.3f, 0.2f);
        rectTransform.anchorMax = new Vector2(0.7f, 0.8f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Undo.RegisterCreatedObjectUndo(popupPanel, "Create Popup Panel");

        return popupPanel;
    }

    // ============================================
    // 메서드: CreateTitlePanel
    // ============================================

    private static void CreateTitlePanel(GameObject parent)
    {
        GameObject titlePanel = new GameObject(TITLE_PANEL_NAME);
        titlePanel.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = titlePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(0, 60);

        Image panelImage = titlePanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        GameObject titleTextGO = new GameObject(TITLE_TEXT_NAME);
        titleTextGO.transform.SetParent(titlePanel.transform, false);

        RectTransform titleRect = titleTextGO.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI titleText = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "오프라인 보상";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        Undo.RegisterCreatedObjectUndo(titlePanel, "Create Title Panel");
    }

    // ============================================
    // 메서드: CreateDefaultPanel
    // ============================================

    private static GameObject CreateDefaultPanel(GameObject root)
    {
        GameObject panel = new GameObject(POPUP_PANEL_NAME);
        panel.transform.SetParent(root.transform, false);

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.3f, 0.2f);
        rectTransform.anchorMax = new Vector2(0.7f, 0.8f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        Undo.RegisterCreatedObjectUndo(panel, "Create Default Panel");

        return panel;
    }

    // ============================================
    // 메서드: CreateContentArea
    // ============================================

    private static GameObject CreateContentArea(GameObject parent)
    {
        GameObject contentArea = new GameObject(CONTENT_AREA_NAME);
        contentArea.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = contentArea.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = new Vector2(20, 80);
        rectTransform.offsetMax = new Vector2(-20, -80);

        VerticalLayoutGroup layoutGroup = contentArea.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 15f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter sizeFitter = contentArea.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Undo.RegisterCreatedObjectUndo(contentArea, "Create Content Area");

        return contentArea;
    }

    // ============================================
    // 메서드: CreateOfflineTimeText
    // ============================================

    private static TextMeshProUGUI CreateOfflineTimeText(GameObject parent)
    {
        GameObject textGO = new GameObject(OFFLINE_TIME_TEXT_NAME);
        textGO.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = textGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(0, 40);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "오프라인 시간: 00:00:00";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        Undo.RegisterCreatedObjectUndo(textGO, "Create Offline Time Text");

        return text;
    }

    // ============================================
    // 메서드: CreateRewardGroup
    // ============================================

    private static GameObject CreateRewardGroup(GameObject parent, string rewardType)
    {
        string groupName = rewardType == "Gold" ? GOLD_REWARD_GROUP_NAME : EXP_REWARD_GROUP_NAME;
        GameObject groupGO = new GameObject(groupName);
        groupGO.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = groupGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup layoutGroup = groupGO.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(20, 20, 5, 5);

        // 아이콘 프레임 생성 (이미지 설정)
        CreateItemFrame(groupGO, rewardType);

        // 보상 텍스트 배경 GameObject 생성
        string textBgName = rewardType == "Gold" ? GOLD_REWARD_TEXT_BG_NAME : EXP_REWARD_TEXT_BG_NAME;
        GameObject textBgGO = new GameObject(textBgName);
        textBgGO.transform.SetParent(groupGO.transform, false);

        RectTransform textBgRect = textBgGO.AddComponent<RectTransform>();
        textBgRect.sizeDelta = new Vector2(0, 50);

        Image textBgImage = textBgGO.AddComponent<Image>();
        textBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // 보상 텍스트 GameObject 생성 (배경 자식으로)
        string textName = rewardType == "Gold" ? GOLD_REWARD_TEXT_NAME : EXP_REWARD_TEXT_NAME;
        GameObject textGO = new GameObject(textName);
        textGO.transform.SetParent(textBgGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(0, -10);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "0";
        text.fontSize = 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        Undo.RegisterCreatedObjectUndo(groupGO, $"Create {rewardType} Reward Group");

        return groupGO;
    }

    // ============================================
    // 메서드: CreateItemFrame
    // ============================================

    private static void CreateItemFrame(GameObject parent, string rewardType)
    {
        GameObject frameGO = new GameObject($"{rewardType}_IconFrame");
        frameGO.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = frameGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50, 50);

        Image frameImage = frameGO.AddComponent<Image>();

        Sprite iconSprite = null;
        if (rewardType == "Gold")
        {
            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GOLD_ICON_PATH);
        }
        else if (rewardType == "Exp")
        {
            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EXP_ICON_PATH);
        }

        if (iconSprite != null)
        {
            frameImage.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning($"[OfflineRewardBuilder] {rewardType} 아이콘 이미지를 찾을 수 없습니다.");
            frameImage.color = rewardType == "Gold" ? new Color(1, 0.8f, 0) : new Color(0.2f, 0.8f, 1);
        }

        Undo.RegisterCreatedObjectUndo(frameGO, $"Create {rewardType} Item Frame");
    }

    // ============================================
    // 메서드: CreateButtonsArea
    // ============================================

    private static GameObject CreateButtonsArea(GameObject parent)
    {
        GameObject buttonsArea = new GameObject(BUTTONS_AREA_NAME);
        buttonsArea.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = buttonsArea.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = new Vector2(0, 20);
        rectTransform.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup layoutGroup = buttonsArea.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(30, 30, 5, 5);

        Undo.RegisterCreatedObjectUndo(buttonsArea, "Create Buttons Area");

        return buttonsArea;
    }

    // ============================================
    // 메서드: CreateClaimButton
    // ============================================

    private static GameObject CreateClaimButton(GameObject parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CLAIM_BUTTON_PREFAB_PATH);
        GameObject claimButtonGO;

        if (prefab != null)
        {
            claimButtonGO = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            Debug.LogWarning($"[OfflineRewardBuilder] ClaimButton prefab를 찾을 수 없습니다: {CLAIM_BUTTON_PREFAB_PATH}");
            claimButtonGO = new GameObject(CLAIM_BUTTON_NAME);
            Image buttonImage = claimButtonGO.AddComponent<Image>();
            buttonImage.color = Color.green;
            claimButtonGO.AddComponent<Button>();
        }

        claimButtonGO.name = CLAIM_BUTTON_NAME;
        claimButtonGO.transform.SetParent(parent.transform, false);

        // 버튼 텍스트 변경
        TextMeshProUGUI buttonText = claimButtonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "보상 받기";
        }

        Undo.RegisterCreatedObjectUndo(claimButtonGO, "Create Claim Button");

        return claimButtonGO;
    }
}
