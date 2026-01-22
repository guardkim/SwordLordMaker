using System;
using System.Text.RegularExpressions;
using BansheeGz.BGDatabase;
using UnityEngine;

public class PlayerSessionManager : DontDestroySingleton<PlayerSessionManager>
{
    private const string PlayerProfileTableName = "PlayerProfile";
    private const string PlayerStatTableName = "PlayerStat";
    private const string PlayerNameKey = "LastPlayerName";

    private string _currentPlayerName;
    private bool _isLoggedIn;

    public string CurrentPlayerName => _currentPlayerName;
    public bool IsLoggedIn => _isLoggedIn;

    public event Action<string> OnPlayerNameChanged;
    public event Action OnLoginCompleted;

    protected override void Initialize()
    {
        // 자동 로드 제거 - StartScene에서 명시적으로 로그인 처리
    }

    public string GetSavedPlayerName()
    {
        string savedName = PlayerPrefs.GetString(PlayerNameKey, null);
        return string.IsNullOrEmpty(savedName) ? null : savedName;
    }

    public bool PlayerExistsInDatabase(string playerName)
    {
        Debug.Log($"[PlayerSessionManager] PlayerExistsInDatabase 호출: '{playerName}'");

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("[PlayerSessionManager] 플레이어 이름이 비어있음");
            return false;
        }

        BGMetaEntity meta = BGRepo.I[PlayerProfileTableName];
        if (meta == null || meta.CountEntities == 0)
        {
            Debug.Log($"[PlayerSessionManager] 테이블이 없거나 비어있음 (count: {meta?.CountEntities ?? 0})");
            return false;
        }

        int count = meta.CountEntities;
        Debug.Log($"[PlayerSessionManager] DB에 있는 플레이어 수: {count}");

        for (int i = 0; i < count; i++)
        {
            BGEntity entity = meta.GetEntity(i);
            Debug.Log($"[PlayerSessionManager] DB 플레이어 [{i}]: '{entity.Name}'");

            if (entity.Name == playerName)
            {
                Debug.Log($"[PlayerSessionManager] 일치하는 플레이어 발견: '{playerName}'");
                return true;
            }
        }

        Debug.Log($"[PlayerSessionManager] '{playerName}'을 찾지 못함");
        return false;
    }

    public NicknameValidationResult ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return NicknameValidationResult.Empty;
        }

        if (nickname.Length < 2 || nickname.Length > 12)
        {
            return NicknameValidationResult.InvalidLength;
        }

        if (!Regex.IsMatch(nickname, @"^[가-힣a-zA-Z0-9]+$"))
        {
            return NicknameValidationResult.InvalidCharacter;
        }

        return NicknameValidationResult.Valid;
    }

    public void Login(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("[PlayerSessionManager] 플레이어 이름이 비어있습니다.");
            return;
        }

        _currentPlayerName = playerName;
        _isLoggedIn = true;

        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();

        OnPlayerNameChanged?.Invoke(_currentPlayerName);
        OnLoginCompleted?.Invoke();

        Debug.Log($"[PlayerSessionManager] 로그인 완료: {playerName}");
    }

    public void CreatePlayerInDatabase(string playerName)
    {
        Debug.Log($"[PlayerSessionManager] CreatePlayerInDatabase 호출: '{playerName}'");

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("[PlayerSessionManager] 플레이어 이름이 비어있어 생성할 수 없습니다.");
            return;
        }

        CreatePlayerProfile(playerName);
        CreatePlayerStat(playerName);

        BGRepo.I.Save();

        Debug.Log($"[PlayerSessionManager] 신규 플레이어 생성 완료: '{playerName}'");
    }

    private void CreatePlayerProfile(string playerName)
    {
        BGMetaEntity profileMeta = BGRepo.I[PlayerProfileTableName];
        if (profileMeta == null)
        {
            Debug.LogWarning($"[PlayerSessionManager] 테이블을 찾을 수 없습니다: {PlayerProfileTableName}");
            return;
        }

        BGEntity profileEntity = profileMeta.NewEntity();
        profileEntity.Name = playerName;
        profileEntity.Set("Gold", "0");
        profileEntity.Set("Ruby", "0");
        profileEntity.Set("UpgradeLevels", "{}");
        profileEntity.Set("LastLoginTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
    }

    private void CreatePlayerStat(string playerName)
    {
        BGMetaEntity statMeta = BGRepo.I[PlayerStatTableName];
        if (statMeta == null)
        {
            Debug.LogWarning($"[PlayerSessionManager] 테이블을 찾을 수 없습니다: {PlayerStatTableName}");
            return;
        }

        BGEntity statEntity = statMeta.NewEntity();
        statEntity.Name = playerName;
        statEntity.Set("BaseMaxHealth", "100");
        statEntity.Set("BaseMoveSpeed", 5.0f);
        statEntity.Set("Level", 1);
        statEntity.Set("CurrentExp", 0.0);
        statEntity.Set("MaxExp", 10.0);
    }

    public void Logout()
    {
        _currentPlayerName = null;
        _isLoggedIn = false;

        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.Save();

        Debug.Log("[PlayerSessionManager] 로그아웃 완료");
    }

    public void SetPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[PlayerSessionManager] 플레이어 이름이 비어있습니다.");
            return;
        }

        _currentPlayerName = name;
        PlayerPrefs.SetString(PlayerNameKey, name);
        PlayerPrefs.Save();

        OnPlayerNameChanged?.Invoke(_currentPlayerName);
    }
}
