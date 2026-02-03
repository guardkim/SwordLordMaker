using System.Collections.Generic;
using UnityEngine;

public class PixelSwordController : BaseSwordController
{
    [Header("■ [Pixel] 발사 설정")]
    public int SwordCountPerFire = 5;

    [Header("■ 타겟팅 설정")]
    public float MaxTargetDistance = 25f;

    [Header("■ Sword Stat")]
    [SerializeField] private string _swordStatId = "PIXEL_SWORD";

    private SwordStat _baseStat;
    private SwordStat _swordStat;
    private bool _isInitialized;

    public SwordStat SwordStat => _swordStat;

    private readonly List<PixelFlyingSword> _activeSwords = new List<PixelFlyingSword>();
    private readonly List<EnemyAI> _filteredEnemyBuffer = new List<EnemyAI>();

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
        foreach (PixelFlyingSword sword in _activeSwords)
        {
            if (sword != null)
            {
                sword.InitializeStat(_swordStat);
            }
        }
    }

    protected override void ResetSequence()
    {
        SpawnSwords();
    }

    private void SpawnSwords()
    {
        IReadOnlyList<EnemyAI> enemies = FindEnemies();
        IReadOnlyList<EnemyAI> validEnemies = FilterEnemiesByDistance(enemies);
        if (validEnemies == null || validEnemies.Count == 0) return;

        for (int i = 0; i < SwordCountPerFire; i++)
        {
            Transform target = GetRandomEnemyTarget(validEnemies);

            GameObject obj = Instantiate(SwordPrefab, transform.position, Quaternion.identity);
            PixelFlyingSword sword = obj.GetComponent<PixelFlyingSword>();

            if (sword)
            {
                _activeSwords.Add(sword);
                sword.Init(transform, target, () => OnSwordFinished(sword), _swordStat);
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

    private void OnSwordFinished(PixelFlyingSword sword)
    {
        _activeSwords.Remove(sword);
    }
}
