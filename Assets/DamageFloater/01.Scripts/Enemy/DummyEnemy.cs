using System.Numerics;
using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private string _maxHealthString = "100";
    [SerializeField] private bool _randomMove = true;

    private BigInteger _maxHealth;
    private BigInteger _currentHealth;
    private float _directionChangeTimer;
    
    private void Start()
    {
        _maxHealth = BigInteger.Parse(_maxHealthString);
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(BigInteger damage, bool isCrit)
    {
        _currentHealth -= damage;
        if (_currentHealth < BigInteger.Zero)
        {
            _currentHealth = BigInteger.Zero;
        }
        
        // 피격 이펙트 (깜빡임)
        StartCoroutine(FlashEffect());
        
        if (_currentHealth <= BigInteger.Zero)
        {
            Die();
        }
    }
    
    private System.Collections.IEnumerator FlashEffect()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            Color original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
        }
    }
    
    private void Die()
    {
        // 골드 지급
        CurrencyManager.Instance.AddGold(100);

        // 파티클 등 사망 이펙트
        Destroy(gameObject);
    }

}
