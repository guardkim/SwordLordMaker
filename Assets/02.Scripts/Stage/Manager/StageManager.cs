using System;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : DontDestroySingleton<StageManager>
{
    [Header("▼ 설정")]
    [SerializeField] private bool _autoStartOnAwake = true;
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _stageTransitionDelay = 2f;

    private IStageRepository _repository;
    private int _currentStageId = 1;
    private int _maxStageId;

    private List<EnemyAI> _aliveEnemies = new List<EnemyAI>();
    private int _remainingSpawnCount;
    private bool _isSpawning;
    private StageStat _currentStageStat;

    public int CurrentStageId => _currentStageId;
    public string CurrentStageName => _currentStageStat?.StageName ?? "";
    public StageStat CurrentStageStat => _currentStageStat;
    public int AliveEnemyCount => _aliveEnemies.Count;

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action OnAllStagesCleared;

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
        if (_autoStartOnAwake)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        _currentStageId = 1;
        StartStage(_currentStageId);
    }

    public void StartStage(int stageId)
    {
        StageStat stage = _repository.GetByStageId(stageId);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] 스테이지를 찾을 수 없습니다: {stageId}");
            return;
        }

        _currentStageId = stageId;
        _currentStageStat = stage;
        _aliveEnemies.Clear();
        _remainingSpawnCount = stage.SpawnCount;
        _isSpawning = true;

        OnStageStarted?.Invoke(_currentStageId);

        StartCoroutine(SpawnEnemiesRoutine(stage));
    }

    private System.Collections.IEnumerator SpawnEnemiesRoutine(StageStat stage)
    {
        var wait = new WaitForSeconds(_spawnInterval);

        while (_remainingSpawnCount > 0)
        {
            SpawnEnemy(stage.EnemyStatId);
            _remainingSpawnCount--;
            yield return wait;
        }

        _isSpawning = false;
    }

    private void SpawnEnemy(string enemyStatId)
    {
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("[StageManager] EnemySpawner가 없습니다.");
            return;
        }

        EnemyAI enemy = EnemySpawner.Instance.SpawnAtRandomPoint(enemyStatId);
        if (enemy != null)
        {
            _aliveEnemies.Add(enemy);
        }
    }

    public void OnEnemyDied(EnemyAI enemy)
    {
        _aliveEnemies.Remove(enemy);

        // 스폰 완료 + 모든 Enemy 처치 시 스테이지 클리어
        if (!_isSpawning && _aliveEnemies.Count == 0)
        {
            OnStageCleared?.Invoke(_currentStageId);
            StartCoroutine(TransitionToNextStage());
        }
    }

    private System.Collections.IEnumerator TransitionToNextStage()
    {
        yield return new WaitForSeconds(_stageTransitionDelay);

        if (_currentStageId < _maxStageId)
        {
            StartStage(_currentStageId + 1);
        }
        else
        {
            // 모든 스테이지 클리어 이벤트 발생 후 마지막 스테이지 반복
            OnAllStagesCleared?.Invoke();
            StartStage(_maxStageId);
        }
    }

    public void RestartCurrentStage()
    {
        ClearAllEnemies();
        StopAllCoroutines();
        StartStage(_currentStageId);
    }

    public void RestartFromStage(int stageId)
    {
        ClearAllEnemies();
        StopAllCoroutines();
        StartStage(stageId);
    }

    private void ClearAllEnemies()
    {
        foreach (var enemy in _aliveEnemies.ToArray())
        {
            if (enemy != null)
            {
                EnemySpawner.Instance?.Return(enemy);
            }
        }
        _aliveEnemies.Clear();
    }
}
