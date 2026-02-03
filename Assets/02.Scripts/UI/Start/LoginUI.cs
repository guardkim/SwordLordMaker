using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Image _loginButton;
    [SerializeField] private TextMeshProUGUI _errorText;

    [Header("Settings")]
    [SerializeField] private int _maxLength = 12;

    public event Action<string> OnLoginRequested;

    private void Awake()
    {
        SetupLoginButton();
        SetupNicknameInput();

        HideError();
        Hide();
    }

    private void Start()
    {
        SoundManager.Instance.PlayBGM(EBgmId.Title);
    }

    private void SetupLoginButton()
    {
        if (_loginButton != null)
        {
            Debug.Log("[LoginUI] 로그인 버튼 설정 중...");

            EventTrigger trigger = _loginButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _loginButton.gameObject.AddComponent<EventTrigger>();
                Debug.Log("[LoginUI] EventTrigger 추가됨");
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) =>
            {
                Debug.Log("[LoginUI] 버튼 클릭됨!");
                RequestLogin();
            });
            trigger.triggers.Add(entry);

            Debug.Log("[LoginUI] 로그인 버튼 설정 완료");
        }
        else
        {
            Debug.LogError("[LoginUI] _loginButton이 연결되지 않았습니다!");
        }
    }

    private void SetupNicknameInput()
    {
        if (_nicknameInput != null)
        {
            _nicknameInput.characterLimit = _maxLength;
            _nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
            _nicknameInput.onSubmit.AddListener(OnNicknameSubmit);
        }
    }

    private void OnDestroy()
    {
        if (_nicknameInput != null)
        {
            _nicknameInput.onValueChanged.RemoveListener(OnNicknameChanged);
            _nicknameInput.onSubmit.RemoveListener(OnNicknameSubmit);
        }
    }

    public void Show(string defaultNickname = null)
    {
        Debug.Log($"[LoginUI] Show 호출됨, defaultNickname: {defaultNickname}");

        if (_panel != null)
        {
            _panel.SetActive(true);
        }

        if (_nicknameInput != null)
        {
            _nicknameInput.text = defaultNickname ?? string.Empty;
            _nicknameInput.Select();
            _nicknameInput.ActivateInputField();
        }

        HideError();
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void ShowError(string message)
    {
        if (_errorText != null)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }
    }

    private void HideError()
    {
        if (_errorText != null)
        {
            _errorText.gameObject.SetActive(false);
        }
    }

    private void OnNicknameSubmit(string value)
    {
        RequestLogin();
    }

    private void RequestLogin()
    {
        string nickname = _nicknameInput?.text?.Trim();
        Debug.Log($"[LoginUI] RequestLogin 호출됨, 닉네임: {nickname}");
        OnLoginRequested?.Invoke(nickname);
    }

    private void OnNicknameChanged(string value)
    {
        HideError();
    }
}
