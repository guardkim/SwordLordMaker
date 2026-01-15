using System;
using System.Numerics;
using UnityEngine;
using System.Collections.Generic;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class DamageFloaterManager : DontDestroySingleton<DamageFloaterManager>
{
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

    // -----------------------------------------------------------
    // 4. BigInteger 데미지 (무한 스케일링 지원)
    // -----------------------------------------------------------
    public void ShowDamage(DamageStyle style, BigInteger damage, Vector3 spawnPoint, bool isCrit)
    {
        if (DamageFloaterPrefab == null) return;

        GameObject obj = Instantiate(DamageFloaterPrefab, spawnPoint, Quaternion.identity);
        DamageFloater floater = obj.GetComponent<DamageFloater>();

        if (floater != null)
        {
            floater.ApplyOption(SingleFloaterOption);
            string formattedDamage = CurrencyFormatter.FormatAbbreviated(damage);
            floater.ShowFormattedDamage(formattedDamage, style, isCrit);
        }
    }
}