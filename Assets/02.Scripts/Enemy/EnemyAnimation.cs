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
        if (_animator == null || _enemyAI == null)
        {
            return;
        }

        EnemyAI.State state = _enemyAI.CurrentState;

        bool isMoving = state == EnemyAI.State.Chase;
        bool isAttacking = state == EnemyAI.State.Attack;

        _animator.SetBool(_isMovingParam, isMoving);
        _animator.SetBool(_isAttackingParam, isAttacking);
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
}
