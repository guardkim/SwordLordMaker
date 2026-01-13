using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("▼ 체력 설정")]
    [SerializeField] private int _maxHealth = 100;

    [Header("▼ 참조")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private PlayerMovement _playerMovement;

    private int _currentHealth;
    private bool _isDead;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

    public event Action<int, int> OnHealthChanged;  // (current, max)
    public event Action OnDeath;

    private void Awake()
    {
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
        _currentHealth = _maxHealth;
        _isDead = false;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(int damage, bool isCrit)
    {
        if (IsDead)
        {
            return;
        }

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);

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

        // 사망 이벤트 발생 (게임 오버 처리 등 외부에서 구독)
        OnDeath?.Invoke();
    }

    public void Revive(int healthAmount = -1)
    {
        if (!_isDead) return;

        _isDead = false;
        _currentHealth = healthAmount > 0 ? healthAmount : _maxHealth;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);

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

    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void SetMaxHealth(int maxHealth, bool healToFull = false)
    {
        _maxHealth = maxHealth;

        if (healToFull)
        {
            _currentHealth = _maxHealth;
        }
        else
        {
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
