using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class HypoFlyingSword : BaseFlyingSword
{
    [Header("■ [1] 패턴 설정 (Hypocycloid Rose)")]
    public float PatternSize = 8.0f;       
    public float MoveSpeed = 20.0f; 
    [Range(3, 10)] 
    public int PetalCount = 4;
    public float PrecessionSpeed = 20.0f;
    
    [Header("■ [2] 움직임 품질")]
    public float SmoothingSpeed = 40.0f;
    
    [Header("■ [3] 기타 설정")]
    public float DeploySpeed = 40f;        
    public int MaxAttackCount = 4;         
    public float FlyAwaySpeed = 20f;
    public TrailRenderer Trail;
    

    private Action _onDepartureCallback;
    
    // 수학 변수
    private float _currentTheta;
    private float _rotateDir;
    private float _kRatio;

    // 상태 변수
    private bool _isDeploying = true;       
    private bool _isDeparting;      
    private int _hitCount;       
    private Vector3 _departDirection;       
    private float _lastHitTime;        

    public void Init(Transform startPoint, Transform enemy, Action onDeparture, SwordStat stat)
    {
        TargetEnemy = enemy; // 부모 변수 사용
        transform.position = startPoint.position;
        _onDepartureCallback = onDeparture;

        InitializeStat(stat);
        if (stat != null)
        {
            MoveSpeed = stat.MoveSpeed;
        }

        _hitCount = 0;
        _isDeploying = true;
        _isDeparting = false;

        _rotateDir = (Random.value > 0.5f) ? 1.0f : -1.0f;
        _currentTheta = Random.Range(0f, 360f * Mathf.Deg2Rad);

        // k = 1 - N (Hypocycloid 공식)
        _kRatio = 1.0f - PetalCount;
        if (!Trail) return;
        Trail.Clear();
        Trail.emitting = true;
    }

    private void Update()
    {
        // 타겟이 죽었으면 null 처리
        if (TargetEnemy != null)
        {
            var enemy = TargetEnemy.GetComponent<EnemyAI>();
            if (enemy != null && enemy.IsDead)
            {
                TargetEnemy = null;
            }
        }

        if (!TargetEnemy)
        {
            Destroy(gameObject);
            return;
        }

        if (_isDeparting)
        {
            transform.position = ClampHeight(transform.position + _departDirection * (FlyAwaySpeed * Time.deltaTime));
            return;
        }

        // [수학] 속도 보정을 위한 미분값 계산
        float r = PatternSize * 0.25f; 
        float denom = 1.0f + (_kRatio * _kRatio) + (2.0f * _kRatio * Mathf.Cos(_currentTheta * (1.0f - _kRatio)));
        float currentDerivative = r * Mathf.Sqrt(denom);
        
        float dTheta = (MoveSpeed * Time.deltaTime) / currentDerivative;
        _currentTheta += dTheta * _rotateDir;

        // [수학] 위치 계산 (XZ 평면 - 3D 환경 대응)
        float precession = Time.time * PrecessionSpeed * _rotateDir;
        Vector3 localPos = CalculateHypoPos(_currentTheta);
        localPos = RotateVector(localPos, precession);
        Vector3 targetPos = TargetEnemy.position + localPos;

        // 이동 로직
        if (_isDeploying)
        {
            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, DeploySpeed * Time.deltaTime);
            transform.position = ClampHeight(newPos);
            RotateSelf(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
                _isDeploying = false;
        }
        else
        {
            Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * SmoothingSpeed);
            transform.position = ClampHeight(newPos);

            // Look Ahead (XZ 평면 - 3D 환경 대응)
            float lookTheta = _currentTheta + (0.1f * _rotateDir);
            Vector3 nextLocal = RotateVector(CalculateHypoPos(lookTheta), precession);
            Vector3 lookTarget = TargetEnemy.position + nextLocal;

            RotateSelf(lookTarget);
            _departDirection = (lookTarget - transform.position).normalized;
        }
    }

    // XZ 평면 기준 Hypocycloid 위치 계산 (3D 환경 대응)
    private Vector3 CalculateHypoPos(float theta)
    {
        float r = PatternSize * 0.25f;
        float x = r * Mathf.Cos(theta) + r * Mathf.Cos(theta * _kRatio);
        float z = r * Mathf.Sin(theta) + r * Mathf.Sin(theta * _kRatio);
        return new Vector3(x, 0, z);
    }

    // XZ 평면 기준 벡터 회전 (Y축 회전)
    private Vector3 RotateVector(Vector3 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector3(v.x * cos - v.z * sin, 0, v.x * sin + v.z * cos);
    }

    private void RotateSelf(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        // 검 모델의 Y축이 칼날 방향이므로 X축 -90도 추가 회전
        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion tipCorrection = Quaternion.Euler(-90f, 0f, 0f);
        transform.rotation = lookRot * tipCorrection;
    }

    // 3D 충돌 처리
    private void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other.CompareTag("Enemy"), other.GetComponent<IDamageable>());
    }

    // 2D 충돌 처리 (하위 호환)
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other.CompareTag("Enemy"), other.GetComponent<IDamageable>());
    }

    private void HandleTrigger(bool isEnemy, IDamageable target)
    {
        if (_isDeploying || _isDeparting) return;
        if (Time.time - _lastHitTime < 0.1f) return;

        bool hasHit = false;
        if (target != null)
        {
            bool isCritical = Random.value < (_stat?.CritChance ?? 0.5f);
            BigInteger finalDamage = _stat?.CalculateDamage(isCritical) ?? new BigInteger(10);
            target.TakeDamage(finalDamage, isCritical);
            hasHit = true;
        }

        if (isEnemy)
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
        Destroy(gameObject, 5.0f);
    }
}