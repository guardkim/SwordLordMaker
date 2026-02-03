using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("▼ 참조")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private PlayerAutoMovement _playerMovement;

    private double _baseMaxHealth;
    private double _maxHealth;
    private double _currentHealth;
    private bool _isDead;

    public double MaxHealth => _maxHealth;
    public double CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

    public event Action<double, double> OnHealthChanged;  // (current, max)
    public event Action OnDeath;

    private void Awake()
    {
        // PlayerStatManager에서 기본값 로드
        if (PlayerStatManager.Instance != null)
        {
            _baseMaxHealth = PlayerStatManager.Instance.BaseMaxHealth;
        }
        else
        {
            // 폴백: Manager가 없으면 기본값 사용
            _baseMaxHealth = 100;
            UnityEngine.Debug.LogWarning("[PlayerHealth] PlayerStatManager가 없어 기본값 사용");
        }

        if (_playerAnimation == null)
        {
            _playerAnimation = GetComponent<PlayerAnimation>();
        }

        if (_playerMovement == null)
        {
            _playerMovement = GetComponent<PlayerAutoMovement>();
        }
    }

    private void Start()
    {
        ApplyUpgradeBonus();
        _currentHealth = _maxHealth;
        _isDead = false;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        // GameManager에 등록 (의존성 주입)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        // 강화 시 스탯 갱신 구독
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized += OnUpgradeManagerInitialized;
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
        double oldMaxHealth = _maxHealth;
        ApplyUpgradeBonus();

        double healthIncrease = _maxHealth - oldMaxHealth;
        if (healthIncrease > 0)
        {
            _currentHealth += healthIncrease;
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        if (upgradeId == EUpgradeId.PlayerHealth.ToKey())
        {
            double oldMaxHealth = _maxHealth;
            ApplyUpgradeBonus();

            // 증가한 만큼 현재 체력도 증가
            double healthIncrease = _maxHealth - oldMaxHealth;
            if (healthIncrease > 0)
            {
                _currentHealth += healthIncrease;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }

    private void ApplyUpgradeBonus()
    {
        double bonus = 0;
        if (UpgradeManager.Instance != null)
        {
            bonus = UpgradeManager.Instance.GetPlayerHealthBonus();
        }
        _maxHealth = _baseMaxHealth + bonus;
    }

    public void TakeDamage(double damage, bool isCrit)
    {
        if (IsDead)
        {
            return;
        }

        _currentHealth -= damage;
        if (_currentHealth < 0)
        {
            _currentHealth = 0;
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        // 이동 비활성화
        if (_playerMovement != null)
        {
            _playerMovement.SetEnabled(false);
        }

        // 사망 애니메이션
        if (_playerAnimation != null)
        {
            _playerAnimation.Die();
        }

        // 사망 이벤트 발생 (GameManager가 구독하여 부활 처리)
        OnDeath?.Invoke();
    }

    public void Revive()
    {
        if (!_isDead) return;

        _isDead = false;
        _currentHealth = _maxHealth;

        // 이동 활성화
        if (_playerMovement != null)
        {
            _playerMovement.Revive();
        }

        // 애니메이션 리셋
        if (_playerAnimation != null)
        {
            _playerAnimation.Revive();
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void Heal(double amount)
    {
        if (IsDead)
        {
            return;
        }

        _currentHealth += amount;
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void SetMaxHealth(double maxHealth, bool healToFull = false)
    {
        _maxHealth = maxHealth;

        if (healToFull)
        {
            _currentHealth = _maxHealth;
        }
        else if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
