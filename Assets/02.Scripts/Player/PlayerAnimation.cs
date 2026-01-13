using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _movement;

    [Header("▼ 애니메이션 파라미터")]
    [SerializeField] private string _speedParam = "Speed";
    [SerializeField] private string _isDeadParam = "IsDead";

    private bool _isDead;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_movement == null)
        {
            _movement = GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (_isDead) return;

        UpdateAnimationParameters();
    }

    private void UpdateAnimationParameters()
    {
        if (_animator == null || _movement == null) return;

        float speed = _movement.GetCurrentSpeed();
        _animator.SetFloat(_speedParam, speed);
    }

    public void Die()
    {
        if (_animator == null || _isDead) return;

        _isDead = true;
        _animator.SetBool(_isDeadParam, true);
    }

    public void Revive()
    {
        if (_animator == null || !_isDead) return;

        _isDead = false;
        _animator.SetBool(_isDeadParam, false);
        _animator.Rebind();
        _animator.Update(0f);
    }
}
