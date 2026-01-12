using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _movement;

    [Header("▼ 애니메이션 파라미터")]
    [SerializeField] private string _speedParam = "Speed";

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
        UpdateAnimationParameters();
    }

    private void UpdateAnimationParameters()
    {
        if (_animator == null || _movement == null)
        {
            return;
        }

        float speed = _movement.MoveDirection.magnitude;
        _animator.SetFloat(_speedParam, speed);
    }
}
