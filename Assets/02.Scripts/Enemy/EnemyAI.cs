using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("▼ 참조")]
    [SerializeField] private Transform _target;
    [SerializeField] private EnemyAnimation _enemyAnimation;
    [SerializeField] private EnemyHPBar _hpBar;

    [Header("▼ AI 설정")]
    [SerializeField] private float _chaseRange = 15f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("▼ 최적화 설정")]
    [SerializeField] private float _updateIntervalNear = 0.2f;
    [SerializeField] private float _updateIntervalFar = 0.5f;
    [SerializeField] private float _farDistanceThreshold = 10f;

    private NavMeshAgent _agent;
    private State _currentState = State.Idle;
    private float _lastUpdateTime;
    private float _lastAttackTime;

    // DB에서 로드한 스탯
    private EnemyStat _stat;
    private int _currentHealth;

    public State CurrentState => _currentState;
    public float Speed => _agent != null ? _agent.velocity.magnitude : 0f;
    public bool IsDead => _currentState == State.Dead;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Spawner를 통해 생성되지 않은 경우 (에디터 테스트용)
        if (_stat == null)
        {
            FindTarget();
            FindComponents();
        }
    }

    // 풀에서 가져올 때 호출 (스탯 초기화)
    public void Initialize(EnemyStat stat)
    {
        _stat = stat;
        _currentHealth = stat.MaxHP;
        _currentState = State.Idle;

        // NavMeshAgent 속도 설정
        if (_agent != null)
        {
            _agent.speed = stat.MoveSpeed;
            _agent.enabled = true;
            _agent.isStopped = false;
        }

        // HPBar 초기화
        if (_hpBar != null)
        {
            _hpBar.Initialize(stat.MaxHP);
        }

        FindTarget();
        FindComponents();

        // 애니메이션 리셋
        if (_enemyAnimation != null)
        {
            _enemyAnimation.ResetAnimation();
        }
    }

    // 풀로 반환될 때 호출 (상태 리셋)
    public void ResetForPool()
    {
        _currentState = State.Idle;
        _stat = null;
        _currentHealth = 0;
        _lastUpdateTime = 0f;
        _lastAttackTime = 0f;

        if (_agent != null)
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
            _agent.enabled = false;
        }

        if (_hpBar != null)
        {
            _hpBar.Reset();
        }

        StopAllCoroutines();
    }

    private void FindTarget()
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

    private void FindComponents()
    {
        if (_enemyAnimation == null)
        {
            _enemyAnimation = GetComponent<EnemyAnimation>();
        }
    }

    private void Update()
    {
        if (_currentState == State.Dead)
        {
            return;
        }

        if (_target == null || _agent == null)
        {
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _target.position);
        UpdateState(distanceToTarget);
        ExecuteState(distanceToTarget);
    }

    public void TakeDamage(int damage, bool isCrit)
    {
        if (_currentState == State.Dead)
        {
            return;
        }

        _currentHealth -= damage;

        // HPBar 업데이트
        if (_hpBar != null)
        {
            _hpBar.UpdateHP(_currentHealth);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_currentState == State.Dead)
        {
            return;
        }

        _currentState = State.Dead;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        // 골드 지급 (스탯에서 가져옴)
        if (CurrencyManager.Instance != null && _stat != null)
        {
            CurrencyManager.Instance.AddGold(_stat.GoldReward);
        }

        // StageManager에 사망 알림
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnEnemyDied(this);
        }

        // 사망 애니메이션
        if (_enemyAnimation != null)
        {
            _enemyAnimation.Die();
        }

        // 풀로 반환 (사망 애니메이션 후)
        StartCoroutine(ReturnToPoolAfterDelay(3f));
    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.Return(this);
        }
        else
        {
            // Spawner가 없으면 Destroy (에디터 테스트용)
            Destroy(gameObject);
        }
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
        // 스탯에서 공격력 사용
        if (_stat != null)
        {
            // TODO: 플레이어에게 _stat.AttackDamage 데미지 전달
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
