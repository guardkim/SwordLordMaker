using System;
using System.Collections;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;

public class CurrencyManager : DontDestroySingleton<CurrencyManager>
{
    private ICurrencyRepository _repository;
    private Currency _currency;
    private Coroutine _autoSaveCoroutine;
    private WaitForSeconds _autoSaveWait;

    private const float GoldAutoSaveInterval = 60f;

    public event Action<CurrencyType, BigInteger> OnCurrencyChanged;

    public BigInteger Gold => _currency?.Gold ?? BigInteger.Zero;
    public BigInteger Ruby => _currency?.Ruby ?? BigInteger.Zero;

    protected override void Initialize()
    {
        _repository = CreateRepository();
        _autoSaveWait = new WaitForSeconds(GoldAutoSaveInterval);
        LoadCurrency();
    }

    private ICurrencyRepository CreateRepository()
    {
        return new CurrencyRepository();
    }

    private void LoadCurrency()
    {
        _ = LoadCurrencyInternalAsync();
    }

    private async Task LoadCurrencyInternalAsync()
    {
        try
        {
            _currency = await _repository.LoadAsync();
            _currency.OnChanged += HandleCurrencyChanged;

            _autoSaveCoroutine = StartCoroutine(AutoSaveGoldRoutine());

            OnCurrencyChanged?.Invoke(CurrencyType.Gold, _currency.Gold);
            OnCurrencyChanged?.Invoke(CurrencyType.Ruby, _currency.Ruby);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 로드 실패: {e.Message}");
        }
    }

    private void HandleCurrencyChanged(CurrencyType type, BigInteger newValue)
    {
        OnCurrencyChanged?.Invoke(type, newValue);

        if (type == CurrencyType.Ruby)
        {
            SaveRubyImmediate(newValue);
        }
    }

    private void SaveRubyImmediate(BigInteger ruby)
    {
        _ = SaveRubyInternalAsync(ruby);
    }

    private async Task SaveRubyInternalAsync(BigInteger ruby)
    {
        try
        {
            await _repository.SaveRubyAsync(ruby);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 루비 저장 실패: {e.Message}");
        }
    }

    private IEnumerator AutoSaveGoldRoutine()
    {
        while (true)
        {
            yield return _autoSaveWait;
            SaveGold();
        }
    }

    private void SaveGold()
    {
        if (_currency == null) return;
        _ = SaveGoldInternalAsync();
    }

    private async Task SaveGoldInternalAsync()
    {
        try
        {
            await _repository.SaveGoldAsync(_currency.Gold);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 골드 저장 실패: {e.Message}");
        }
    }

    public void AddGold(BigInteger amount)
    {
        _currency?.Add(CurrencyType.Gold, amount);
    }

    public void AddRuby(BigInteger amount)
    {
        _currency?.Add(CurrencyType.Ruby, amount);
    }

    public bool TrySpendGold(BigInteger amount)
    {
        return _currency?.TrySpend(CurrencyType.Gold, amount) ?? false;
    }

    public bool TrySpendRuby(BigInteger amount)
    {
        return _currency?.TrySpend(CurrencyType.Ruby, amount) ?? false;
    }

    public BigInteger GetCurrency(CurrencyType type)
    {
        return _currency?.Get(type) ?? BigInteger.Zero;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveAllSync();
        }
    }

    private void OnApplicationQuit()
    {
        SaveAllSync();
    }

    private void SaveAllSync()
    {
        if (_currency == null) return;

        try
        {
            // 동기식으로 저장 (앱 종료 시 비동기 완료 보장 불가)
            _repository.SaveAsync(_currency).Wait();
            _repository.ForceSaveToDisk();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 저장 실패: {e.Message}");
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            AddGold(new BigInteger(100000));
            Debug.Log("[CurrencyManager] 테스트: 10만 골드 추가");
        }
    }
#endif

    private void OnDestroy()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }

        if (_currency != null)
        {
            _currency.OnChanged -= HandleCurrencyChanged;
        }
    }
}
