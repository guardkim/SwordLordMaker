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
    private SwordStat _swordStat;
    public SwordStat SwordStat => _swordStat;

    private readonly List<PixelFlyingSword> _activeSwords = new List<PixelFlyingSword>();

    private void Awake()
    {
        LoadAndApplyUpgrades();
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

    private void OnUpgradeManagerInitialized()
    {
        LoadAndApplyUpgrades();

        foreach (var sword in _activeSwords)
        {
            if (sword != null)
            {
                sword.InitializeStat(_swordStat);
            }
        }
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        if (upgradeId.StartsWith("Sword_"))
        {
            LoadAndApplyUpgrades();

            foreach (var sword in _activeSwords)
            {
                if (sword != null)
                {
                    sword.InitializeStat(_swordStat);
                }
            }
        }
    }

    private void LoadAndApplyUpgrades()
    {
        var repository = new SwordStatRepository();
        SwordStat baseStat = repository.GetById(_swordStatId);

        if (UpgradeManager.Instance != null)
        {
            _swordStat = UpgradeManager.Instance.ApplyUpgrades(baseStat);
        }
        else
        {
            _swordStat = baseStat;
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
            PixelFlyingSword sword = obj.GetComponent<PixelFlyingSword>();

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

    private void OnSwordFinished(PixelFlyingSword sword)
    {
        _activeSwords.Remove(sword);
    }
}
