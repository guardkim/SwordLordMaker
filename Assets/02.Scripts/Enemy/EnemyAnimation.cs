using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private Animator _animator;

    [Header("▼ 애니메이션 파라미터")]
    [SerializeField] private string _isMovingParam = "IsMoving";
    [SerializeField] private string _isAttackingParam = "IsAttacking";
    [SerializeField] private string _isDeadParam = "IsDead";
    [SerializeField] private string _hitTriggerParam = "Hit";
    [SerializeField] private string _skillTriggerParam = "Skill";

    private bool _isDead;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (_animator == null || _isDead) return;
        _animator.SetBool(_isMovingParam, isMoving);
    }

    public void SetAttacking(bool isAttacking)
    {
        if (_animator == null || _isDead) return;
        _animator.SetBool(_isAttackingParam, isAttacking);
    }

    public void StopAllActions()
    {
        if (_animator == null) return;
        _animator.SetBool(_isMovingParam, false);
        _animator.SetBool(_isAttackingParam, false);
    }

    public void TriggerHit()
    {
        if (_animator == null || _isDead)
        {
            return;
        }

        _animator.SetTrigger(_hitTriggerParam);
    }

    public void TriggerSkill()
    {
        if (_animator == null || _isDead)
        {
            return;
        }

        _animator.SetTrigger(_skillTriggerParam);
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
