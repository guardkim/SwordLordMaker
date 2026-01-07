using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private Transform _target;

    [Header("▼ 카메라 설정")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(-10f, 9f, -10f);
    [SerializeField] private Vector3 _rotation = new Vector3(30f, 45f, 0f);
    [SerializeField] private float _smoothSpeed = 10f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(_rotation);
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

        Vector3 targetPosition = _target.position + _positionOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
    }
}
