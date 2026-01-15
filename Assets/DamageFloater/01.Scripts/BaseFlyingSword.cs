using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public abstract class BaseFlyingSword : MonoBehaviour
{
    [Header("■ [Base] 높이 제한")]
    public float MinHeight = 0.5f;

    protected Transform TargetEnemy;
    protected SwordStat _stat;

    public virtual void InitializeStat(SwordStat stat)
    {
        _stat = stat;
    }

    protected Vector3 ClampHeight(Vector3 position)
    {
        position.y = Mathf.Max(position.y, MinHeight);
        return position;
    }

    protected bool TryDealDamage(Collider other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (other.CompareTag("Player")) return false;
        if (target != null)
        {
            if (_stat == null)
            {
                target.TakeDamage(10, false);
                return true;
            }

            bool isCritical = Random.value < _stat.CritChance;
            BigInteger finalDamage = _stat.CalculateDamage(isCritical);
            target.TakeDamage(finalDamage, isCritical);
            return true;
        }
        return false;
    }

    protected bool TryDealDamage(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            if (_stat == null)
            {
                target.TakeDamage(10, false);
                return true;
            }

            bool isCritical = Random.value < _stat.CritChance;
            BigInteger finalDamage = _stat.CalculateDamage(isCritical);
            target.TakeDamage(finalDamage, isCritical);
            return true;
        }
        return false;
    }
}
