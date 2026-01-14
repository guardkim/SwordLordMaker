using System;
using System.Collections;
using UnityEngine;

public class GameManager : DontDestroySingleton<GameManager>
{
    private const float RESPAWN_DELAY = 5f;
    private const int RESPAWN_STAGE_ID = 1;

    private PlayerHealth _playerHealth;

    public event Action OnPlayerDeath;
    public event Action OnPlayerRevive;
    public event Action<int> OnRequestStageRestart;

    private void OnDestroy()
    {
        UnsubscribeFromPlayer();
    }

    public void RegisterPlayer(PlayerHealth playerHealth)
    {
        UnsubscribeFromPlayer();

        _playerHealth = playerHealth;

        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }
    }

    private void UnsubscribeFromPlayer()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        OnPlayerDeath?.Invoke();
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(RESPAWN_DELAY);

        // 스테이지 리셋 요청 이벤트 발생
        OnRequestStageRestart?.Invoke(RESPAWN_STAGE_ID);

        // 플레이어 부활
        if (_playerHealth != null)
        {
            _playerHealth.Revive();
        }

        OnPlayerRevive?.Invoke();
    }
}
