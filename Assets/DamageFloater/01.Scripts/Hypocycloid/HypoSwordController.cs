using System.Collections.Generic;
using UnityEngine;

public class HypoSwordController : BaseSwordController
{
    [Header("■ [Hypo] 발사 설정")]
    public int SwordCountPerFire = 5;

    [Header("■ 타겟팅 설정")]
    public float MaxTargetDistance = 25f;

    [Header("■ Sword Stat")]
    [SerializeField] private string _swordStatId = "HYPO_SWORD";

    private SwordStat _baseStat;
    private SwordStat _swordStat;
    private bool _isInitialized;

    public SwordStat SwordStat => _swordStat;

    private readonly List<HypoFlyingSword> _activeSwords = new List<HypoFlyingSword>();

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

        _swordStat = new SwordStat(
            _baseStat.Id,
            _baseStat.AttackDamage,
            _baseStat.Cooldown,
            _baseStat.MoveSpeed,
            _baseStat.CritDamageMultiplier,
            _baseStat.CritChance
        );

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
            _swordStat.AttackDamage = _baseStat.AttackDamage;
            _swordStat.Cooldown = _baseStat.Cooldown;
            _swordStat.MoveSpeed = _baseStat.MoveSpeed;
            _swordStat.CritDamageMultiplier = _baseStat.CritDamageMultiplier;
            _swordStat.CritChance = _baseStat.CritChance;
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
        foreach (var sword in _activeSwords)
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
        GameObject[] enemies = FindEnemies();
        GameObject[] validEnemies = FilterEnemiesByDistance(enemies);
        if (validEnemies.Length == 0) return;

        for (int i = 0; i < SwordCountPerFire; i++)
        {
            Transform target = GetRandomEnemyTarget(validEnemies);

            GameObject obj = Instantiate(SwordPrefab, transform.position, Quaternion.identity);
            HypoFlyingSword sword = obj.GetComponent<HypoFlyingSword>();

            if (sword)
            {
                _activeSwords.Add(sword);
                sword.Init(transform, target, () => OnSwordFinished(sword), _swordStat);
            }
        }
    }

    private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
    {
        var result = new List<GameObject>();
        Vector3 playerPos = transform.position;

        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(enemy.transform.position, playerPos) <= MaxTargetDistance)
            {
                result.Add(enemy);
            }
        }

        return result.ToArray();
    }

    private void OnSwordFinished(HypoFlyingSword sword)
    {
        _activeSwords.Remove(sword);
    }
}
