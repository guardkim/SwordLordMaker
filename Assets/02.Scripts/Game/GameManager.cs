using System;
using System.Collections;
using UnityEngine;

public class GameManager : DontDestroySingleton<GameManager>, IGameService
{
    private const float RESPAWN_DELAY = 5f;
    private const int RESPAWN_STAGE_ID = 1;

    private PlayerHealth _playerHealth;

    // DI: 인터페이스를 통한 의존성
    private IStageService _stageService;

    // IGameService 이벤트 구현
    public event Action OnPlayerDeath;
    public event Action OnPlayerRevive;
    public event Action<int> OnRequestStageRestart;

    protected override void Initialize()
    {
        // ServiceLocator에 등록
        ServiceLocator.Register<IGameService>(this);
    }

    private void Start()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        // ServiceLocator에서 의존성 획득
        _stageService = ServiceLocator.Resolve<IStageService>();

        // 폴백: ServiceLocator에 없으면 기존 싱글톤 사용
        if (_stageService == null && StageManager.Instance)
        {
            _stageService = StageManager.Instance;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayer();
        ServiceLocator.Unregister<IGameService>();
    }

    // IGameService 구현
    public void RegisterPlayer(PlayerHealth playerHealth)
    {
        UnsubscribeFromPlayer();

        _playerHealth = playerHealth;

        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }
    }

    public void UnregisterPlayer()
    {
        UnsubscribeFromPlayer();
        _playerHealth = null;
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

        // 인터페이스를 통해 스테이지 매니저에 알림
        _stageService?.OnPlayerDied();

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
