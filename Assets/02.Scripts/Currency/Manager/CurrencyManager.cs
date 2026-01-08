using System;
using System.Collections;
using System.Numerics;
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
        _repository = new CurrencyRepository();
        _autoSaveWait = new WaitForSeconds(GoldAutoSaveInterval);
        LoadCurrencyAsync();
    }

    private async void LoadCurrencyAsync()
    {
        _currency = await _repository.LoadAsync();
        _currency.OnChanged += HandleCurrencyChanged;

        _autoSaveCoroutine = StartCoroutine(AutoSaveGoldRoutine());

        OnCurrencyChanged?.Invoke(CurrencyType.Gold, _currency.Gold);
        OnCurrencyChanged?.Invoke(CurrencyType.Ruby, _currency.Ruby);
    }

    private void HandleCurrencyChanged(CurrencyType type, BigInteger newValue)
    {
        OnCurrencyChanged?.Invoke(type, newValue);

        if (type == CurrencyType.Ruby)
        {
            SaveRubyImmediate(newValue);
        }
    }

    private async void SaveRubyImmediate(BigInteger ruby)
    {
        await _repository.SaveRubyAsync(ruby);
    }

    private IEnumerator AutoSaveGoldRoutine()
    {
        while (true)
        {
            yield return _autoSaveWait;
            SaveGold();
        }
    }

    private async void SaveGold()
    {
        if (_currency == null)
        {
            return;
        }
        await _repository.SaveGoldAsync(_currency.Gold);
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
            SaveAll();
        }
    }

    private void OnApplicationQuit()
    {
        SaveAll();
    }

    private async void SaveAll()
    {
        if (_currency == null)
        {
            return;
        }

        await _repository.SaveAsync(_currency);
        _repository.ForceSaveToDisk();
    }

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
