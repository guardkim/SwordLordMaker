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

    protected override void Initialize()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (_playerHealth != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            SubscribeToPlayerEvents();
        }
    }

    private void SubscribeToPlayerEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }
    }

    private void OnDestroy()
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

        // 스테이지 리셋
        if (StageManager.Instance != null)
        {
            StageManager.Instance.RestartFromStage(RESPAWN_STAGE_ID);
        }

        // 플레이어 부활
        if (_playerHealth != null)
        {
            _playerHealth.Revive();
        }

        OnPlayerRevive?.Invoke();
    }

    public void RegisterPlayer(PlayerHealth playerHealth)
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }

        _playerHealth = playerHealth;
        SubscribeToPlayerEvents();
    }
}
