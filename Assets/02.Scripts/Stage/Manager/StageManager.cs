using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : DontDestroySingleton<StageManager>
{
    [Header("Settings")]
    [SerializeField] private bool _autoStartOnAwake = true;
    [SerializeField] private float _spawnInterval = 1f;
    [SerializeField] private float _stageTransitionDelay = 5f;

    private IStageRepository _repository;
    private int _currentStageId = 1;
    private int _maxStageId;

    private EnemyAI _currentBoss;
    private bool _isSpawning;
    private bool _bossSpawned;
    private StageStat _currentStageStat;
    private Coroutine _spawnCoroutine;

    public int CurrentStageId => _currentStageId;
    public string CurrentStageName => _currentStageStat?.StageName ?? "";
    public StageStat CurrentStageStat => _currentStageStat;
    public int AliveEnemyCount => EnemySpawner.Instance?.AliveEnemies.Count ?? 0;
    public bool IsBossSpawned => _bossSpawned;
    public bool IsBossAlive => _currentBoss != null && !_currentBoss.IsDead;

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action OnAllStagesCleared;
    public event Action<EnemyAI> OnBossSpawned;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _maxStageId = _repository.GetMaxStageId();
    }

    private IStageRepository CreateRepository()
    {
        return new StageRepository();
    }

    private void Start()
    {
        SubscribeToEvents();

        if (_autoStartOnAwake)
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRequestStageRestart += HandleStageRestartRequest;
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnBossDiedEvent += HandleBossDied;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnRequestStageRestart -= HandleStageRestartRequest;
        }

        if (EnemySpawner.HasInstance)
        {
            EnemySpawner.Instance.OnBossDiedEvent -= HandleBossDied;
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
        _currentBoss = null;
        _bossSpawned = false;
        _isSpawning = true;

        OnStageStarted?.Invoke(_currentStageId);

        _spawnCoroutine = StartCoroutine(SpawnEnemiesRoutine(stage));
    }

    // 1초마다 무한 스폰 (보스 처치 전까지)
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
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not found.");
            return;
        }

        // 배율 적용된 Enemy 스폰 (EnemySpawner가 AliveEnemies 자동 관리)
        EnemySpawner.Instance.SpawnWithMultiplier(stage.EnemyStatId, stage);
    }

    // 외부에서 호출하여 보스 스폰 (UI 버튼 등)
    public void SpawnBoss()
    {
        if (_bossSpawned)
        {
            Debug.LogWarning("[StageManager] Boss already spawned.");
            return;
        }

        if (_currentStageStat == null || string.IsNullOrEmpty(_currentStageStat.BossStatId))
        {
            Debug.LogWarning("[StageManager] No boss configured for this stage.");
            return;
        }

        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("[StageManager] EnemySpawner not found.");
            return;
        }

        // 스폰 중지 + 모든 몬스터 제거
        StopSpawning();
        ClearAllEnemies();

        EnemyAI boss = EnemySpawner.Instance.SpawnBoss(_currentStageStat.BossStatId, _currentStageStat);
        if (boss != null)
        {
            _currentBoss = boss;
            _bossSpawned = true;
            OnBossSpawned?.Invoke(boss);
        }
    }

    // 보스 사망 처리 핸들러 (EnemySpawner 이벤트 구독)
    private void HandleBossDied(EnemyAI boss)
    {
        if (boss != _currentBoss) return;

        _currentBoss = null;
        _isSpawning = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        // 스테이지 클리어
        OnStageCleared?.Invoke(_currentStageId);

        // 남은 적들 제거 후 다음 스테이지로 전환
        StartCoroutine(TransitionToNextStage());
    }

    private IEnumerator TransitionToNextStage()
    {
        // 남은 적들 정리
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
        // EnemySpawner에게 모든 적 반환 위임
        EnemySpawner.Instance?.ReturnAll();

        // 보스도 제거
        if (_currentBoss != null)
        {
            EnemySpawner.Instance?.Return(_currentBoss);
            _currentBoss = null;
        }

        _bossSpawned = false;
    }
}
