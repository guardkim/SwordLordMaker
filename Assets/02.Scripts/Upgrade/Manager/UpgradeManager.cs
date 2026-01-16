using System;
using System.Numerics;
using UnityEngine;

public class UpgradeManager : DontDestroySingleton<UpgradeManager>
{
    private IUpgradeRepository _repository;
    private PlayerUpgradeLevels _playerLevels;

    public event Action<string, int> OnUpgraded;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _playerLevels = _repository.LoadPlayerLevels();
    }

    private IUpgradeRepository CreateRepository()
    {
        return new UpgradeRepository();
    }

    public bool TryUpgrade(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null)
        {
            Debug.LogError($"[UpgradeManager] 강화 데이터를 찾을 수 없습니다: {upgradeId}");
            return false;
        }

        int currentLevel = _playerLevels.GetLevel(upgradeId);

        if (data.IsMaxLevel(currentLevel))
        {
            Debug.Log($"[UpgradeManager] 최대 레벨 도달: {upgradeId}");
            return false;
        }

        BigInteger cost = data.GetCost(currentLevel);

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[UpgradeManager] CurrencyManager가 없습니다.");
            return false;
        }

        if (!CurrencyManager.Instance.TrySpendGold(cost))
        {
            Debug.Log($"[UpgradeManager] 골드 부족: 필요 {CurrencyFormatter.FormatAbbreviated(cost)}");
            return false;
        }

        _playerLevels.IncrementLevel(upgradeId);
        _repository.SavePlayerLevels(_playerLevels);

        int newLevel = _playerLevels.GetLevel(upgradeId);
        OnUpgraded?.Invoke(upgradeId, newLevel);

        Debug.Log($"[UpgradeManager] 강화 성공: {upgradeId} Lv.{newLevel}");
        return true;
    }

    public int GetLevel(string upgradeId)
    {
        return _playerLevels.GetLevel(upgradeId);
    }
    public BigInteger GetBigIntBonus(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return BigInteger.Zero; // 0 대신 BigInteger.Zero 반환

        int level = _playerLevels.GetLevel(upgradeId);
    
        // data.GetTotalBonus도 내부적으로 BigInteger를 반환하도록 수정되어야 합니다!
        // 만약 data가 float만 뱉는다면 여기서 (BigInteger)캐스팅을 해야 하지만, 
        // 근본적으로는 data 쪽도 BigInteger를 지원해야 합니다.
        return data.GetTotalBigIntBonus(level); 
    }
    public float GetBonus(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return 0f;

        int level = _playerLevels.GetLevel(upgradeId);
        return data.GetTotalBonus(level);
    }

    public BigInteger GetCost(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return BigInteger.Zero;

        int currentLevel = _playerLevels.GetLevel(upgradeId);
        return data.GetCost(currentLevel);
    }

    public int GetMaxLevel(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        return data?.MaxLevel ?? 0;
    }

    public bool IsMaxLevel(string upgradeId)
    {
        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return true;

        int currentLevel = _playerLevels.GetLevel(upgradeId);
        return data.IsMaxLevel(currentLevel);
    }

    public UpgradeData GetUpgradeData(string upgradeId)
    {
        return _repository.GetUpgradeData(upgradeId);
    }


    // 플레이어 스탯 보너스 조회
    public BigInteger GetPlayerHealthBonus()
    {
        return GetBigIntBonus(UpgradeId.PlayerHealth);
    }

    public float GetPlayerMoveSpeedBonus()
    {
        return GetBonus(UpgradeId.PlayerMoveSpeed);
    }

    // 검 스탯 보너스 적용
    public SwordStat ApplyUpgrades(SwordStat baseStat)
    {
        return baseStat with
        {
            AttackDamage = baseStat.AttackDamage + GetBigIntBonus(UpgradeId.SwordAttackDamage),
            Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(UpgradeId.SwordCooldown)),
            MoveSpeed = baseStat.MoveSpeed + GetBonus(UpgradeId.SwordMoveSpeed),
            CritDamageMultiplier = baseStat.CritDamageMultiplier + GetBonus(UpgradeId.SwordCritDamage),
            CritChance = Mathf.Min(1f, baseStat.CritChance + GetBonus(UpgradeId.SwordCritChance))
        };
    }
}
