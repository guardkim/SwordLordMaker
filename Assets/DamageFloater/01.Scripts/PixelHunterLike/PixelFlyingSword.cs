using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PixelFlyingSword : BaseFlyingSword
{
    [Header("■ [1] 패턴 설정 (Infinity Loop - Circle Switch)")]
    [Header("루프 지름")]
    public float LoopDiameter = 5.0f;
    [Header("비행 속도")]
    public float OrbitalSpeed = 600.0f; 
    [Header("궤도 회전 (Precession)")]
    public float PrecessionSpeed = 10.0f;

    [Header("■ [2] 움직임 품질")]
    public float CenterSnapStrength = 50.0f;
    public float SmoothingSpeed = 20.0f;

    [Header("■ [3] 기타 설정")]
    public float DeploySpeed = 40f;
    public int MaxAttackCount = 4;
    public float FlyAwaySpeed = 20f;

    // 내부 동작용 변수 (첫 번째 코드의 로직 복원)
    private Action _onDepartureCallback;
    private float _currentPhase;       // 0 ~ 360 진행도
    private float _currentAxisAngle;   // 현재 원의 축 (0 or 180)
    private float _rotateDir = 1.0f;   // 1(반시계) <-> -1(시계)
    private float _accumulatedPrecession; // 전체 회전 누적값

    private bool _isDeploying = true;
    private bool _isDeparting;
    private int _hitCount;
    private float _lastHitTime;
    private Vector3 _departDirection;

    public void Init(Transform startPoint, Transform target, Action onFinished)
    {
        TargetEnemy = target; // 부모 변수 할당
        _onDepartureCallback = onFinished;
        transform.position = startPoint.position;
        
        _hitCount = 0;
        _isDeploying = true;
        _isDeparting = false;
        
        // [로직 1 복원] 초기화
        _currentPhase = 0f;
        _rotateDir = 1.0f;     // 반시계 시작
        _currentAxisAngle = 0f; // 적의 오른쪽부터 시작
        _accumulatedPrecession = Random.Range(0f, 360f); // 전체 각도는 랜덤
    }

    private void Update()
    {
        if (!TargetEnemy)
        {
            Destroy(gameObject);
            return;
        }

        if (_isDeparting)
        {
            transform.position += _departDirection * (FlyAwaySpeed * Time.deltaTime);
            return; 
        }

        // ------------------------------------------------------------------
        // [1] 위상 계산 (첫 번째 코드의 핵심 로직 이식)
        // ------------------------------------------------------------------
        _currentPhase += Time.deltaTime * OrbitalSpeed * _rotateDir;

        // 한 바퀴(360도) 돌았는지 체크
        bool completed = (_rotateDir > 0 && _currentPhase >= 360f) || (_rotateDir < 0 && _currentPhase <= -360f);

        if (completed)
        {
            // 위상 리셋 및 오차 보정
            _currentPhase = (_rotateDir > 0) ? (_currentPhase - 360f) : (_currentPhase + 360f);
            
            // 방향 반전 (오른쪽 원 -> 왼쪽 원)
            _rotateDir *= -1.0f;
            
            // 축 변경 (0도 -> 180도)
            _currentAxisAngle += 180.0f;
            
            // 궤도 비틀기 (Precession)
            _accumulatedPrecession += PrecessionSpeed;
        }

        // ------------------------------------------------------------------
        // [2] 목표 좌표 계산 (Coordinate Calculation)
        // ------------------------------------------------------------------
        float r = LoopDiameter * 0.5f;
        float finalAxis = _currentAxisAngle + _accumulatedPrecession;

        // 원의 중심(Anchor) 계산
        Vector3 anchorPos = TargetEnemy.position + (GetDirVector(finalAxis) * r);

        // 최종 목표 위치 계산 (Anchor 기준 회전)
        // 적 위치(0,0)에서 시작해야 하므로 Anchor 기준 180도 반대편 + 현재 진행도
        float swordAngle = finalAxis + 180f + _currentPhase;
        Vector3 targetPos = anchorPos + (GetDirVector(swordAngle) * r);

        // ------------------------------------------------------------------
        // [3] 이동 및 회전 적용
        // ------------------------------------------------------------------
        if (_isDeploying)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, DeploySpeed * Time.deltaTime);
            RotateSelf(targetPos);
            
            if (Vector3.Distance(transform.position, targetPos) < 0.5f) 
                _isDeploying = false; 
        }
        else
        {
            // [중심 흡착 로직 복원]
            float dist = Vector3.Distance(transform.position, TargetEnemy.position);
            // 거리가 반지름보다 가까울수록 1에 가까워짐
            float snapFactor = Mathf.Clamp01(1.0f - (dist / r)); 
            
            // 흡착 강도에 따라 속도 증가
            float dynamicSpeed = Mathf.Lerp(SmoothingSpeed, SmoothingSpeed + CenterSnapStrength, snapFactor * snapFactor);

            // 이동 (Lerp 사용)
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dynamicSpeed);
            
            // 회전: 진행 방향(접선) 계산 (첫 번째 코드 방식)
            float lookAngle = swordAngle + (90f * Mathf.Sign(_rotateDir)); 
            Vector3 lookDir = GetDirVector(lookAngle);
            
            float zAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, zAngle);

            _departDirection = lookDir; // 이탈 시 사용할 방향 저장
        }
    }

    // 각도를 벡터로 변환하는 헬퍼 함수
    private Vector3 GetDirVector(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
    }

    private void RotateSelf(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDeploying || _isDeparting) return;
        if (Time.time - _lastHitTime < 0.1f) return;

        // 부모 클래스(BaseFlyingSword)의 데미지 처리 사용
        bool hasHit = TryDealDamage(other);

        if (other.CompareTag("Enemy"))
        {
            _hitCount++;
            if (_hitCount >= MaxAttackCount) StartDeparture();
            hasHit = true;
        }

        if (hasHit) _lastHitTime = Time.time;
    }

    private void StartDeparture()
    {
        _isDeparting = true;
        _onDepartureCallback?.Invoke();
        Destroy(gameObject, 3.0f);
    }
}