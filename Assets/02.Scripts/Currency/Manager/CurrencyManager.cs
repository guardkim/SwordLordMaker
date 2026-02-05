using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class CurrencyManager : DontDestroySingleton<CurrencyManager>, ICurrencyService
{
    private ICurrencyRepository _repository;
    private Currency _currency;
    private Coroutine _autoSaveCoroutine;
    private WaitForSeconds _autoSaveWait;

    private const float GoldAutoSaveInterval = 60f;

    // ICurrencyService 이벤트 구현
    public event Action<ECurrencyType, double> OnCurrencyChanged;

    // ICurrencyService 상태 조회 구현
    public double Gold => _currency?.Gold ?? 0;
    public double Ruby => _currency?.Ruby ?? 0;

    protected override void Initialize()
    {
        Debug.Log($"[CurrencyManager] Initialize 호출됨");
        Debug.Log($"[CurrencyManager] PlayerSessionManager.IsLoggedIn: {PlayerSessionManager.Instance.IsLoggedIn}");

        _autoSaveWait = new WaitForSeconds(GoldAutoSaveInterval);

        PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;

        if (PlayerSessionManager.Instance.IsLoggedIn)
        {
            Debug.Log("[CurrencyManager] 이미 로그인됨 → InitializeRepository 호출");
            InitializeRepository();
        }
        else
        {
            Debug.Log("[CurrencyManager] 아직 로그인 안 됨 → OnLoginCompleted 대기");
        }

        // ServiceLocator에 등록
        ServiceLocator.Register<ICurrencyService>(this);
    }

    private void OnLoginCompleted()
    {
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        string playerName = PlayerSessionManager.Instance.CurrentPlayerName;
        Debug.Log($"[CurrencyManager] Repository 초기화: '{playerName}'");

        _repository = new CurrencyRepository(playerName);
        LoadCurrency();
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

            OnCurrencyChanged?.Invoke(ECurrencyType.Gold, _currency.Gold);
            OnCurrencyChanged?.Invoke(ECurrencyType.Ruby, _currency.Ruby);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 로드 실패: {e.Message}");
        }
    }

    private void HandleCurrencyChanged(ECurrencyType type, double newValue)
    {
        OnCurrencyChanged?.Invoke(type, newValue);

        if (type == ECurrencyType.Ruby)
        {
            SaveRubyImmediate(newValue);
        }
    }

    private void SaveRubyImmediate(double ruby)
    {
        _ = SaveRubyInternalAsync(ruby);
    }

    private async Task SaveRubyInternalAsync(double ruby)
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
            _repository.ForceSaveToDisk();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CurrencyManager] 골드 저장 실패: {e.Message}");
        }
    }

    // ICurrencyService 구현
    public void AddGold(double amount)
    {
        _currency?.Add(ECurrencyType.Gold, amount);
    }

    public void AddRuby(double amount)
    {
        _currency?.Add(ECurrencyType.Ruby, amount);
    }

    public bool TrySpendGold(double amount)
    {
        if (!_currency.TrySpend(ECurrencyType.Gold, amount)) return false;
        SaveGold();
        return true;
    }

    public bool TrySpendRuby(double amount)
    {
        return _currency?.TrySpend(ECurrencyType.Ruby, amount) ?? false;
    }

    public double GetCurrency(ECurrencyType type)
    {
        return _currency?.Get(type) ?? 0;
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
            AddGold(1000000000);
            Debug.Log("[CurrencyManager] 테스트: 10억 골드 추가");
        }
    }
#endif

    private void OnDestroy()
    {
        if (PlayerSessionManager.HasInstance)
        {
            PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
        }

        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }

        if (_currency != null)
        {
            _currency.OnChanged -= HandleCurrencyChanged;
        }

        ServiceLocator.Unregister<ICurrencyService>();
    }
}
