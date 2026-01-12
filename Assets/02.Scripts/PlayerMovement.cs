using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("▼ 이동 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;

    private Vector3 _moveDirection;
    private CharacterController _controller;

    public Vector3 MoveDirection => _moveDirection;
    public bool IsMoving => _moveDirection.magnitude > 0.1f;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleInput();
        Move();
        Rotate();
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 쿼터뷰 기준 이동 방향 계산 (45도 회전).
        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        _moveDirection = Quaternion.Euler(0f, 45f, 0f) * input;
    }

    private void Move()
    {
        if (_controller == null)
        {
            return;
        }

        Vector3 velocity = _moveDirection * _moveSpeed;
        velocity.y = -9.81f;
        _controller.Move(velocity * Time.deltaTime);
    }

    private void Rotate()
    {
        if (!IsMoving)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}
