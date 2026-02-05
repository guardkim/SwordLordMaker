using System;

public class PlayerStatManager : DontDestroySingleton<PlayerStatManager>, IPlayerStatService
{
    private const double ExpCompareEpsilon = 0.0001;
    private IPlayerStatRepository _repository;
    private PlayerStat _baseStat;

    // IPlayerStatService 상태 조회 구현
    public double BaseMaxHealth => _baseStat?.BaseMaxHealth ?? 100;
    public float BaseMoveSpeed => _baseStat?.BaseMoveSpeed ?? 5f;
    public int Level => _baseStat?.Level ?? 1;
    public double CurrentExp => _baseStat?.CurrentExp ?? 0.0;
    public double MaxExp => _baseStat?.MaxExp ?? 10.0;

    // IPlayerStatService 이벤트 구현
    public event Action<int> OnLevelUp;
    public event Action<double, double> OnExpChanged;

    protected override void Initialize()
    {
        UnityEngine.Debug.Log("[PlayerStatManager] Initialize() 시작");

        PlayerSessionManager.Instance.OnLoginCompleted += OnLoginCompleted;
        UnityEngine.Debug.Log($"[PlayerStatManager] OnLoginCompleted 구독 완료, IsLoggedIn: {PlayerSessionManager.Instance.IsLoggedIn}");

        // 이미 로그인된 상태라면 바로 초기화
        if (PlayerSessionManager.Instance.IsLoggedIn)
        {
            InitializeRepository();
        }
        else
        {
            UnityEngine.Debug.Log("[PlayerStatManager] 아직 로그인 안 됨, OnLoginCompleted 대기");
        }

        // ServiceLocator에 등록
        ServiceLocator.Register<IPlayerStatService>(this);
    }

    private void OnDestroy()
    {
        if (PlayerSessionManager.HasInstance)
        {
            PlayerSessionManager.Instance.OnLoginCompleted -= OnLoginCompleted;
        }

        ServiceLocator.Unregister<IPlayerStatService>();
    }

    private void OnLoginCompleted()
    {
        UnityEngine.Debug.Log("[PlayerStatManager] OnLoginCompleted 이벤트 수신");
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        string playerName = PlayerSessionManager.Instance.CurrentPlayerName;
        UnityEngine.Debug.Log($"[PlayerStatManager] Repository 초기화: '{playerName}'");

        _repository = new PlayerStatRepository(playerName);
        _baseStat = _repository.Load();
    }

    // IPlayerStatService 구현
    public void AddExp(double exp)
    {
        if (_baseStat == null)
        {
            return;
        }

        _baseStat.CurrentExp += exp;
        OnExpChanged?.Invoke(_baseStat.CurrentExp, _baseStat.MaxExp);
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        if (_baseStat == null)
        {
            return;
        }

        if (_baseStat.CurrentExp + ExpCompareEpsilon >= _baseStat.MaxExp)
        {
            int newLevel = _baseStat.Level + 1;
            double newMaxExp = CalculateMaxExp(newLevel);

            _baseStat.CurrentExp -= _baseStat.MaxExp;
            _baseStat.Level = newLevel;
            _baseStat.MaxExp = newMaxExp;

            UnityEngine.Debug.Log($"[PlayerStatManager] 레벨업! Level: {_baseStat.Level}, CurrentExp: {_baseStat.CurrentExp}, MaxExp: {_baseStat.MaxExp}");

            OnLevelUp?.Invoke(_baseStat.Level);
            OnExpChanged?.Invoke(_baseStat.CurrentExp, _baseStat.MaxExp);

            if (_repository != null)
            {
                _repository.Save(_baseStat);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PlayerStatManager] _repository가 null이라 저장 불가");
            }

            CheckLevelUp();
        }
    }

    private double CalculateMaxExp(int level)
    {
        return 10.0 * System.Math.Pow(2, level - 1);
    }

    public void SaveExp(int level, double currentExp, double maxExp)
    {
        if (_baseStat != null)
        {
            _baseStat.Level = level;
            _baseStat.CurrentExp = currentExp;
            _baseStat.MaxExp = maxExp;
            _repository.Save(_baseStat);
        }
    }
}
