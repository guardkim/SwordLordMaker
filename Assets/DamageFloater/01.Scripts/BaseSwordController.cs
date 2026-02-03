using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSwordController : MonoBehaviour
{
    [Header("▼ [Base] 프리팹 설정")]
    public GameObject SwordPrefab;

    // 외부(Manager)에서 호출하여 발사체를 생성하는 진입점
    public void Fire()
    {
        StopSequence();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(ESfxId.SwordAttack);
        }

        ResetSequence();
    }

    // 강제로 공격을 중단해야 할 때 호출 (모드 변경 등)
    public virtual void StopSequence()
    {
        StopAllCoroutines();
    }

    // 자식 클래스마다 다른 발사 로직
    protected abstract void ResetSequence();

    // EnemySpawner의 AliveEnemies 직접 접근으로 LINQ 제거
    protected IReadOnlyList<EnemyAI> FindEnemies()
    {
        return EnemySpawner.Instance?.AliveEnemies;
    }

    protected Transform GetRandomEnemyTarget(IReadOnlyList<EnemyAI> enemies)
    {
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[Random.Range(0, enemies.Count)].transform;
    }
}
