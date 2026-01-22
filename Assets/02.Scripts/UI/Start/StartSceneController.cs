using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScene";
    private const string MainSceneName = "MainScene";

    [Header("UI References")]
    [SerializeField] private LoginUI _loginUI;

    private void OnEnable()
    {
        ShowLoginUI();
    }

    private void ShowLoginUI()
    {
        if (_loginUI != null)
        {
            string savedName = PlayerSessionManager.Instance.GetSavedPlayerName();
            _loginUI.Show(savedName);
            _loginUI.OnLoginRequested += HandleLoginRequest;
        }
        else
        {
            Debug.LogError("[StartSceneController] LoginUI가 연결되지 않았습니다.");
        }
    }

    private void HandleLoginRequest(string nickname)
    {
        Debug.Log($"[StartSceneController] HandleLoginRequest 호출: '{nickname}'");

        NicknameValidationResult result = PlayerSessionManager.Instance.ValidateNickname(nickname);
        Debug.Log($"[StartSceneController] 닉네임 검증 결과: {result}");

        if (result != NicknameValidationResult.Valid)
        {
            _loginUI.ShowError(GetErrorMessage(result));
            return;
        }

        bool exists = PlayerSessionManager.Instance.PlayerExistsInDatabase(nickname);
        Debug.Log($"[StartSceneController] DB에 플레이어 존재 여부: {exists}");

        if (!exists)
        {
            Debug.Log($"[StartSceneController] 신규 플레이어 생성 시작: '{nickname}'");
            PlayerSessionManager.Instance.CreatePlayerInDatabase(nickname);
        }

        PlayerSessionManager.Instance.Login(nickname);

        _loginUI.OnLoginRequested -= HandleLoginRequest;
        MoveToLoadingScene();
    }

    private string GetErrorMessage(NicknameValidationResult result)
    {
        return result switch
        {
            NicknameValidationResult.Empty => "닉네임을 입력해주세요.",
            NicknameValidationResult.InvalidLength => "닉네임은 2~12자로 입력해주세요.",
            NicknameValidationResult.InvalidCharacter => "한글, 영문, 숫자만 사용할 수 있습니다.",
            _ => "올바른 닉네임을 입력해주세요."
        };
    }

    private void MoveToLoadingScene()
    {
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOut(() =>
            {
                LoadingSceneController.NextSceneName = MainSceneName;
                SceneManager.LoadScene(LoadingSceneName);
            });
        }
        else
        {
            LoadingSceneController.NextSceneName = MainSceneName;
            SceneManager.LoadScene(LoadingSceneName);
        }
    }
}
