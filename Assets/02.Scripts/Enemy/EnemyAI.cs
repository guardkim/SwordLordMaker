using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        SkillAttack,
        Hit,
        Dead
    }

    private const float DAMAGE_FLOATER_HEIGHT_OFFSET = 2f;
    private const float HIT_VFX_HEIGHT_OFFSET = 1f;
    private const float DEATH_POOL_RETURN_DELAY = 3f;
    private const float HIT_STUN_DURATION = 0.3f;
    private const float KNOCKBACK_DISTANCE = 0.5f;
    private const float KNOCKBACK_DURATION = 0.15f;

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

    [Header("▼ 보스 스킬 설정")]
    [SerializeField] private float _skillCooldown = 5f;
    [SerializeField] private float _skillRadius = 3f;
    [SerializeField] private float _skillChargeTime = 1f;
    [SerializeField] private float _skillDamageMultiplier = 2f;

    private NavMeshAgent _agent;
    private State _currentState = State.Idle;
    private State _previousState = State.Idle;
    private float _lastUpdateTime;
    private float _lastAttackTime;

    // DB에서 로드한 스탯
    private EnemyStat _stat;
    private BigInteger _currentHealth;
    private bool _isBoss;

    // 보스 스킬 상태
    private float _lastSkillTime;
    private bool _isUsingSkill;

    public State CurrentState => _currentState;
    public float Speed => _agent != null ? _agent.velocity.magnitude : 0f;
    public bool IsDead => _currentState == State.Dead;
    public bool IsMoving => _currentState == State.Chase;
    public bool IsAttacking => _currentState == State.Attack;
    public bool IsHit => _currentState == State.Hit;
    public bool IsBoss => _isBoss;
    public bool IsUsingSkill => _isUsingSkill;

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

    // 보스로 초기화
    public void InitializeAsBoss(EnemyStat stat)
    {
        Initialize(stat);
        _isBoss = true;
    }

    // 풀로 반환될 때 호출 (상태 리셋)
    public void ResetForPool()
    {
        _currentState = State.Idle;
        _previousState = State.Idle;
        _stat = null;
        _currentHealth = BigInteger.Zero;
        _lastUpdateTime = 0f;
        _lastAttackTime = 0f;
        _isBoss = false;
        _lastSkillTime = 0f;
        _isUsingSkill = false;

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
        if (_currentState == State.Dead || _currentState == State.Hit || _currentState == State.SkillAttack)
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

    public void TakeDamage(BigInteger damage, bool isCrit)
    {
        if (_currentState == State.Dead) return;

        _currentHealth -= damage;
        if (_currentHealth < BigInteger.Zero)
        {
            _currentHealth = BigInteger.Zero;
        }

        ShowDamageEffects(damage, isCrit);
        UpdateHealthBar();

        if (_currentHealth <= BigInteger.Zero)
        {
            Die();
        }
        else
        {
            TriggerHitState();
        }
    }

    private void TriggerHitState()
    {
        if (_currentState == State.Dead || _currentState == State.Hit) return;

        _currentState = State.Hit;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        if (_enemyAnimation != null)
        {
            _enemyAnimation.StopAllActions();
            _enemyAnimation.TriggerHit();
        }

        StartCoroutine(ApplyKnockback());
        StartCoroutine(RecoverFromHit());
    }

    private IEnumerator ApplyKnockback()
    {
        // 바라보는 방향의 반대(뒤쪽)로 넉백
        Vector3 knockbackDirection = -transform.forward;
        knockbackDirection.y = 0f;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + knockbackDirection * KNOCKBACK_DISTANCE;

        // NavMesh 위의 유효한 위치인지 확인
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, KNOCKBACK_DISTANCE, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        else
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < KNOCKBACK_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / KNOCKBACK_DURATION;

            // EaseOut 효과로 자연스러운 넉백
            t = 1f - (1f - t) * (1f - t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        // NavMeshAgent 위치 동기화
        if (_agent != null && _agent.enabled)
        {
            _agent.Warp(targetPosition);
        }
    }

    private IEnumerator RecoverFromHit()
    {
        yield return new WaitForSeconds(HIT_STUN_DURATION);

        if (_currentState == State.Hit)
        {
            _currentState = State.Idle;

            if (_agent != null)
            {
                _agent.isStopped = false;
            }
        }
    }

    private void ShowDamageEffects(BigInteger damage, bool isCrit)
    {
        if (DamageFloaterManager.Instance != null)
        {
            // BigInteger 오버로드 사용 (축약 표기 자동 지원)
            DamageFloaterManager.Instance.ShowDamage(
                DamageStyle.Basic, damage, GetDamageFloaterPosition(), isCrit);
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.PlayHitVfx(GetHitVfxPosition());
        }
    }

    private void UpdateHealthBar()
    {
        if (_hpBar != null)
        {
            _hpBar.UpdateHP(_currentHealth);
        }
    }

    private Vector3 GetDamageFloaterPosition()
    {
        return transform.position + Vector3.up * DAMAGE_FLOATER_HEIGHT_OFFSET;
    }

    private Vector3 GetHitVfxPosition()
    {
        return transform.position + Vector3.up * HIT_VFX_HEIGHT_OFFSET;
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

        // StageManager에 사망 알림 (보스/일반 구분)
        if (StageManager.Instance != null)
        {
            if (_isBoss)
            {
                StageManager.Instance.OnBossDied(this);
            }
            else
            {
                StageManager.Instance.OnEnemyDied(this);
            }
        }

        // 사망 애니메이션
        if (_enemyAnimation != null)
        {
            _enemyAnimation.Die();
        }

        // 풀로 반환 (사망 애니메이션 후)
        StartCoroutine(ReturnToPoolAfterDelay(DEATH_POOL_RETURN_DELAY));
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
        if (_isUsingSkill) return;

        // 보스 스킬 체크 (공격 범위 내에서 스킬 쿨다운이 찼을 때)
        if (_isBoss && distanceToTarget <= _attackRange && CanUseSkill())
        {
            StartCoroutine(ExecuteSkillAttack());
            return;
        }

        State newState;

        if (distanceToTarget <= _attackRange)
        {
            newState = State.Attack;
        }
        else if (distanceToTarget <= _chaseRange)
        {
            newState = State.Chase;
        }
        else
        {
            newState = State.Idle;
        }

        if (newState != _currentState)
        {
            _previousState = _currentState;
            _currentState = newState;
            OnStateChanged();
        }
    }

    private void OnStateChanged()
    {
        if (_enemyAnimation == null) return;

        switch (_currentState)
        {
            case State.Idle:
                _enemyAnimation.SetMoving(false);
                _enemyAnimation.SetAttacking(false);
                break;
            case State.Chase:
                _enemyAnimation.SetMoving(true);
                _enemyAnimation.SetAttacking(false);
                break;
            case State.Attack:
                _enemyAnimation.SetMoving(false);
                _enemyAnimation.SetAttacking(true);
                break;
            case State.SkillAttack:
                _enemyAnimation.SetMoving(false);
                _enemyAnimation.SetAttacking(false);
                break;
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
        if (_stat == null || _target == null)
        {
            return;
        }

        IDamageable damageable = _target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_stat.AttackDamage, false);
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    // 보스 스킬 사용 가능 여부 체크
    private bool CanUseSkill()
    {
        if (!_isBoss) return false;
        if (_isUsingSkill) return false;
        return Time.time - _lastSkillTime >= _skillCooldown;
    }

    // 스킬 공격 실행 (AoE 범위 공격)
    private IEnumerator ExecuteSkillAttack()
    {
        _isUsingSkill = true;
        _currentState = State.SkillAttack;

        // NavMeshAgent 정지
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        // 차징 애니메이션
        _enemyAnimation?.TriggerSkill();

        // 차징 시간 대기
        yield return new WaitForSeconds(_skillChargeTime);

        // 범위 내 플레이어에게 데미지
        ApplyAoEDamage();

        _lastSkillTime = Time.time;
        _isUsingSkill = false;
        _currentState = State.Idle;

        // NavMeshAgent 재개
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = false;
        }
    }

    // AoE 범위 데미지 적용
    private void ApplyAoEDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _skillRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    BigInteger skillDamage = MultiplyBigInteger(_stat.AttackDamage, _skillDamageMultiplier);
                    target.TakeDamage(skillDamage, false);
                }
            }
        }

        // 스킬 VFX 재생
        EffectManager.Instance?.PlaySkillVfx(transform.position);
    }

    // BigInteger에 float 배율 적용
    private BigInteger MultiplyBigInteger(BigInteger value, float multiplier)
    {
        if (multiplier <= 0f) return value;
        if (multiplier == 1f) return value;

        int scaledMultiplier = (int)(multiplier * 1000);
        return value * scaledMultiplier / 1000;
    }
}
