using UnityEngine;

/// <summary>
/// 모든 비행 검의 최상위 부모 클래스입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class BaseFlyingSword : MonoBehaviour
{
    [Header("■ [Base] 공통 전투 설정")]
    public int Damage = 10;

    // 자식 클래스들이 공유해서 사용할 타겟 변수
    protected Transform TargetEnemy;

    /// <summary>
    /// 공통 데미지 처리 로직 (크리티컬 50% 포함)
    /// </summary>
    /// <returns>데미지 인터페이스(IDamageable)가 있어서 타격에 성공하면 true</returns>
    protected bool TryDealDamage(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            // 치명타 로직 (기존 로직 유지: 50% 확률)
            bool isCritical = (Random.Range(0, 100) % 2 != 0);
            int finalDamage = isCritical ? Damage * 2 : Damage;

            target.TakeDamage(finalDamage, isCritical);
            return true;
        }
        return false;
    }
}