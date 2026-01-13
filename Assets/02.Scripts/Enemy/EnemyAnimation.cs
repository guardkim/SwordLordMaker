using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyAI _enemyAI;

    [Header("▼ 애니메이션 파라미터")]
    [SerializeField] private string _isMovingParam = "IsMoving";
    [SerializeField] private string _isAttackingParam = "IsAttacking";
    [SerializeField] private string _isDeadParam = "IsDead";
    [SerializeField] private string _hitTriggerParam = "Hit";

    private bool _isDead;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_enemyAI == null)
        {
            _enemyAI = GetComponent<EnemyAI>();
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        UpdateAnimationParameters();
    }

    private void UpdateAnimationParameters()
    {
        if (_animator == null || _enemyAI == null) return;

        // Hit 상태에서는 이동/공격 애니메이션 즉시 중단
        if (_enemyAI.IsHit)
        {
            _animator.SetBool(_isMovingParam, false);
            _animator.SetBool(_isAttackingParam, false);
            return;
        }

        _animator.SetBool(_isMovingParam, _enemyAI.IsMoving);
        _animator.SetBool(_isAttackingParam, _enemyAI.IsAttacking);
    }

    public void TriggerHit()
    {
        if (_animator == null || _isDead)
        {
            return;
        }

        _animator.SetTrigger(_hitTriggerParam);
    }

    public void Die()
    {
        if (_animator == null || _isDead)
        {
            return;
        }

        _isDead = true;
        _animator.SetBool(_isDeadParam, true);
    }

    public void ResetAnimation()
    {
        _isDead = false;

        if (_animator != null)
        {
            _animator.SetBool(_isMovingParam, false);
            _animator.SetBool(_isAttackingParam, false);
            _animator.SetBool(_isDeadParam, false);
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}
