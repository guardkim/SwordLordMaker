using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AdelFlyingSwordController : BaseSwordController
{
    // SwordPrefab은 부모에 있음

    [Header("■ [Adel] 사출 설정")]
    public float SpawnForce = 10f;
    public int MaxSwordCount = 6;
    public float AttackDelay = 0.2f;

    [Header("■ 타겟팅 설정")]
    public float MaxTargetDistance = 25f;

    [Header("■ Sword Stat")]
    [SerializeField] private string _swordStatId = "ADEL_SWORD";
    private SwordStat _swordStat;
    public SwordStat SwordStat => _swordStat;

    private readonly List<AdelFlyingSword> _activeSwords = new List<AdelFlyingSword>();
    private int _currentAttackerOrderIndex; 
    private int _spawnTotalCount;
    private float _delayTimer;

    // [최적화용 변수 추가]
    private float _searchTimer;
    private const float SearchInterval = 0.2f; // 0.2초마다 탐색

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

            // 기존 활성 검들에도 새 스탯 즉시 적용
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

    // 부모 추상 메서드 구현
    protected override void ResetSequence()
    {
        SpawnDualSwords();
    }

    private void Update()
    {
        if (_delayTimer > 0) _delayTimer -= Time.deltaTime;

        // [수정됨] 매 프레임 실행하지 않고 0.2초마다 실행
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

        GameObject[] enemies = FindEnemies();
        GameObject[] validEnemies = FilterEnemiesByDistance(enemies);
        if (validEnemies.Length == 0) return;

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

    private GameObject[] FilterEnemiesByDistance(GameObject[] enemies)
    {
        Vector3 playerPos = transform.position;
        return enemies
            .Where(e => Vector3.Distance(e.transform.position, playerPos) <= MaxTargetDistance)
            .ToArray();
    }

    public void RequestNewTarget(AdelFlyingSword sword)
    {
        GameObject[] enemies = FindEnemies();
        GameObject[] validEnemies = FilterEnemiesByDistance(enemies);

        if (validEnemies.Length > 0)
        {
            sword.SetTarget(validEnemies[Random.Range(0, validEnemies.Length)].transform);
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
        if (FindEnemies().Length == 0) return false;
        if (_delayTimer > 0) return false;
        return swordOrderIndex == _currentAttackerOrderIndex;
    }

    public void NextTurn()
    {
        _delayTimer = AttackDelay; 
        IncrementTurnIndex();
    }

    private void IncrementTurnIndex()
    {
        if (_activeSwords.Count == 0) return;

        _activeSwords.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
        AdelFlyingSword nextSword = _activeSwords.FirstOrDefault(s => s.OrderIndex > _currentAttackerOrderIndex);
        
        if (!nextSword)
            nextSword = _activeSwords[0];

        _currentAttackerOrderIndex = nextSword.OrderIndex;
    }

    private void RetargetSwords()
    {
        if (_activeSwords.Count == 0) return;
        if (_activeSwords.All(s => s.HasTarget())) return;

        GameObject[] enemies = FindEnemies();
        GameObject[] validEnemies = FilterEnemiesByDistance(enemies);

        if (validEnemies.Length == 0)
        {
            foreach (var s in _activeSwords) s.SetTarget(null);
            return;
        }

        foreach (var s in _activeSwords)
        {
            if (!s.HasTarget())
            {
                s.SetTarget(validEnemies[Random.Range(0, validEnemies.Length)].transform);
            }
        }
    }
}