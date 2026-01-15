using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [Header("Hit VFX")]
    [SerializeField] private GameObject _hitVfxPrefab;
    [SerializeField] private float _vfxLifetime = 1f;
    [SerializeField] private int _poolSize = 20;

    [Header("Skill VFX")]
    [SerializeField] private GameObject _skillVfxPrefab;
    [SerializeField] private float _skillVfxLifetime = 1.5f;
    [SerializeField] private int _skillPoolSize = 5;

    private Queue<GameObject> _vfxPool;
    private Queue<GameObject> _skillVfxPool;

    protected override void Initialize()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _vfxPool = new Queue<GameObject>();
        _skillVfxPool = new Queue<GameObject>();

        if (_hitVfxPrefab != null)
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject vfx = Instantiate(_hitVfxPrefab);
                vfx.SetActive(false);
                _vfxPool.Enqueue(vfx);
            }
        }

        if (_skillVfxPrefab != null)
        {
            for (int i = 0; i < _skillPoolSize; i++)
            {
                GameObject vfx = Instantiate(_skillVfxPrefab);
                vfx.SetActive(false);
                _skillVfxPool.Enqueue(vfx);
            }
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

    public void PlaySkillVfx(Vector3 position)
    {
        if (_skillVfxPrefab == null) return;

        GameObject vfx = GetSkillVfxFromPool();
        vfx.transform.SetPositionAndRotation(position, Quaternion.identity);
        vfx.SetActive(true);

        StartCoroutine(ReturnSkillVfxToPool(vfx, _skillVfxLifetime));
    }

    private GameObject GetSkillVfxFromPool()
    {
        if (_skillVfxPool.Count > 0)
        {
            return _skillVfxPool.Dequeue();
        }

        return Instantiate(_skillVfxPrefab);
    }

    private IEnumerator ReturnSkillVfxToPool(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfx.SetActive(false);
        _skillVfxPool.Enqueue(vfx);
    }
}
