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
        var repository = new SwordStatRepository();
        _swordStat = repository.GetById(_swordStatId);
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