using System.Collections.Generic;
using UnityEngine;

public class AdelFlyingSwordController : BaseSwordController
{
    [Header("■ [Adel] 사출 설정")]
    public float SpawnForce = 10f;
    public int MaxSwordCount = 6;
    public float AttackDelay = 0.2f;

    [Header("■ 타겟팅 설정")]
    public float MaxTargetDistance = 25f;

    [Header("■ Sword Stat")]
    [SerializeField] private string _swordStatId = "ADEL_SWORD";

    private SwordStat _baseStat;
    private SwordStat _swordStat;
    private bool _isInitialized;

    public SwordStat SwordStat => _swordStat;

    private readonly List<AdelFlyingSword> _activeSwords = new List<AdelFlyingSword>();
    private readonly List<EnemyAI> _filteredEnemyBuffer = new List<EnemyAI>();
    private int _currentAttackerOrderIndex;
    private int _spawnTotalCount;
    private float _delayTimer;

    private float _searchTimer;
    private const float SearchInterval = 0.2f;

    private void Awake()
    {
        LoadBaseStat();
        ApplyUpgrades();
    }

    private void Start()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized += OnUpgradeManagerInitialized;

            if (!UpgradeManager.Instance.IsReady)
            {
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (UpgradeManager.HasInstance)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized -= OnUpgradeManagerInitialized;
        }
    }

    private void LoadBaseStat()
    {
        if (_isInitialized) return;

        var repository = new SwordStatRepository();
        _baseStat = repository.GetById(_swordStatId);
        _swordStat = new SwordStat(_baseStat);

        _isInitialized = true;
    }

    private void ApplyUpgrades()
    {
        if (_baseStat == null || _swordStat == null) return;

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrades(_baseStat, _swordStat);
        }
        else
        {
            _swordStat.CopyFrom(_baseStat);
        }
    }

    private void OnUpgradeManagerInitialized()
    {
        ApplyUpgrades();
        UpdateActiveSwords();
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        if (upgradeId.StartsWith("Sword_"))
        {
            ApplyUpgrades();
            UpdateActiveSwords();
        }
    }

    private void UpdateActiveSwords()
    {
        foreach (AdelFlyingSword sword in _activeSwords)
        {
            if (sword != null)
            {
                sword.InitializeStat(_swordStat);
            }
        }
    }

    protected override void ResetSequence()
    {
        SpawnDualSwords();
    }

    private void Update()
    {
        if (_delayTimer > 0) _delayTimer -= Time.deltaTime;

        _searchTimer += Time.deltaTime;
        if (_searchTimer >= SearchInterval)
        {
            RetargetSwords();
            _searchTimer = 0f;
        }
    }

    private void SpawnDualSwords()
    {
        if (_activeSwords.Count >= MaxSwordCount) return;

        IReadOnlyList<EnemyAI> enemies = FindEnemies();
        IReadOnlyList<EnemyAI> validEnemies = FilterEnemiesByDistance(enemies);
        if (validEnemies == null || validEnemies.Count == 0) return;

        Transform target = GetRandomEnemyTarget(validEnemies);

        for (int i = 0; i < 2; i++)
        {
            if (_activeSwords.Count >= MaxSwordCount) break;

            Vector3 ejectDirection = (i == 0) ? Vector3.left : Vector3.right;
            ejectDirection += Vector3.up * Random.Range(0.2f, 1.0f);

            GameObject obj = Instantiate(SwordPrefab, transform.position, Quaternion.identity);
            AdelFlyingSword sword = obj.GetComponent<AdelFlyingSword>();

            if (sword)
            {
                int myOrder = _spawnTotalCount++;
                if (_activeSwords.Count == 0) _currentAttackerOrderIndex = myOrder;

                _activeSwords.Add(sword);
                sword.Init(this, target, ejectDirection, SpawnForce, myOrder, _swordStat);
            }
        }
    }

    // 버퍼 재사용 + sqrMagnitude 최적화
    private IReadOnlyList<EnemyAI> FilterEnemiesByDistance(IReadOnlyList<EnemyAI> enemies)
    {
        _filteredEnemyBuffer.Clear();
        if (enemies == null) return _filteredEnemyBuffer;

        Vector3 playerPos = transform.position;
        float maxDistSqr = MaxTargetDistance * MaxTargetDistance;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null) continue;
            float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;
            if (distSqr <= maxDistSqr)
            {
                _filteredEnemyBuffer.Add(enemy);
            }
        }

        return _filteredEnemyBuffer;
    }

    public void RequestNewTarget(AdelFlyingSword sword)
    {
        IReadOnlyList<EnemyAI> enemies = FindEnemies();
        IReadOnlyList<EnemyAI> validEnemies = FilterEnemiesByDistance(enemies);

        if (validEnemies != null && validEnemies.Count > 0)
        {
            sword.SetTarget(validEnemies[Random.Range(0, validEnemies.Count)].transform);
        }
        else
        {
            sword.SetTarget(null);
        }
    }

    public void RemoveSword(AdelFlyingSword sword)
    {
        if (_activeSwords.Contains(sword))
        {
            _activeSwords.Remove(sword);
            if (sword.OrderIndex == _currentAttackerOrderIndex) IncrementTurnIndex();
        }
    }

    public bool IsMyTurn(int swordOrderIndex)
    {
        IReadOnlyList<EnemyAI> enemies = FindEnemies();
        if (enemies == null || enemies.Count == 0) return false;
        if (_delayTimer > 0) return false;
        return swordOrderIndex == _currentAttackerOrderIndex;
    }

    public void NextTurn()
    {
        _delayTimer = AttackDelay;
        IncrementTurnIndex();
    }

    // FirstOrDefault() 대체 - foreach 루프 사용
    private void IncrementTurnIndex()
    {
        if (_activeSwords.Count == 0) return;

        _activeSwords.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));

        AdelFlyingSword nextSword = null;
        foreach (AdelFlyingSword sword in _activeSwords)
        {
            if (sword.OrderIndex > _currentAttackerOrderIndex)
            {
                nextSword = sword;
                break;
            }
        }

        if (nextSword == null)
            nextSword = _activeSwords[0];

        _currentAttackerOrderIndex = nextSword.OrderIndex;
    }

    // All() 대체 - foreach 루프 사용
    private void RetargetSwords()
    {
        if (_activeSwords.Count == 0) return;

        bool allHaveTargets = true;
        foreach (AdelFlyingSword sword in _activeSwords)
        {
            if (!sword.HasTarget())
            {
                allHaveTargets = false;
                break;
            }
        }
        if (allHaveTargets) return;

        IReadOnlyList<EnemyAI> enemies = FindEnemies();
        IReadOnlyList<EnemyAI> validEnemies = FilterEnemiesByDistance(enemies);

        if (validEnemies == null || validEnemies.Count == 0)
        {
            foreach (AdelFlyingSword sword in _activeSwords) sword.SetTarget(null);
            return;
        }

        foreach (AdelFlyingSword sword in _activeSwords)
        {
            if (!sword.HasTarget())
            {
                sword.SetTarget(validEnemies[Random.Range(0, validEnemies.Count)].transform);
            }
        }
    }
}
