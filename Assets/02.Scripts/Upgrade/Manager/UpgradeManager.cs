using System;
using UnityEngine;

public class UpgradeManager : DontDestroySingleton<UpgradeManager>
{
    private IUpgradeRepository _repository;
    private PlayerUpgradeLevels _playerLevels;

    public event Action<string, int> OnUpgraded;
    public event Action OnInitialized;

    public bool IsReady => _repository != null;

    protected override void Initialize()
    {
        Debug.Log($"[UpgradeManager] Initialize 호출됨");
        Debug.Log($"[UpgradeManager] PlayerSessionManager.IsLoggedIn: {PlayerSessionManager.Instance.IsLoggedIn}");
        Debug.Log($"[UpgradeManager] CurrentPlayerName: '{PlayerSessionManager.Instance.CurrentPlayerName}'");

        PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;

        if (PlayerSessionManager.Instance.IsLoggedIn)
        {
            Debug.Log("[UpgradeManager] 이미 로그인됨 → InitializeRepository 호출");
            InitializeRepository();
        }
        else
        {
            Debug.Log("[UpgradeManager] 아직 로그인 안 됨 → OnLoginCompleted 대기");
        }
    }

    private void OnDestroy()
    {
        if (PlayerSessionManager.HasInstance)
        {
            PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
        }
    }

    private void OnLoginCompleted()
    {
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        string playerName = PlayerSessionManager.Instance.CurrentPlayerName;
        Debug.Log($"[UpgradeManager] Repository 초기화: '{playerName}'");

        _repository = new UpgradeRepository(playerName);
        _playerLevels = _repository.LoadPlayerLevels();

        if (_playerLevels.IsEmpty())
        {
            _repository.SavePlayerLevels(_playerLevels);
        }

        OnInitialized?.Invoke();
    }

    public bool TryUpgrade(string upgradeId)
    {
        if (_repository == null)
        {
            Debug.LogWarning("[UpgradeManager] 아직 초기화되지 않았습니다.");
            return false;
        }

        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null)
        {
            Debug.LogError($"[UpgradeManager] 강화 데이터를 찾을 수 없습니다: {upgradeId}");
            return false;
        }

        int currentLevel = _playerLevels.GetLevel(upgradeId);
        double cost = data.GetCost(currentLevel);

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[UpgradeManager] CurrencyManager가 없습니다.");
            return false;
        }

        if (!CurrencyManager.Instance.TrySpendGold(cost))
        {
            Debug.Log($"[UpgradeManager] 골드 부족: 필요 {CurrencyFormatter.FormatKorean(cost)}");
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
        if (_playerLevels == null) return 0;
        return _playerLevels.GetLevel(upgradeId);
    }

    public double GetDoubleBonus(string upgradeId)
    {
        if (_repository == null) return 0;

        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return 0;

        int level = _playerLevels?.GetLevel(upgradeId) ?? 0;
        return data.GetTotalDoubleBonus(level);
    }

    public float GetBonus(string upgradeId)
    {
        if (_repository == null) return 0f;

        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return 0f;

        int level = _playerLevels?.GetLevel(upgradeId) ?? 0;
        return data.GetTotalBonus(level);
    }

    public double GetCost(string upgradeId)
    {
        if (_repository == null) return 0;

        UpgradeData data = _repository.GetUpgradeData(upgradeId);
        if (data == null) return 0;

        int currentLevel = _playerLevels?.GetLevel(upgradeId) ?? 0;
        return data.GetCost(currentLevel);
    }

    public UpgradeData GetUpgradeData(string upgradeId)
    {
        if (_repository == null) return null;
        return _repository.GetUpgradeData(upgradeId);
    }

    // 플레이어 스탯 보너스 조회
    public double GetPlayerHealthBonus()
    {
        return GetDoubleBonus(UpgradeId.PlayerHealth.ToKey());
    }

    public float GetPlayerMoveSpeedBonus()
    {
        return GetBonus(UpgradeId.PlayerMoveSpeed.ToKey());
    }

    // 검 스탯 보너스 적용
    public SwordStat ApplyUpgrades(SwordStat baseStat)
    {
        if (_repository == null)
        {
            return baseStat;
        }

        return baseStat with
        {
            AttackDamage = baseStat.AttackDamage + GetDoubleBonus(UpgradeId.SwordAttackDamage.ToKey()),
            Cooldown = Mathf.Max(0.1f, baseStat.Cooldown - GetBonus(UpgradeId.SwordCooldown.ToKey())),
            MoveSpeed = baseStat.MoveSpeed + GetBonus(UpgradeId.SwordMoveSpeed.ToKey()),
            CritDamageMultiplier = baseStat.CritDamageMultiplier + GetBonus(UpgradeId.SwordCritDamage.ToKey()),
            CritChance = Mathf.Min(1f, baseStat.CritChance + GetBonus(UpgradeId.SwordCritChance.ToKey()))
        };
    }
}
