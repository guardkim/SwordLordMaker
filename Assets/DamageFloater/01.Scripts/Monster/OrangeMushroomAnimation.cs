using System.Collections;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class OrangeMushroomAnimation : MonoBehaviour, IDamageable
{
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int Revive = Animator.StringToHash("Revive");
    private static readonly int Hit = Animator.StringToHash("Hit");

    // --- 설정 변수 (Inspector에서 수정 가능) ---
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private float _changeActionTime = 2.0f; // 행동 변경 주기

    [Header("Combat Settings")]
    [SerializeField] private float _maxHealth = 50f;     // 최대 체력
    [SerializeField] private float _knockbackDistance = 0.5f; // 넉백 거리

    // --- 내부 변수 ---
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;

    private float _currentHealth; // 현재 체력
    private float _timer;
    private int _nextMoveDirection; // -1: 왼쪽, 0: 정지, 1: 오른쪽
    
    // 화면 경계
    private float _minX, _maxX;
    private float _spriteHalfWidth;

    // 상태 관리
    private enum State { Roaming, Hit, Dead }
    private State _currentState = State.Roaming;

    // 현재 실행 중인 코루틴 저장 (중복 실행 방지 및 취소용)
    private Coroutine _currentActionCoroutine;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();

        // 초기 체력 설정
        _currentHealth = _maxHealth;

        // 화면 경계 계산
        CalculateScreenBounds();
        
        // 초기 타이머 설정
        _timer = _changeActionTime;
    }

    private void Update()
    {
        HandleInput(); // 테스트용 입력

        if (_currentState == State.Roaming)
        {
            HandleRandomBehavior();
            CheckBoundsAndFlip();
        }
    }

    private void FixedUpdate()
    {
        if (_currentState == State.Roaming)
        {
            Move();
        }
    }

    // --- 1. 입력 처리 (테스트용) ---
    private void HandleInput()
    {
        // 실제 게임에서는 플레이어의 공격 스크립트가 TakeDamage를 호출하겠지만,
        // 테스트를 위해 키 입력으로 TakeDamage를 호출합니다.

        // Space: 약한 공격 (10 데미지) -> Hit 로직 확인
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     if(Random.Range(0, 100) % 2 == 0)
        //         TakeDamage(10, false);
        //     else
        //         TakeDamage(20, true);
        // }
    }

    // --- [핵심] IDamageable 인터페이스 구현 ---
    public void TakeDamage(int damage, bool isCrit)
    {
        // 이미 죽었으면 데미지 무시
        if (_currentState == State.Dead) return;

        _currentHealth -= damage;
        DamageFloaterManager.Instance.ShowDamage(DamageFloaterTester.CurrentStyle, damage,transform.position, isCrit);
        // Debug.Log($"Mushroom HP: {currentHealth}/{maxHealth}");

        // 기존에 진행 중이던 행동(Hit 등)이 있다면 멈추고 새로운 반응을 보여줌
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);

        if (_currentHealth <= 0)
        {
            _currentActionCoroutine = StartCoroutine(DieRoutine());
        }
        else
        {
            _currentActionCoroutine = StartCoroutine(HitRoutine());
        }
    }

    // --- 2. 랜덤 행동 로직 ---
    private void HandleRandomBehavior()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            int randomAction = Random.Range(0, 3); // 0: Idle, 1: Left, 2: Right

            if (randomAction == 0)
            {
                _nextMoveDirection = 0;
                _anim.SetBool(IsMoving, false);
            }
            else
            {
                _nextMoveDirection = (randomAction == 1) ? -1 : 1;
                _anim.SetBool(IsMoving, true);
            }

            _timer = Random.Range(1.0f, _changeActionTime);
        }
    }

    // --- 3. 이동 및 경계 처리 ---
    private void Move()
    {
        _rb.linearVelocity = new Vector2(_nextMoveDirection * _moveSpeed, _rb.linearVelocity.y);
    }

    private void CheckBoundsAndFlip()
    {
        Vector3 pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, _minX, _maxX);
        
        if (pos.x < _minX ||
            pos.x > _maxX)
        {
            transform.position = new Vector3(clampedX, pos.y, pos.z);
            _nextMoveDirection *= -1;
        }

        if (_nextMoveDirection == 1) _sr.flipX = true;
        else if (_nextMoveDirection == -1) _sr.flipX = false;
    }

    // --- 4. Hit (피격) 로직 ---
    private IEnumerator HitRoutine()
    {
        _currentState = State.Hit;       
        _rb.linearVelocity = Vector2.zero; // 미끄러짐 방지
        _anim.SetBool(IsMoving, false);
        _nextMoveDirection = 0;           

        _anim.SetTrigger(Hit);

        // 넉백 방향: 바라보는 방향의 반대 (뒤로 밀림)
        float direction = _sr.flipX ? -1f : 1f;
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(direction * _knockbackDistance, 0, 0);
        targetPos.x = Mathf.Clamp(targetPos.x, _minX, _maxX);

        float moveDuration = 0.1f; // 넉백 시간 (짧고 빠르게)
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / moveDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, percent);
            yield return null;
        }

        transform.position = targetPos;

        // 경직 시간 (Stun)
        yield return new WaitForSeconds(0.4f); 

        // 상태 복구
        _currentState = State.Roaming;
        _timer = 0; // 즉시 새로운 행동 결정
        _currentActionCoroutine = null;
    }

    // --- 5. Die (사망) 로직 ---
    private IEnumerator DieRoutine()
    {
        _currentState = State.Dead;
        _rb.linearVelocity = Vector2.zero;
        _anim.SetTrigger(Die);
        _anim.SetBool(IsMoving, false);

        // 골드 지급
        CurrencyManager.Instance.AddGold(100);

        // 사망 처리 대기 (2초 후 부활 로직으로 연결)
        yield return new WaitForSeconds(2.0f); 

        // --- 부활 (Revive) ---
        // 몬스터가 다시 살아나는 로직 (필요 없다면 Destroy(gameObject) 사용)
        _currentHealth = _maxHealth; // 체력 회복
        _anim.SetTrigger(Revive); // 부활 애니메이션(혹은 Idle) 트리거
        
        yield return new WaitForSeconds(0.5f); // 일어나는 모션 시간 벌기

        _currentState = State.Roaming;
        _timer = 0;
        _currentActionCoroutine = null;
    }

    // --- 유틸리티 ---
    private void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        _spriteHalfWidth = _sr.bounds.extents.x;

        if (cam)
        {
            Vector2 minScreen = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector2 maxScreen = cam.ViewportToWorldPoint(new Vector3(1, 0, 0));

            _minX = minScreen.x + _spriteHalfWidth;
            _maxX = maxScreen.x - _spriteHalfWidth;
        }
    }

    public void TakeDamage(BigInteger damage, bool isCrit)
    {
        if (_currentState == State.Dead) return;

        // BigInteger를 float로 변환 (이 몬스터는 체력이 작으므로 float로 충분)
        float floatDamage = (float)damage;
        _currentHealth -= floatDamage;

        // BigInteger 오버로드 사용
        DamageFloaterManager.Instance.ShowDamage(DamageFloaterTester.CurrentStyle, damage, transform.position, isCrit);

        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);

        if (_currentHealth <= 0)
        {
            _currentActionCoroutine = StartCoroutine(DieRoutine());
        }
        else
        {
            _currentActionCoroutine = StartCoroutine(HitRoutine());
        }
    }
}