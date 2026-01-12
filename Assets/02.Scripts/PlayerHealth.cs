using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("▼ 체력 설정")]
    [SerializeField] private int _maxHealth = 100;

    private int _currentHealth;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    public event Action<int, int> OnHealthChanged;  // (current, max)
    public event Action OnDeath;

    private void Start()
    {
        _currentHealth = _maxHealth;
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
        OnDeath?.Invoke();
        // TODO: 사망 처리 (게임 오버 등)
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
