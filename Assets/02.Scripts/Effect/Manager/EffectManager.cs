using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [Header("Hit VFX")]
    [SerializeField] private List<GameObject> _hitVfxPrefabs;
    [SerializeField] private float _vfxLifetime = 1f;
    [SerializeField] private int _poolSizePerPrefab = 10;

    [Header("Skill VFX")]
    [SerializeField] private GameObject _skillVfxPrefab;
    [SerializeField] private float _skillVfxLifetime = 1.5f;
    [SerializeField] private int _skillPoolSize = 5;

    private List<Queue<GameObject>> _hitVfxPools;
    private Queue<GameObject> _skillVfxPool;

    protected override void Initialize()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _hitVfxPools = new List<Queue<GameObject>>();
        _skillVfxPool = new Queue<GameObject>();

        if (_hitVfxPrefabs != null)
        {
            for (int prefabIndex = 0; prefabIndex < _hitVfxPrefabs.Count; prefabIndex++)
            {
                var pool = new Queue<GameObject>();
                for (int i = 0; i < _poolSizePerPrefab; i++)
                {
                    GameObject vfx = Instantiate(_hitVfxPrefabs[prefabIndex]);
                    vfx.SetActive(false);
                    pool.Enqueue(vfx);
                }
                _hitVfxPools.Add(pool);
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

    // 랜덤으로 하나 재생
    public void PlayHitVfx(Vector3 position)
    {
        PlayHitVfx(position, Quaternion.identity);
    }

    public void PlayHitVfx(Vector3 position, Quaternion rotation)
    {
        if (!HasValidHitVfx()) return;

        int randomIndex = Random.Range(0, _hitVfxPrefabs.Count);
        PlayHitVfxByIndex(randomIndex, position, rotation);
    }

    // 특정 인덱스의 VFX 하나만 재생
    public void PlayHitVfxByIndex(int index, Vector3 position)
    {
        PlayHitVfxByIndex(index, position, Quaternion.identity);
    }

    public void PlayHitVfxByIndex(int index, Vector3 position, Quaternion rotation)
    {
        if (!HasValidHitVfx()) return;
        if (index < 0 || index >= _hitVfxPools.Count) return;

        GameObject vfx = GetFromPool(index);
        vfx.transform.SetPositionAndRotation(position, rotation);
        vfx.SetActive(true);

        StartCoroutine(ReturnToPoolAfterDelay(vfx, index, _vfxLifetime));
    }

    // 여러 VFX 동시 재생 (인덱스 배열)
    public void PlayHitVfxMultiple(int[] indices, Vector3 position)
    {
        PlayHitVfxMultiple(indices, position, Quaternion.identity);
    }

    public void PlayHitVfxMultiple(int[] indices, Vector3 position, Quaternion rotation)
    {
        if (!HasValidHitVfx()) return;

        foreach (int index in indices)
        {
            PlayHitVfxByIndex(index, position, rotation);
        }
    }

    // 모든 VFX 동시 재생
    public void PlayAllHitVfx(Vector3 position)
    {
        PlayAllHitVfx(position, Quaternion.identity);
    }

    public void PlayAllHitVfx(Vector3 position, Quaternion rotation)
    {
        if (!HasValidHitVfx()) return;

        for (int i = 0; i < _hitVfxPools.Count; i++)
        {
            PlayHitVfxByIndex(i, position, rotation);
        }
    }

    private bool HasValidHitVfx()
    {
        return _hitVfxPrefabs != null && _hitVfxPrefabs.Count > 0 && _hitVfxPools != null;
    }

    private GameObject GetFromPool(int index)
    {
        if (_hitVfxPools[index].Count > 0)
        {
            return _hitVfxPools[index].Dequeue();
        }

        return Instantiate(_hitVfxPrefabs[index]);
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject vfx, int poolIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfx.SetActive(false);
        _hitVfxPools[poolIndex].Enqueue(vfx);
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

    public void PlayHitCameraShake()
    {
        if (QuarterViewCamera.Instance != null)
        {
            QuarterViewCamera.Instance.Shake();
        }
    }

    public void PlayCameraShake(float duration, float strength, int vibrato)
    {
        if (QuarterViewCamera.Instance != null)
        {
            QuarterViewCamera.Instance.Shake(duration, strength, vibrato);
        }
    }
}
