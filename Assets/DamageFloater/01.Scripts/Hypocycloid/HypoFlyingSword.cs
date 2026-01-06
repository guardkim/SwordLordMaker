using System;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public void Init(Transform startPoint, Transform enemy, Action onDeparture)
    {
        TargetEnemy = enemy; // 부모 변수 사용
        transform.position = startPoint.position;
        _onDepartureCallback = onDeparture;
        
        _hitCount = 0;
        _isDeploying = true; 
        _isDeparting = false;
        
        _rotateDir = (Random.value > 0.5f) ? 1.0f : -1.0f;
        _currentTheta = Random.Range(0f, 360f * Mathf.Deg2Rad);
        
        // k = 1 - N (Hypocycloid 공식)
        _kRatio = 1.0f - PetalCount;
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

        // [수학] 속도 보정을 위한 미분값 계산
        float r = PatternSize * 0.25f; 
        float denom = 1.0f + (_kRatio * _kRatio) + (2.0f * _kRatio * Mathf.Cos(_currentTheta * (1.0f - _kRatio)));
        float currentDerivative = r * Mathf.Sqrt(denom);
        
        float dTheta = (MoveSpeed * Time.deltaTime) / currentDerivative;
        _currentTheta += dTheta * _rotateDir;

        // [수학] 위치 계산
        float precession = Time.time * PrecessionSpeed * _rotateDir;
        Vector2 localPos = CalculateHypoPos(_currentTheta);
        localPos = RotateVector(localPos, precession);
        Vector3 targetPos = TargetEnemy.position + (Vector3)localPos;

        // 이동 로직
        if (_isDeploying)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, DeploySpeed * Time.deltaTime);
            RotateSelf(targetPos);
            
            if (Vector3.Distance(transform.position, targetPos) < 0.5f) 
                _isDeploying = false; 
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * SmoothingSpeed);
            
            // Look Ahead
            float lookTheta = _currentTheta + (0.1f * _rotateDir); 
            Vector2 nextLocal = RotateVector(CalculateHypoPos(lookTheta), precession);
            Vector3 lookTarget = TargetEnemy.position + (Vector3)nextLocal;

            RotateSelf(lookTarget);
            _departDirection = (lookTarget - transform.position).normalized;
        }
    }

    private Vector2 CalculateHypoPos(float theta)
    {
        float r = PatternSize * 0.25f;
        float x = r * Mathf.Cos(theta) + r * Mathf.Cos(theta * _kRatio);
        float y = r * Mathf.Sin(theta) + r * Mathf.Sin(theta * _kRatio);
        return new Vector2(x, y);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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

        // 부모 메서드로 데미지 처리
        bool hasHit = TryDealDamage(other);

        // Enemy 태그 타격 시 횟수 증가 (기존 로직)
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
        Destroy(gameObject, 5.0f);
    }
}