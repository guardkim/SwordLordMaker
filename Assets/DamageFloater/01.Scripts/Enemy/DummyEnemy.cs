using UnityEngine;

public interface IDamageable
{
    public void TakeDamage(int damage, bool isCrit);
}
public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private bool _randomMove = true;
    
    private float _currentHealth;
    private float _directionChangeTimer;
    
    private void Start()
    {
        _currentHealth = _maxHealth;
    }
    
    public void TakeDamage(int damage, bool isCrit)
    {
        _currentHealth -= damage;
        
        // 피격 이펙트 (깜빡임)
        StartCoroutine(FlashEffect());
        
        if (_currentHealth <= 0)
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
        // 파티클 등 사망 이펙트
        Destroy(gameObject);
    }

}
