using UnityEngine;

// 모든 비행 검의 최상위 부모 클래스. 2D/3D 환경 모두 지원.
public abstract class BaseFlyingSword : MonoBehaviour
{
    [Header("■ [Base] 공통 전투 설정")]
    public int Damage = 10;

    [Header("■ [Base] 높이 제한")]
    public float MinHeight = 0.5f;

    // 자식 클래스들이 공유해서 사용할 타겟 변수
    protected Transform TargetEnemy;

    // Y 좌표가 MinHeight 이하로 내려가지 않도록 클램핑.
    protected Vector3 ClampHeight(Vector3 position)
    {
        position.y = Mathf.Max(position.y, MinHeight);
        return position;
    }

    // 공통 데미지 처리 로직 (크리티컬 50% 포함). 3D Collider용.
    protected bool TryDealDamage(Collider other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            bool isCritical = (Random.Range(0, 100) % 2 != 0);
            int finalDamage = isCritical ? Damage * 2 : Damage;

            target.TakeDamage(finalDamage, isCritical);
            return true;
        }
        return false;
    }

    // 공통 데미지 처리 로직 (크리티컬 50% 포함). 2D Collider용.
    protected bool TryDealDamage(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            bool isCritical = (Random.Range(0, 100) % 2 != 0);
            int finalDamage = isCritical ? Damage * 2 : Damage;

            target.TakeDamage(finalDamage, isCritical);
            return true;
        }
        return false;
    }
}