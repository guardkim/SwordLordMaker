using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : DontDestroySingleton<EnemySpawner>
{
    [Header("Prefab")]
    [SerializeField] private EnemyAI _enemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Pool Settings")]
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 50;

    private IEnemyStatRepository _repository;
    private ObjectPool<EnemyAI> _pool;

    protected override void Initialize()
    {
        _repository = new EnemyStatRepository();
        CreatePool();
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

        if (_pool == null)
        {
            enemy.ResetForPool();
            Destroy(enemy.gameObject);
            return;
        }

        _pool.Release(enemy);
    }

    public int SpawnPointCount => _spawnPoints?.Length ?? 0;
}
