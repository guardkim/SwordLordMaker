using UnityEngine;

public class HypoSwordController : BaseSwordController
{
    // SwordPrefab은 부모에 있음

    [Header("■ 웨이브 설정")]
    public int SwordCount = 6;
    public int MaxWaves = 3;

    [Header("■ Sword Stat")]
    [SerializeField] private string _swordStatId = "HYPO_SWORD";
    private SwordStat _swordStat;
    public SwordStat SwordStat => _swordStat;

    private int _currentWaveIndex;
    private int _finishedSwordsInWave;

    private void Awake()
    {
        LoadAndApplyUpgrades();
    }

    private void Start()
    {
        // 강화 이벤트 구독
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
        }
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
        }
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        // 검 관련 강화 시 스탯 재적용
        if (upgradeId.StartsWith("Sword_"))
        {
            LoadAndApplyUpgrades();
        }
    }

    private void LoadAndApplyUpgrades()
    {
        var repository = new SwordStatRepository();
        SwordStat baseStat = repository.GetById(_swordStatId);

        // 강화 보너스 적용
        if (UpgradeManager.Instance != null)
        {
            _swordStat = UpgradeManager.Instance.ApplyUpgrades(baseStat);
        }
        else
        {
            _swordStat = baseStat;
        }
    }

    // 부모의 추상 메서드 구현 (Update의 Space 입력 시 호출됨)
    protected override void ResetSequence()
    {
        StopAllCoroutines(); 
        _currentWaveIndex = 0;
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (_currentWaveIndex >= MaxWaves) 
        {
            Debug.Log("Hypo: All waves completed!");
            return;
        }
        
        GameObject[] enemies = FindEnemies(); // 부모 메서드
        if (enemies.Length == 0) return;

        Debug.Log($"Hypo Wave Start: {_currentWaveIndex + 1} / {MaxWaves}");
        
        _finishedSwordsInWave = 0;

        for (int i = 0; i < SwordCount; i++)
        {
            GameObject obj = Instantiate(SwordPrefab, transform.position, Quaternion.identity);
            HypoFlyingSword sword = obj.GetComponent<HypoFlyingSword>();
            
            if (sword)
            {
                Transform randomTarget = GetRandomEnemyTarget(enemies); // 부모 메서드
                sword.Init(transform, randomTarget, OnSwordFinished, _swordStat);
            }
        }
        
        _currentWaveIndex++;
    }

    private void OnSwordFinished()
    {
        _finishedSwordsInWave++;
        if (_finishedSwordsInWave >= SwordCount)
        {
            Invoke(nameof(SpawnWave), 1.0f);
        }
    }
}