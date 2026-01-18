using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float GRAVITY = -9.81f;
    private const float MOVING_THRESHOLD = 0.1f;

    [Header("▼ 이동 설정")]
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _quarterViewYaw = 45f;

    private float _baseMoveSpeed;
    private float _moveSpeed;
    private Vector3 _moveDirection;
    private CharacterController _controller;
    private bool _isEnabled = true;

    public Vector3 MoveDirection => _moveDirection;
    public bool IsMoving => _moveDirection.magnitude > MOVING_THRESHOLD;

    public float GetCurrentSpeed()
    {
        return _moveDirection.magnitude;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // PlayerStatManager에서 기본값 로드
        if (PlayerStatManager.Instance != null)
        {
            _baseMoveSpeed = PlayerStatManager.Instance.BaseMoveSpeed;
        }
        else
        {
            // 폴백: Manager가 없으면 기본값 사용
            _baseMoveSpeed = 5f;
            Debug.LogWarning("[PlayerMovement] PlayerStatManager가 없어 기본값 사용");
        }
    }

    private void Start()
    {
        ApplyUpgradeBonus();

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
        }
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
        }
    }

    private void OnUpgradeChanged(string upgradeId, int newLevel)
    {
        if (upgradeId == UpgradeId.PlayerMoveSpeed.ToKey())
        {
            ApplyUpgradeBonus();
        }
    }

    private void ApplyUpgradeBonus()
    {
        float bonus = 0f;
        if (UpgradeManager.Instance != null)
        {
            bonus = UpgradeManager.Instance.GetPlayerMoveSpeedBonus();
        }
        _moveSpeed = _baseMoveSpeed + bonus;
    }

    private void Update()
    {
        if (!_isEnabled)
        {
            _moveDirection = Vector3.zero;
            return;
        }

        HandleInput();
        Move();
        Rotate();
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;

        if (!enabled)
        {
            _moveDirection = Vector3.zero;
        }
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 쿼터뷰 기준 이동 방향 계산
        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        _moveDirection = Quaternion.Euler(0f, _quarterViewYaw, 0f) * input;
    }

    private void Move()
    {
        if (_controller == null) return;

        Vector3 velocity = _moveDirection * _moveSpeed;
        velocity.y = GRAVITY;
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
