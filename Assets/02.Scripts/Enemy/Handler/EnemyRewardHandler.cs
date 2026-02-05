using UnityEngine;

public class EnemyRewardHandler : MonoBehaviour, IRewardHandler
{
    private ICurrencyService _currencyService;
    private IPlayerStatService _playerStatService;
    private IEnemySpawner _enemySpawner;

    private void Start()
    {
        InitializeDependencies();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeDependencies()
    {
        _currencyService = ServiceLocator.Resolve<ICurrencyService>();
        _playerStatService = ServiceLocator.Resolve<IPlayerStatService>();
        _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();

        // 폴백: ServiceLocator에 없으면 기존 싱글톤 사용
        if (_currencyService == null && CurrencyManager.HasInstance)
        {
            _currencyService = CurrencyManager.Instance;
        }

        if (_playerStatService == null && PlayerStatManager.HasInstance)
        {
            _playerStatService = PlayerStatManager.Instance;
        }

        if (_enemySpawner == null && EnemySpawner.Instance)
        {
            _enemySpawner = EnemySpawner.Instance;
        }
    }

    private void SubscribeToEvents()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemyDefeatedWithStat += HandleReward;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemyDefeatedWithStat -= HandleReward;
        }
    }

    public void HandleReward(EnemyStat stat)
    {
        if (stat == null) return;

        _currencyService?.AddGold(stat.GoldReward);
        _playerStatService?.AddExp(stat.Exp);
    }
}
