using System;
using System.Collections;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OfflineRewardManager : DontDestroySingleton<OfflineRewardManager>
{
    private const int MaxOfflineHours = 8;
    private const int MaxOfflineSeconds = MaxOfflineHours * 60 * 60;
    private const float AutoSaveInterval = 60f;

    [Header("▼ 오프라인 보상 조건")]
    [SerializeField] private int _minOfflineSecondsForReward = 1;

    [Header("▼ 오프라인 보상 설정")]
    [SerializeField] private BigInteger _goldPerMinute = new BigInteger(100);
    [SerializeField] private double _expPerMinute = 10.0;

    private IOfflineRewardRepository _repository;
    private bool _hasCheckedOfflineReward;
    private Coroutine _autoSaveCoroutine;

    public event Action<OfflineRewardResult> OnOfflineRewardReady;

    public bool HasPendingReward { get; private set; }
    public OfflineRewardResult PendingReward { get; private set; }

    protected override void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;

        if (PlayerSessionManager.Instance.IsLoggedIn)
        {
            InitializeRepository();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene" && HasPendingReward && PendingReward != null)
        {
            Debug.Log("[OfflineReward] MainScene 로드 완료 - 보상 이벤트 재발생");
            OnOfflineRewardReady?.Invoke(PendingReward);
        }
    }

    private void OnLoginCompleted()
    {
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        string playerName = PlayerSessionManager.Instance.CurrentPlayerName;
        Debug.Log($"[OfflineRewardManager] Repository 초기화: '{playerName}'");

        _repository = new OfflineRewardRepository(playerName);

        CheckOfflineReward();

        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }
        _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        var wait = new WaitForSeconds(AutoSaveInterval);
        while (true)
        {
            yield return wait;
            SaveCurrentTimeSync();
        }
    }

    private void CheckOfflineReward()
    {
        if (_hasCheckedOfflineReward) return;
        _hasCheckedOfflineReward = true;

        _ = CheckOfflineRewardAsync();
    }

    private async Task CheckOfflineRewardAsync()
    {
        try
        {
            long lastLoginTime = await _repository.LoadLastLoginTimeAsync();
            long currentTime = GetCurrentUnixTimestamp();
            long offlineSeconds = currentTime - lastLoginTime;

            Debug.Log($"[OfflineReward] LastLogin: {lastLoginTime}, Current: {currentTime}, Diff: {offlineSeconds}초");

            if (lastLoginTime <= 0)
            {
                Debug.Log("[OfflineReward] 첫 로그인 - 현재 시간 저장");
                await SaveCurrentTimeAsync();
                return;
            }

            if (offlineSeconds < _minOfflineSecondsForReward)
            {
                Debug.Log($"[OfflineReward] 최소 시간 미달: {offlineSeconds}초 < {_minOfflineSecondsForReward}초");
                await SaveCurrentTimeAsync();
                return;
            }

            long clampedSeconds = Math.Min(offlineSeconds, MaxOfflineSeconds);
            OfflineRewardResult reward = CalculateReward(clampedSeconds);

            Debug.Log($"[OfflineReward] 보상 준비: {clampedSeconds}초, Gold: {reward.GoldReward}");

            // 보상 계산 후 즉시 시간 저장 (중복 수령 방지)
            await SaveCurrentTimeAsync();
            Debug.Log("[OfflineReward] 현재 시간 저장 완료 (중복 수령 방지)");

            HasPendingReward = true;
            PendingReward = reward;

            OnOfflineRewardReady?.Invoke(reward);
        }
        catch (Exception e)
        {
            Debug.LogError($"[OfflineRewardManager] 오프라인 보상 확인 실패: {e.Message}");
        }
    }

    private OfflineRewardResult CalculateReward(long offlineSeconds)
    {
        long offlineMinutes = offlineSeconds / 60;

        BigInteger goldReward = _goldPerMinute * (int)offlineMinutes;
        double expReward = _expPerMinute * offlineMinutes;

        TimeSpan offlineDuration = TimeSpan.FromSeconds(offlineSeconds);

        return new OfflineRewardResult(
            offlineDuration,
            goldReward,
            expReward
        );
    }

    public void ClaimReward()
    {
        if (!HasPendingReward || PendingReward == null) return;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(PendingReward.GoldReward);
        }

        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.AddExp(PendingReward.ExpReward);
        }

        HasPendingReward = false;
        PendingReward = null;

        _ = SaveCurrentTimeAsync();

        Debug.Log("[OfflineRewardManager] 오프라인 보상 지급 완료");
    }

    public void SkipReward()
    {
        HasPendingReward = false;
        PendingReward = null;

        _ = SaveCurrentTimeAsync();

        Debug.Log("[OfflineRewardManager] 오프라인 보상 스킵");
    }

    private async Task SaveCurrentTimeAsync()
    {
        try
        {
            long currentTime = GetCurrentUnixTimestamp();
            await _repository.SaveLastLoginTimeAsync(currentTime);
            _repository.ForceSaveToDisk();
        }
        catch (Exception e)
        {
            Debug.LogError($"[OfflineRewardManager] 시간 저장 실패: {e.Message}");
        }
    }

    private long GetCurrentUnixTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCurrentTimeSync();
        }
    }

    private void OnApplicationQuit()
    {
        SaveCurrentTimeSync();
    }

    private void SaveCurrentTimeSync()
    {
        try
        {
            long currentTime = GetCurrentUnixTimestamp();
            _repository.SaveLastLoginTimeAsync(currentTime).Wait();
            _repository.ForceSaveToDisk();
        }
        catch (Exception e)
        {
            Debug.LogError($"[OfflineRewardManager] 종료 시 저장 실패: {e.Message}");
        }
    }

    public void SetRewardRates(BigInteger goldPerMinute, double expPerMinute)
    {
        _goldPerMinute = goldPerMinute;
        _expPerMinute = expPerMinute;
    }

    public static int GetMaxOfflineHours()
    {
        return MaxOfflineHours;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (PlayerSessionManager.HasInstance)
        {
            PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
        }

        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }
    }
}

public class OfflineRewardResult
{
    public TimeSpan OfflineDuration { get; }
    public BigInteger GoldReward { get; }
    public double ExpReward { get; }

    public OfflineRewardResult(TimeSpan offlineDuration, BigInteger goldReward, double expReward)
    {
        OfflineDuration = offlineDuration;
        GoldReward = goldReward;
        ExpReward = expReward;
    }

    public string GetFormattedDuration()
    {
        if (OfflineDuration.TotalHours >= 1)
        {
            return $"{(int)OfflineDuration.TotalHours}시간 {OfflineDuration.Minutes}분";
        }
        return $"{OfflineDuration.Minutes}분";
    }
}
