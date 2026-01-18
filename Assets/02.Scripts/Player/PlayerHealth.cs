using System;
using System.Numerics;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("▼ 참조")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private PlayerMovement _playerMovement;

    private BigInteger _baseMaxHealth;
    private BigInteger _maxHealth;
    private BigInteger _currentHealth;
    private bool _isDead;

    public BigInteger MaxHealth => _maxHealth;
    public BigInteger CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

    public event Action<BigInteger, BigInteger> OnHealthChanged;  // (current, max)
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
            _baseMaxHealth = new BigInteger(100);
            UnityEngine.Debug.LogWarning("[PlayerHealth] PlayerStatManager가 없어 기본값 사용");
        }

        if (_playerAnimation == null)
        {
            _playerAnimation = GetComponent<PlayerAnimation>();
        }

        if (_playerMovement == null)
        {
            _playerMovement = GetComponent<PlayerMovement>();
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
        if (upgradeId == UpgradeId.PlayerHealth.ToKey())
        {
            BigInteger oldMaxHealth = _maxHealth;
            ApplyUpgradeBonus();

            // 증가한 만큼 현재 체력도 증가
            BigInteger healthIncrease = _maxHealth - oldMaxHealth;
            if (healthIncrease > BigInteger.Zero)
            {
                _currentHealth += healthIncrease;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }

    private void ApplyUpgradeBonus()
    {
        BigInteger bonus = BigInteger.Zero;
        if (UpgradeManager.Instance != null)
        {
            bonus = UpgradeManager.Instance.GetPlayerHealthBonus();
        }
        _maxHealth = _baseMaxHealth + bonus;
    }

    public void TakeDamage(BigInteger damage, bool isCrit)
    {
        if (IsDead)
        {
            return;
        }

        _currentHealth -= damage;
        if (_currentHealth < BigInteger.Zero)
        {
            _currentHealth = BigInteger.Zero;
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= BigInteger.Zero)
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
            _playerMovement.SetEnabled(true);
        }

        // 애니메이션 리셋
        if (_playerAnimation != null)
        {
            _playerAnimation.Revive();
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void Heal(BigInteger amount)
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

    public void SetMaxHealth(BigInteger maxHealth, bool healToFull = false)
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
