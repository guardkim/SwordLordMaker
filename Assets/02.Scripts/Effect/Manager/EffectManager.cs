using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [Header("Hit VFX")]
    [SerializeField] private GameObject _hitVfxPrefab;
    [SerializeField] private float _vfxLifetime = 1f;

    public void PlayHitVfx(Vector3 position)
    {
        if (_hitVfxPrefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(_hitVfxPrefab, position, Quaternion.identity);
        Destroy(vfx, _vfxLifetime);
    }

    public void PlayHitVfx(Vector3 position, Quaternion rotation)
    {
        if (_hitVfxPrefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(_hitVfxPrefab, position, rotation);
        Destroy(vfx, _vfxLifetime);
    }
}
