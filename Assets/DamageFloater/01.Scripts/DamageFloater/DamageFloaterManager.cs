using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class DamageFloaterManager : MonoBehaviour
{
    // 어디서든 접근 가능한 싱글턴 인스턴스
    public static DamageFloaterManager Instance;

    [Header("Reference")]
    [Tooltip("DamageFloater 컴포넌트가 붙어있는 프리팹")]
    public GameObject DamageFloaterPrefab;

    [Header("Options")]
    [Tooltip("단발성 평타(Single) 전용 옵션")]
    public FloaterOption SingleFloaterOption = FloaterOption.Default;

    [Tooltip("연타 스킬(Multi) 전용 옵션")]
    public FloaterOption MultiFloaterOption = FloaterOption.Default;

    [Tooltip("Temp(임시) 옵션")]
    public GameObject SpawnPos; // temp
    public bool IsMulti;
    private readonly List<int> _tempList = new List<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // -----------------------------------------------------------
    // 1. 옵션 설정 함수 (런타임에 옵션 변경 가능)
    // -----------------------------------------------------------
    public void SetSingleOption(FloaterOption option)
    {
        SingleFloaterOption = option;
    }

    public void SetMultiOption(FloaterOption option)
    {
        MultiFloaterOption = option;
    }

    // 기존 함수 (하위 호환성을 위해 유지하거나 isCrit=false로 연결)
    public void ShowDamage(DamageStyle style, int damage, Vector3 spawnPoint)
    {
        ShowDamage(style, damage, spawnPoint, false);
    }
    // -----------------------------------------------------------
    // 2. 단일 데미지 (Single Hit) -> singleFloaterOption 사용
    // -----------------------------------------------------------
    public void ShowDamage(DamageStyle style, int damage, Vector3 spawnPoint, bool isCrit)
    {
        if (DamageFloaterPrefab == null) return;

        GameObject obj = Instantiate(DamageFloaterPrefab, spawnPoint, Quaternion.identity);
        DamageFloater floater = obj.GetComponent<DamageFloater>();

        if (floater)
        {
            floater.ApplyOption(SingleFloaterOption);
            // 배열로 변환해서 넘길 때 isCrit 전달
            floater.ShowDamage(new[] { damage }, style, isCrit);
        }
    }

    // -----------------------------------------------------------
    // 3. 연타 데미지 (Multi Hit) -> multiFloaterOption 사용
    // -----------------------------------------------------------
    public void ShowDamage(DamageStyle style, List<int> damages, Vector3 spawnPoint, bool isCrit = false)
    {
        if (DamageFloaterPrefab == null) return;
        if (damages == null || damages.Count == 0) return;

        GameObject obj = Instantiate(DamageFloaterPrefab, spawnPoint, Quaternion.identity);
        DamageFloater floater = obj.GetComponent<DamageFloater>();

        if (floater != null)
        {
            floater.ApplyOption(MultiFloaterOption);
            floater.ShowDamage(damages.ToArray(), style, isCrit);
        }
    }
    
    /// <summary>
    ///  테스트 Scene을 위한 함수
    /// </summary>

    public void ToggleMulti()
    {
        IsMulti = !IsMulti;
    }
    private void Start()
    {
        _tempList.Add(352342);
        _tempList.Add(2455);
        _tempList.Add(69384);
        _tempList.Add(39483);
        _tempList.Add(322);
        _tempList.Add(28593);
        _tempList.Add(21909);
        _tempList.Add(592830217);
        _tempList.Add(559934903);
    }
    private void Update()
    {
        //TODO : Demo용 코드입니다. Manager 실 사용시에는 Update를 지워주세요
        if (ModeChange.Instance.CurrentType != EModeType.DamageFloater) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isCrit = Convert.ToBoolean(Random.Range(0, 2));
            if (!IsMulti)
            {
                int random = Random.Range(9999, 99999);
                ShowDamage((DamageStyle)SingleFloaterOption.damageStyle,random, SpawnPos.transform.position, isCrit);
                
            }
            else
            {
                int random = Random.Range(2, 11);
                _tempList.Clear();
                for (int i = 0; i < random; i++)
                {
                    int temp = Random.Range(999, 999999);
                    _tempList.Add(temp);
                }
                ShowDamage((DamageStyle)MultiFloaterOption.damageStyle,_tempList, SpawnPos.transform.position, isCrit);
                    
            }
        }
    }
}