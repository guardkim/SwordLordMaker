using System.Numerics;
using UnityEngine;
using UnityEngine.Pool;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class EnemySpawner : DontDestroySingleton<EnemySpawner>
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

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _bossRepository = CreateBossRepository();
        CreatePool();
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
    }

    private void OnReturnedToPool(EnemyAI enemy)
    {
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

    public void Return(EnemyAI enemy)
    {
        if (!enemy)
        {
            return;
        }

        // 보스는 풀에 반환하지 않고 Destroy
        if (enemy.IsBoss)
        {
            enemy.ResetForPool();
            Destroy(enemy.gameObject);
            return;
        }

        if (_pool == null)
        {
            enemy.ResetForPool();
            Destroy(enemy.gameObject);
            return;
        }

        _pool.Release(enemy);
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

    // 보스 스폰 (배율 적용)
    public EnemyAI SpawnBoss(string bossStatId, StageStat stageStat)
    {
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

        return boss;
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
