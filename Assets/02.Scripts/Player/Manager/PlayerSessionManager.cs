using System;
using BansheeGz.BGDatabase;
using UnityEngine;

public class PlayerSessionManager : DontDestroySingleton<PlayerSessionManager>
{
    private const string PlayerProfileTableName = "PlayerProfile";
    private const string PlayerNameKey = "LastPlayerName";
    private const string DefaultPlayerName = "Guardkim";

    private string _currentPlayerName;

    public string CurrentPlayerName => _currentPlayerName;

    public event Action<string> OnPlayerNameChanged;

    protected override void Initialize()
    {
        LoadPlayerName();
    }

    private void LoadPlayerName()
    {
        _currentPlayerName = LoadPlayerNameFromDatabase();

        if (string.IsNullOrEmpty(_currentPlayerName))
        {
            _currentPlayerName = PlayerPrefs.GetString(PlayerNameKey, DefaultPlayerName);
        }

        if (string.IsNullOrEmpty(_currentPlayerName))
        {
            _currentPlayerName = DefaultPlayerName;
        }

        PlayerPrefs.SetString(PlayerNameKey, _currentPlayerName);
        PlayerPrefs.Save();
    }

    private string LoadPlayerNameFromDatabase()
    {
        BGMetaEntity meta = BGRepo.I[PlayerProfileTableName];
        if (meta == null || meta.CountEntities == 0)
        {
            return null;
        }

        int count = meta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = meta.GetEntity(i);
            if (entity.Name == DefaultPlayerName)
            {
                return entity.Name;
            }
        }

        return meta.GetEntity(0)?.Name;
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

    public void CreateNewPlayer(string name)
    {
        SetPlayerName(name);
    }
}
