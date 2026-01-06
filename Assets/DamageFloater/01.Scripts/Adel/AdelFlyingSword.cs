using UnityEngine;

public class AdelFlyingSword : BaseFlyingSword
{
    [Header("■ 움직임 설정")]
    public float CurveScale = 10.0f; 
    public float SmoothTime = 0.2f; 
    public float PatrolSpeed = 2.5f;       
    public float AttackBoostSpeed = 23.8f;
    
    [Header("■ 대기 모드 (Target 없음)")]
    public float IdleRadius = 3.0f;
    public float IdleSpeed = 1.0f;         

    [Header("■ 기타")]
    public float MaxLifeTime = 40f;
    public TrailRenderer Trail;

    private AdelFlyingSwordController _controller;
    private int _myOrderIndex = -1;
    public int OrderIndex => _myOrderIndex;

    private float _currentLifeTime;
    private bool _isEjecting = true;
    private Vector3 _currentVelocity;

    // 8자 궤도 수학 변수
    private float _time; 
    private float _axisRotation; 
    private float _axisDriftSpeed; 
    private bool _hasPassedCenter;

    public void Init(AdelFlyingSwordController ctrl, Transform enemy, Vector3 ejectDir, float force, int orderIndex)
    {
        _controller = ctrl;
        TargetEnemy = enemy; // 부모 변수
        _myOrderIndex = orderIndex;

        _currentLifeTime = 0f;
        
        _time = (Random.Range(0, 2) == 0) ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;
        _axisRotation = Random.Range(0f, 360f);
        _axisDriftSpeed = Random.Range(10f, 30f) * (Random.Range(0,2)==0 ? 1 : -1);

        _currentVelocity = ejectDir.normalized * force; 
        _isEjecting = true;
        
        if(Trail) Trail.Clear();
    }

    public void SetTarget(Transform newTarget)
    {
        TargetEnemy = newTarget;
    }

    public bool HasTarget()
    {
        return TargetEnemy;
    }

    private void Update()
    {
        if (_currentLifeTime >= MaxLifeTime)
        {
            _controller.RemoveSword(this);
            Destroy(gameObject);
            return;
        }
        _currentLifeTime += Time.deltaTime;

        if (_isEjecting)
        {
            HandleEject();
        }
        else
        {
            if (TargetEnemy)
                HandleContinuousFigure8();
            else
                HandleIdle();
        }
    }

    private void HandleEject()
    {
        transform.position += _currentVelocity * Time.deltaTime;
        _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.deltaTime * 4f);
        
        if (_currentVelocity.sqrMagnitude > 0.1f) 
            LookAtDirection(_currentVelocity);

        if (_currentVelocity.magnitude < 3.0f) 
        {
            _isEjecting = false;
            if(Trail) Trail.emitting = true;
        }
    }

    private void HandleContinuousFigure8()
    {
        bool isMyTurn = _controller.IsMyTurn(_myOrderIndex);
        
        float distFactor = Mathf.Abs(Mathf.Sin(_time)); 
        bool approachingCenter = distFactor < 0.3f; 

        float targetSpeed = PatrolSpeed; 

        if (isMyTurn)
        {
            if (approachingCenter)
            {
                targetSpeed = AttackBoostSpeed;
                CheckCenterPass(); 
            }
        }
        else
        {
            if (!approachingCenter) _hasPassedCenter = false;
        }

        _time += Time.deltaTime * targetSpeed;

        float x = Mathf.Sin(_time) * CurveScale;
        float y = Mathf.Sin(2f * _time) * (CurveScale * 0.5f); 

        Vector3 localPos = new Vector3(x, y, 0);

        _axisRotation += _axisDriftSpeed * Time.deltaTime;
        Quaternion rot = Quaternion.Euler(0, 0, _axisRotation);
        
        Vector3 virtualTargetPos = TargetEnemy.position + (rot * localPos);

        transform.position = Vector3.SmoothDamp(transform.position, virtualTargetPos, ref _currentVelocity, SmoothTime);

        if (_currentVelocity.sqrMagnitude > 0.1f) 
            LookAtDirection(_currentVelocity);
    }

    private void HandleIdle()
    {
        float idleSpeedCalc = IdleSpeed * Time.deltaTime;
        _time += idleSpeedCalc; 
        
        float angle = _time + (_myOrderIndex * 1.0f); 
        float x = Mathf.Cos(angle) * IdleRadius;
        float y = Mathf.Sin(angle) * IdleRadius;
        
        Vector3 idlePos = _controller.transform.position + new Vector3(x, y, 0);
        
        transform.position = Vector3.SmoothDamp(transform.position, idlePos, ref _currentVelocity, SmoothTime);
        LookAtDirection(transform.position - _controller.transform.position);
    }

    private void CheckCenterPass()
    {
        if (_hasPassedCenter) return;

        float dist = Vector3.Distance(transform.position, TargetEnemy.position);
        if (dist < 2.0f)
        {
            _hasPassedCenter = true;
            _controller.NextTurn(); 
            
            _axisDriftSpeed = -_axisDriftSpeed; 
            _axisRotation += Random.Range(20f, 60f); 
        }
    }

    private void LookAtDirection(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Adel은 닿으면 무조건 데미지 (쿨타임 X)
        TryDealDamage(other);
    }
}