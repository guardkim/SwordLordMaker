using DG.Tweening;
using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    public static QuarterViewCamera Instance { get; private set; }

    [Header("▼ 참조")]
    [SerializeField] private Transform _target;

    [Header("▼ 카메라 설정")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(-10f, 9f, -10f);
    [SerializeField] private Vector3 _rotation = new Vector3(30f, 45f, 0f);
    [SerializeField] private float _smoothSpeed = 10f;

    [Header("▼ 줌 설정")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 0.5f;
    [SerializeField] private float _maxZoom = 2.0f;
    [SerializeField] private float _zoomSmoothSpeed = 10f;

    [Header("▼ 카메라 쉐이크 (모바일용)")]
    [SerializeField] private float _shakeDuration = 0.1f;
    [SerializeField] private float _shakeStrength = 0.08f;
    [SerializeField] private int _shakeVibrato = 30;

    private Vector3 _shakeOffset;
    private Tweener _shakeTweener;
    private float _currentZoom = 1.0f;
    private float _targetZoom = 1.0f;
    private Vector3 _baseOffset;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        transform.rotation = Quaternion.Euler(_rotation);
        _baseOffset = _positionOffset;
    }

    private void Update()
    {
        HandleZoomInput();
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            _targetZoom -= scrollInput * _zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomSmoothSpeed * Time.deltaTime);
        _positionOffset = _baseOffset * _currentZoom;
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 targetPosition = _target.position + _positionOffset + _shakeOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
    }

    public void Shake()
    {
        Shake(_shakeDuration, _shakeStrength, _shakeVibrato);
    }

    public void Shake(float duration, float strength, int vibrato)
    {
        _shakeTweener?.Kill();
        _shakeOffset = Vector3.zero;

        _shakeTweener = DOTween.Shake(
            () => _shakeOffset,
            x => _shakeOffset = x,
            duration,
            strength,
            vibrato,
            90f,
            false,
            true
        ).OnComplete(() => _shakeOffset = Vector3.zero);
    }
}
