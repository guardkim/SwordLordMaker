using System.Linq;
using UnityEngine;

public abstract class BaseSwordController : MonoBehaviour
{
    [Header("▼ [Base] 프리팹 설정")]
    public GameObject SwordPrefab;

    // 입력 감지 로직(Update) 제거 -> Manager가 제어함

    /// <summary>
    /// 외부(Manager)에서 호출하여 발사체를 생성하는 진입점
    /// </summary>
    public void Fire()
    {
        // 기존에 실행 중이던 시퀀스나 코루틴이 있다면 정지
        StopSequence();

        // 공격 시작 효과음
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.SwordAttack);
        }

        // 자식 클래스의 고유 로직 실행
        ResetSequence();
    }

    /// <summary>
    /// 강제로 공격을 중단해야 할 때 호출 (모드 변경 등)
    /// </summary>
    public virtual void StopSequence()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 자식 클래스마다 다른 발사 로직 (기존 ResetSequence 유지)
    /// </summary>
    protected abstract void ResetSequence();

    // --- 공통 유틸리티 ---
    protected GameObject[] FindEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy")
            .Where(e =>
            {
                var enemy = e.GetComponent<EnemyAI>();
                return enemy == null || !enemy.IsDead;
            })
            .ToArray();
    }

    protected Transform GetRandomEnemyTarget(GameObject[] enemies)
    {
        if (enemies == null || enemies.Length == 0) return null;
        return enemies[Random.Range(0, enemies.Length)].transform;
    }
}