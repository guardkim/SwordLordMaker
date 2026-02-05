using System;
using System.Collections;
using UnityEngine;

public class StageManager : Singleton<StageManager>, IStageService
{
    [Header("Settings")]
    [SerializeField] private bool _autoStartOnAwake = true;
    [SerializeField] private float _spawnInterval = 1f;
    [SerializeField] private float _stageTransitionDelay = 5f;

    private IStageRepository _repository;
    private int _currentStageId = 1;
    private int _maxStageId;

    private bool _isSpawning;
    private StageStat _currentStageStat;
    private Coroutine _spawnCoroutine;

    // DI: 인터페이스를 통한 의존성
    private IEnemySpawner _enemySpawner;
    private IGameService _gameService;

    // IStageService 구현 - 상태 조회
    public int CurrentStageId => _currentStageId;
    public string CurrentStageName => _currentStageStat?.StageName ?? "";
    public StageStat CurrentStageStat => _currentStageStat;

    // 프록시 프로퍼티 (하위 호환 - UI가 사용 중)
    // 내부적으로는 인터페이스를 통해 접근
    public int AliveEnemyCount => _enemySpawner?.AliveEnemyCount ?? 0;
    public bool IsBossSpawned => _enemySpawner?.IsBossSpawned ?? false;
    public bool IsBossAlive => _enemySpawner?.IsBossAlive ?? false;

    // IStageService 이벤트
    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action OnAllStagesCleared;
    public event Action<EnemyAI> OnBossSpawned;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _maxStageId = _repository.GetMaxStageId();

        // ServiceLocator에 등록
        ServiceLocator.Register<IStageService>(this);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        ServiceLocator.Unregister<IStageService>();
    }

    private IStageRepository CreateRepository()
    {
        return new StageRepository();
    }

    private void Start()
    {
        InitializeDependencies();
        SubscribeToEvents();

        if (_autoStartOnAwake)
        {
            StartGame();
        }
    }

    private void InitializeDependencies()
    {
        // ServiceLocator에서 의존성 획득 (우선)
        _enemySpawner = ServiceLocator.Resolve<IEnemySpawner>();
        _gameService = ServiceLocator.Resolve<IGameService>();

        // 폴백: ServiceLocator에 없으면 기존 싱글톤 사용
        if (_enemySpawner == null && EnemySpawner.Instance)
        {
            _enemySpawner = EnemySpawner.Instance;
        }

        if (_gameService == null && GameManager.HasInstance)
        {
            _gameService = GameManager.Instance;
        }
    }

    private void SubscribeToEvents()
    {
        // IGameService 이벤트 구독
        if (_gameService != null)
        {
            _gameService.OnRequestStageRestart += HandleStageRestartRequest;
        }

        // IEnemySpawner 이벤트 구독
        if (_enemySpawner != null)
        {
            _enemySpawner.OnBossDied += HandleBossDied;
        }
    }

    private void UnsubscribeFromEvents()
    {
        // IGameService 이벤트 구독 해제
        if (_gameService != null)
        {
            _gameService.OnRequestStageRestart -= HandleStageRestartRequest;
        }

        // IEnemySpawner 이벤트 구독 해제
        if (_enemySpawner != null)
        {
            _enemySpawner.OnBossDied -= HandleBossDied;
        }
    }

    private void HandleStageRestartRequest(int stageId)
    {
        RestartFromStage(stageId);
    }

    public void StartGame()
    {
        _currentStageId = 1;
        StartStage(_currentStageId);

        // SoundManager는 독립적이므로 기존 방식 유지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(EBgmId.Main);
        }
    }

    public void StartStage(int stageId)
    {
        StageStat stage = _repository.GetByStageId(stageId);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] Stage not found: {stageId}");
            return;
        }

        _currentStageId = stageId;
        _currentStageStat = stage;
        _isSpawning = true;

        // 보스 상태 리셋 (인터페이스 통해 접근)
        _enemySpawner?.ResetBossState();

        OnStageStarted?.Invoke(_currentStageId);

        _spawnCoroutine = StartCoroutine(SpawnEnemiesRoutine(stage));
    }

    private IEnumerator SpawnEnemiesRoutine(StageStat stage)
    {
        var wait = new WaitForSeconds(_spawnInterval);

        while (_isSpawning)
        {
            SpawnEnemy(stage);
            yield return wait;
        }
    }

    private void SpawnEnemy(StageStat stage)
    {
        if (_enemySpawner == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not available.");
            return;
        }

        // 인터페이스를 통해 스폰 요청
        _enemySpawner.SpawnEnemy(stage.EnemyStatId, stage);
    }

    public void SpawnBoss()
    {
        if (_currentStageStat == null || string.IsNullOrEmpty(_currentStageStat.BossStatId))
        {
            Debug.LogWarning("[StageManager] No boss configured for this stage.");
            return;
        }

        if (_enemySpawner == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not available.");
            return;
        }

        StopSpawning();
        ClearAllEnemies();

        // 인터페이스를 통해 보스 스폰 요청
        EnemyAI boss = _enemySpawner.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
        if (boss != null)
        {
            OnBossSpawned?.Invoke(boss);
        }
    }

    private void HandleBossDied(EnemyAI boss)
    {
        _isSpawning = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        OnStageCleared?.Invoke(_currentStageId);

        StartCoroutine(TransitionToNextStage());
    }

    private IEnumerator TransitionToNextStage()
    {
        ClearAllEnemies();

        yield return new WaitForSeconds(_stageTransitionDelay);

        if (_currentStageId < _maxStageId)
        {
            StartStage(_currentStageId + 1);
        }
        else
        {
            OnAllStagesCleared?.Invoke();
            StartStage(_maxStageId);
        }
    }

    public void RestartCurrentStage()
    {
        StopSpawning();
        ClearAllEnemies();
        StartStage(_currentStageId);
    }

    public void RestartFromStage(int stageId)
    {
        StopSpawning();
        ClearAllEnemies();
        StartStage(stageId);
    }

    public void OnPlayerDied()
    {
        StopSpawning();
        ClearAllEnemies();
    }

    private void StopSpawning()
    {
        _isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private void ClearAllEnemies()
    {
        // 인터페이스를 통해 모든 적 반환 요청
        _enemySpawner?.ReturnAll();
    }
}
