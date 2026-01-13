using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [Header("Hit VFX")]
    [SerializeField] private GameObject _hitVfxPrefab;
    [SerializeField] private float _vfxLifetime = 1f;
    [SerializeField] private int _poolSize = 20;

    private Queue<GameObject> _vfxPool;

    protected override void Initialize()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _vfxPool = new Queue<GameObject>();

        if (_hitVfxPrefab == null) return;

        for (int i = 0; i < _poolSize; i++)
        {
            GameObject vfx = Instantiate(_hitVfxPrefab);
            vfx.SetActive(false);
            _vfxPool.Enqueue(vfx);
        }
    }

    public void PlayHitVfx(Vector3 position)
    {
        PlayHitVfx(position, Quaternion.identity);
    }

    public void PlayHitVfx(Vector3 position, Quaternion rotation)
    {
        if (_hitVfxPrefab == null) return;

        GameObject vfx = GetFromPool();
        vfx.transform.SetPositionAndRotation(position, rotation);
        vfx.SetActive(true);

        StartCoroutine(ReturnToPoolAfterDelay(vfx, _vfxLifetime));
    }

    private GameObject GetFromPool()
    {
        if (_vfxPool.Count > 0)
        {
            return _vfxPool.Dequeue();
        }

        return Instantiate(_hitVfxPrefab);
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfx.SetActive(false);
        _vfxPool.Enqueue(vfx);
    }
}
