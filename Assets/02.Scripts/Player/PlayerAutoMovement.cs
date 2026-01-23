using UnityEngine;
using UnityEngine.AI;

public class PlayerAutoMovement : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Run,
        Die
    }

    private const float MOVING_THRESHOLD = 0.1f;

    [Header("▼ 이동 설정")]
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _wanderRadius = 5f;
    [SerializeField] private float _arrivalThreshold = 0.5f;

    [Header("▼ 몬스터 회피")]
    [SerializeField] private float _enemyDetectionRadius = 5f;
    [SerializeField] private float _fleeDistance = 6f;
    [SerializeField] private float _pathUpdateInterval = 0.3f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("▼ 영역 제한")]
    [SerializeField] private Transform _areaCenter;
    [SerializeField] private float _areaRadius = 12f;

    [Header("▼ 상태 전환")]
    [SerializeField] private float _idleDuration = 2f;
    [SerializeField] private float _runDuration = 3f;
    [SerializeField] private float _chaseChance = 0.3f;

    private NavMeshAgent _agent;
    private PlayerState _currentState = PlayerState.Idle;
    private float _baseMoveSpeed;
    private float _moveSpeed;
    private bool _isEnabled = true;
    private float _lastPathUpdateTime;
    private float _stateTimer;
    private bool _isChasing;

    public PlayerState CurrentState => _currentState;
    public bool IsMoving => _currentState == PlayerState.Run && _agent != null && _agent.velocity.magnitude > MOVING_THRESHOLD;

    public float GetCurrentSpeed()
    {
        if (_agent == null) return 0f;
        return _agent.velocity.magnitude;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (PlayerStatManager.Instance != null)
        {
            _baseMoveSpeed = PlayerStatManager.Instance.BaseMoveSpeed;
        }
        else
        {
            _baseMoveSpeed = 5f;
            Debug.LogWarning("[PlayerAutoMovement] PlayerStatManager가 없어 기본값 사용");
        }
    }

    private void Start()
    {
        ApplyUpgradeBonus();

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgraded += OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized += OnUpgradeManagerInitialized;
        }

        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.updateRotation = false;
        }

        if (_areaCenter == null)
        {
            _areaCenter = transform;
        }

        _stateTimer = _runDuration;
        TransitionTo(PlayerState.Run);
    }

    private void OnDestroy()
    {
        if (UpgradeManager.HasInstance)
        {
            UpgradeManager.Instance.OnUpgraded -= OnUpgradeChanged;
            UpgradeManager.Instance.OnInitialized -= OnUpgradeManagerInitialized;
        }
    }

    private void OnUpgradeManagerInitialized()
    {
        ApplyUpgradeBonus();
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

        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
        }
    }

    private void Update()
    {
        if (!_isEnabled || _currentState == PlayerState.Die)
        {
            return;
        }

        if (_agent == null || !_agent.isOnNavMesh)
        {
            return;
        }

        ExecuteState();
        UpdateRotation();
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;

        if (!enabled)
        {
            TransitionTo(PlayerState.Die);
        }
    }

    private void ExecuteState()
    {
        _stateTimer -= Time.deltaTime;

        switch (_currentState)
        {
            case PlayerState.Idle:
                if (_stateTimer <= 0f)
                {
                    _stateTimer = _runDuration;
                    _isChasing = Random.value < _chaseChance;
                    TransitionTo(PlayerState.Run);
                }
                break;
            case PlayerState.Run:
                ExecuteRun();
                if (_stateTimer <= 0f)
                {
                    _stateTimer = _idleDuration;
                    _isChasing = false;
                    TransitionTo(PlayerState.Idle);
                }
                break;
            case PlayerState.Die:
                ExecuteDie();
                break;
        }
    }

    private void ExecuteRun()
    {
        if (Time.time - _lastPathUpdateTime < _pathUpdateInterval)
        {
            return;
        }

        _lastPathUpdateTime = Time.time;

        if (_isChasing)
        {
            SetChaseDestination();
        }
        else
        {
            Vector3 fleeDirection = CalculateFleeDirection();

            if (fleeDirection != Vector3.zero)
            {
                SetFleeDestination(fleeDirection);
            }
            else
            {
                SetRandomDestination();
            }
        }
    }

    private void SetChaseDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, _enemyDetectionRadius * 2f, _enemyLayer);

        if (enemies.Length == 0)
        {
            SetRandomDestination();
            return;
        }

        Transform nearestEnemy = null;
        float nearestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestEnemy = enemy.transform;
            }
        }

        if (nearestEnemy != null)
        {
            Vector3 dirToEnemy = (nearestEnemy.position - transform.position).normalized;
            Vector3 targetPos = transform.position + dirToEnemy * (_fleeDistance * 0.5f);

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, _fleeDistance, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _agent.isStopped = false;
            }
        }
    }

    private Vector3 CalculateFleeDirection()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, _enemyDetectionRadius, _enemyLayer);

        if (enemies.Length == 0)
        {
            return Vector3.zero;
        }

        Vector3 fleeDirection = Vector3.zero;

        foreach (var enemy in enemies)
        {
            Vector3 directionAway = transform.position - enemy.transform.position;
            float distance = directionAway.magnitude;

            if (distance > 0.1f)
            {
                fleeDirection += directionAway.normalized / distance;
            }
        }

        fleeDirection.y = 0f;
        return fleeDirection.normalized;
    }

    private void SetFleeDestination(Vector3 fleeDirection)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        Vector3 fleePoint = transform.position + fleeDirection * _fleeDistance;

        Vector3 centerPoint = _areaCenter != null ? _areaCenter.position : transform.position;
        float distanceFromCenter = Vector3.Distance(
            new Vector3(fleePoint.x, 0f, fleePoint.z),
            new Vector3(centerPoint.x, 0f, centerPoint.z)
        );

        if (distanceFromCenter > _areaRadius)
        {
            Vector3 toCenter = (centerPoint - fleePoint).normalized;
            fleePoint = centerPoint + (fleePoint - centerPoint).normalized * (_areaRadius * 0.8f);
        }

        if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, _fleeDistance, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
            _agent.isStopped = false;
        }
    }

    private void ExecuteDie()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    private void TransitionTo(PlayerState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        switch (newState)
        {
            case PlayerState.Idle:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                }
                break;
            case PlayerState.Run:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                }
                break;
            case PlayerState.Die:
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.velocity = Vector3.zero;
                    _agent.ResetPath();
                }
                break;
        }
    }

    private void SetRandomDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        Vector3 centerPoint = _areaCenter != null ? _areaCenter.position : transform.position;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * _wanderRadius;
            randomDirection.y = 0f;
            Vector3 randomPoint = centerPoint + randomDirection;

            float distanceFromCenter = Vector3.Distance(
                new Vector3(randomPoint.x, 0f, randomPoint.z),
                new Vector3(centerPoint.x, 0f, centerPoint.z)
            );

            if (distanceFromCenter > _areaRadius)
            {
                Vector3 direction = (randomPoint - centerPoint).normalized;
                randomPoint = centerPoint + direction * (_areaRadius * 0.8f);
            }

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                return;
            }
        }

        Debug.LogWarning("[PlayerAutoMovement] 유효한 목적지를 찾지 못함");
    }

    private bool HasArrivedAtDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh) return true;

        if (_agent.pathPending) return false;

        if (_agent.remainingDistance <= _arrivalThreshold)
        {
            return true;
        }

        return false;
    }

    private void UpdateRotation()
    {
        if (_currentState != PlayerState.Run) return;
        if (_agent == null || _agent.velocity.magnitude < MOVING_THRESHOLD) return;

        Vector3 direction = _agent.velocity.normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    public void Revive()
    {
        _isEnabled = true;
        _lastPathUpdateTime = 0f;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = false;
        }

        TransitionTo(PlayerState.Run);
    }

    public void SetAreaCenter(Transform center)
    {
        _areaCenter = center;
    }

    public void SetAreaRadius(float radius)
    {
        _areaRadius = radius;
    }
}
