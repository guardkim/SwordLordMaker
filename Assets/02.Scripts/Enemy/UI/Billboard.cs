using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _camera;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                return;
            }
        }

        // 카메라 방향으로 회전 (카메라가 바라보는 방향과 동일하게)
        _transform.LookAt(
            _transform.position + _camera.transform.rotation * Vector3.forward,
            _camera.transform.rotation * Vector3.up
        );
    }
}
