using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chase,
        Attack
    }

    [Header("▼ 참조")]
    [SerializeField] private Transform _target;

    [Header("▼ 추적 설정")]
    [SerializeField] private float _chaseRange = 15f;
    [SerializeField] private float _attackRange = 1.5f;

    [Header("▼ 공격 설정")]
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _attackDamage = 10;

    [Header("▼ 최적화 설정")]
    [SerializeField] private float _updateIntervalNear = 0.2f;
    [SerializeField] private float _updateIntervalFar = 0.5f;
    [SerializeField] private float _farDistanceThreshold = 10f;

    private NavMeshAgent _agent;
    private State _currentState = State.Idle;
    private float _lastUpdateTime;
    private float _lastAttackTime;

    public State CurrentState => _currentState;
    public float Speed => _agent != null ? _agent.velocity.magnitude : 0f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (_target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }
    }

    private void Update()
    {
        if (_target == null || _agent == null)
        {
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        UpdateState(distanceToTarget);
        ExecuteState(distanceToTarget);
    }

    private void UpdateState(float distanceToTarget)
    {
        if (distanceToTarget <= _attackRange)
        {
            _currentState = State.Attack;
        }
        else if (distanceToTarget <= _chaseRange)
        {
            _currentState = State.Chase;
        }
        else
        {
            _currentState = State.Idle;
        }
    }

    private void ExecuteState(float distanceToTarget)
    {
        switch (_currentState)
        {
            case State.Idle:
                ExecuteIdle();
                break;
            case State.Chase:
                ExecuteChase(distanceToTarget);
                break;
            case State.Attack:
                ExecuteAttack();
                break;
        }
    }

    private void ExecuteIdle()
    {
        _agent.isStopped = true;
    }

    private void ExecuteChase(float distanceToTarget)
    {
        _agent.isStopped = false;

        float updateInterval = distanceToTarget > _farDistanceThreshold
            ? _updateIntervalFar
            : _updateIntervalNear;

        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            _agent.SetDestination(_target.position);
            _lastUpdateTime = Time.time;
        }
    }

    private void ExecuteAttack()
    {
        _agent.isStopped = true;

        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Time.time - _lastAttackTime >= _attackCooldown)
        {
            Attack();
            _lastAttackTime = Time.time;
        }
    }

    private void Attack()
    {
        // TODO: 플레이어에게 데미지 전달.
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
