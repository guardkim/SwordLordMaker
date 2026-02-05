using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class EnemySpawner : Singleton<EnemySpawner>, IEnemySpawner
{
    [Header("Prefab")]
    [SerializeField] private EnemyAI _enemyPrefab;
    [SerializeField] private EnemyAI _bossPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _bossSpawnPoint;

    [Header("Pool Settings")]
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 50;

    private IEnemyStatRepository _repository;
    private IBossStatRepository _bossRepository;
    private ObjectPool<EnemyAI> _pool;

    private readonly List<EnemyAI> _aliveEnemies = new();
    public IReadOnlyList<EnemyAI> AliveEnemies => _aliveEnemies;

    // 보스 상태
    private EnemyAI _currentBoss;
    private bool _bossSpawned;

    // IEnemySpawner 구현 - 상태 조회
    public int AliveEnemyCount => _aliveEnemies.Count;
    public bool IsBossSpawned => _bossSpawned;
    public bool IsBossAlive => _currentBoss != null && !_currentBoss.IsDead;
    public EnemyAI CurrentBoss => _currentBoss;

    // 기존 이벤트 (하위 호환)
    public event Action<EnemyAI> OnEnemyDiedEvent;
    public event Action<EnemyAI> OnBossDiedEvent;

    // IEnemySpawner 이벤트 구현
    public event Action<EnemyAI> OnEnemyDied;
    public event Action<EnemyAI> OnBossDied;
    public event Action<EnemyAI> OnBossSpawned;
    public event Action<EnemyStat> OnEnemyDefeatedWithStat;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _bossRepository = CreateBossRepository();
        CreatePool();

        // ServiceLocator에 등록
        ServiceLocator.Register<IEnemySpawner>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IEnemySpawner>();
    }

    private IEnemyStatRepository CreateRepository()
    {
        return new EnemyStatRepository();
    }

    private IBossStatRepository CreateBossRepository()
    {
        return new BossStatRepository();
    }

    private void CreatePool()
    {
        _pool = new ObjectPool<EnemyAI>(
            createFunc: CreatePooledItem,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }

    private EnemyAI CreatePooledItem()
    {
        EnemyAI enemy = Instantiate(_enemyPrefab);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    private void OnTakeFromPool(EnemyAI enemy)
    {
        enemy.gameObject.SetActive(true);
        _aliveEnemies.Add(enemy);

        // 사망 이벤트 구독
        enemy.OnDied += HandleEnemyDied;
    }

    private void OnReturnedToPool(EnemyAI enemy)
    {
        // 사망 이벤트 해제
        enemy.OnDied -= HandleEnemyDied;

        _aliveEnemies.Remove(enemy);
        enemy.ResetForPool();
        enemy.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(EnemyAI enemy)
    {
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }
    }

    // 적 사망 처리 핸들러 (보상은 RewardHandler가 처리)
    private void HandleEnemyDied(EnemyAI enemy, EnemyStat stat)
    {
        if (stat == null) return;

        // 보상 처리용 이벤트 발행 (RewardHandler가 구독)
        OnEnemyDefeatedWithStat?.Invoke(stat);

        // 이벤트 발생 (구독자가 처리)
        if (enemy.IsBoss)
        {
            // 보스 상태 리셋
            if (enemy == _currentBoss)
            {
                _currentBoss = null;
                _bossSpawned = false;
            }

            // 인터페이스 이벤트
            OnBossDied?.Invoke(enemy);
            // 하위 호환 이벤트
            OnBossDiedEvent?.Invoke(enemy);
        }
        else
        {
            // 인터페이스 이벤트
            OnEnemyDied?.Invoke(enemy);
            // 하위 호환 이벤트
            OnEnemyDiedEvent?.Invoke(enemy);
        }
    }

    public EnemyAI Spawn(string statId, int spawnPointIndex)
    {
        if (_spawnPoints == null || spawnPointIndex < 0 || spawnPointIndex >= _spawnPoints.Length)
        {
            Debug.LogError($"[EnemySpawner] 유효하지 않은 스폰 포인트 인덱스: {spawnPointIndex}");
            return null;
        }

        EnemyStat stat = _repository.GetById(statId);
        if (stat == null)
        {
            Debug.LogError($"[EnemySpawner] 스탯을 찾을 수 없습니다: {statId}");
            return null;
        }

        EnemyAI enemy = _pool.Get();
        Transform spawnPoint = _spawnPoints[spawnPointIndex];

        enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        enemy.Initialize(stat);

        return enemy;
    }

    public EnemyAI Spawn(string statId, Vector3 position, Quaternion rotation)
    {
        EnemyStat stat = _repository.GetById(statId);
        if (stat == null)
        {
            Debug.LogError($"[EnemySpawner] 스탯을 찾을 수 없습니다: {statId}");
            return null;
        }

        EnemyAI enemy = _pool.Get();
        enemy.transform.SetPositionAndRotation(position, rotation);
        enemy.Initialize(stat);

        return enemy;
    }

    // IEnemySpawner 구현 - ReturnEnemy
    public void ReturnEnemy(EnemyAI enemy)
    {
        Return(enemy);
    }

    public void Return(EnemyAI enemy)
    {
        if (!enemy)
        {
            return;
        }

        // 보스는 풀에 반환하지 않고 Destroy
        if (enemy.IsBoss || _pool == null)
        {
            // 이벤트 해제
            enemy.OnDied -= HandleEnemyDied;

            _aliveEnemies.Remove(enemy);
            enemy.ResetForPool();
            Destroy(enemy.gameObject);
            return;
        }

        _pool.Release(enemy);
    }

    public void ReturnAll()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (_aliveEnemies[i] != null)
            {
                Return(_aliveEnemies[i]);
            }
        }
    }

    public int SpawnPointCount => _spawnPoints?.Length ?? 0;

    public EnemyAI SpawnAtRandomPoint(string statId)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySpawner] 스폰 포인트가 설정되지 않았습니다.");
            return null;
        }

        int randomIndex = Random.Range(0, _spawnPoints.Length);
        return Spawn(statId, randomIndex);
    }

    // 스테이지 배율이 적용된 Enemy 스폰
    public EnemyAI SpawnWithMultiplier(string statId, StageStat stageStat)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySpawner] 스폰 포인트가 설정되지 않았습니다.");
            return null;
        }

        EnemyStat baseStat = _repository.GetById(statId);
        if (baseStat == null)
        {
            Debug.LogError($"[EnemySpawner] 스탯을 찾을 수 없습니다: {statId}");
            return null;
        }

        EnemyStat multipliedStat = ApplyMultiplier(baseStat, stageStat);

        EnemyAI enemy = _pool.Get();
        int randomIndex = Random.Range(0, _spawnPoints.Length);
        Transform spawnPoint = _spawnPoints[randomIndex];

        enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        enemy.Initialize(multipliedStat);

        return enemy;
    }

    // IEnemySpawner 구현 - SpawnEnemy
    public EnemyAI SpawnEnemy(string statId, StageStat stageStat)
    {
        return SpawnWithMultiplier(statId, stageStat);
    }

    // 보스 스폰 (배율 적용)
    public EnemyAI SpawnBoss(string bossStatId, StageStat stageStat)
    {
        if (_bossSpawned)
        {
            Debug.LogWarning("[EnemySpawner] Boss already spawned.");
            return null;
        }

        if (string.IsNullOrEmpty(bossStatId))
        {
            Debug.LogWarning("[EnemySpawner] BossStatId가 비어있습니다.");
            return null;
        }

        BossStat bossStat = _bossRepository.GetById(bossStatId);
        if (bossStat == null)
        {
            Debug.LogError($"[EnemySpawner] 보스 스탯을 찾을 수 없습니다: {bossStatId}");
            return null;
        }

        // BossStat을 EnemyStat으로 변환 후 배율 적용
        EnemyStat bossEnemyStat = new EnemyStat(
            bossStat.Id,
            bossStat.MaxHP,
            bossStat.AttackDamage,
            bossStat.MoveSpeed,
            bossStat.GoldReward,
            bossStat.Exp
        );

        EnemyStat multipliedStat = ApplyMultiplier(bossEnemyStat, stageStat);

        // 보스 프리팹이 없으면 일반 프리팹 사용
        EnemyAI bossPrefab = _bossPrefab != null ? _bossPrefab : _enemyPrefab;
        EnemyAI boss = Instantiate(bossPrefab);

        // 보스 스폰 위치 결정
        Transform spawnPoint = _bossSpawnPoint != null
            ? _bossSpawnPoint
            : _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        boss.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        boss.InitializeAsBoss(multipliedStat);

        _aliveEnemies.Add(boss);

        // 보스도 사망 이벤트 구독
        boss.OnDied += HandleEnemyDied;

        // 보스 상태 저장
        _currentBoss = boss;
        _bossSpawned = true;

        // 보스 스폰 이벤트 발행
        OnBossSpawned?.Invoke(boss);

        return boss;
    }

    // 보스 상태 리셋 (스테이지 전환 시 호출)
    public void ResetBossState()
    {
        _currentBoss = null;
        _bossSpawned = false;
    }

    private EnemyStat ApplyMultiplier(EnemyStat baseStat, StageStat stageStat)
    {
        // BigInteger에 float 배율 적용
        double multipliedHP = MultiplyBigInteger(baseStat.MaxHP, stageStat.HpMultiplier);
        double multipliedAttack = MultiplyBigInteger(baseStat.AttackDamage, stageStat.AttackMultiplier);
        double multipliedGold = MultiplyBigInteger(baseStat.GoldReward, stageStat.GoldMultiplier);
        float multipliedSpeed = baseStat.MoveSpeed * stageStat.SpeedMultiplier;
        double multipliedExp = baseStat.Exp * stageStat.ExpMultiplier;

        return new EnemyStat(
            baseStat.Id,
            multipliedHP,
            multipliedAttack,
            multipliedSpeed,
            multipliedGold,
            multipliedExp
        );
    }

    private double MultiplyBigInteger(double value, float multiplier)
    {
        if (multiplier <= 0f) return value;
        if (multiplier == 1f) return value;

        // 정밀도를 위해 1000 단위로 계산
        int scaledMultiplier = (int)(multiplier * 1000);
        return value * scaledMultiplier / 1000;
    }
}
