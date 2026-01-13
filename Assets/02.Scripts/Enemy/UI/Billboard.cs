using UnityEngine;

public class Billboard : MonoBehaviour
{
    private const float DEFAULT_HEIGHT_OFFSET = 2.2f;

    [Header("설정")]
    [SerializeField] private Vector3 _offset = new Vector3(0, DEFAULT_HEIGHT_OFFSET, 0);

    private Transform _target;
    private Transform _cameraTransform;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        CacheMainCamera();
        SetupTarget();
    }

    private void CacheMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            _cameraTransform = mainCamera.transform;
        }
    }

    private void SetupTarget()
    {
        if (_target == null && transform.parent != null)
        {
            _target = transform.parent;
        }

        // 부모-자식 관계를 끊어 몬스터 회전과 독립적으로 동작
        transform.SetParent(null);
    }

    private void LateUpdate()
    {
        // 타겟(몬스터)이 죽어서 사라졌다면 HP바도 자폭
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_cameraTransform == null) return;

        // 3. 위치 고정: 몬스터의 '월드 좌표' + 오프셋
        // 부모 관계가 끊겼으므로 몬스터의 회전값은 무시하고 위치만 따라감
        _transform.position = _target.position + _offset;

        // 4. 회전 고정: 카메라와 평행하게 (쿼터뷰 최적화)
        _transform.rotation = _cameraTransform.rotation;
    }

    // (선택사항) 몬스터가 풀링(Pooling)되어 재사용될 때를 위한 함수
    // 몬스터가 다시 태어날 때 이 함수를 호출해줘야 함
    public void ResetTarget(Transform newTarget)
    {
        _target = newTarget;
        gameObject.SetActive(true);
    }
}