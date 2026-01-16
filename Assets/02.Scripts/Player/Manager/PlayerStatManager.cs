using System;
using System.Numerics;

public class PlayerStatManager : DontDestroySingleton<PlayerStatManager>
{
    private const double ExpCompareEpsilon = 0.0001;    
    private IPlayerStatRepository _repository;
    private PlayerStat _baseStat;

    public BigInteger BaseMaxHealth => _baseStat?.BaseMaxHealth ?? new BigInteger(100);
    public float BaseMoveSpeed => _baseStat?.BaseMoveSpeed ?? 5f;
    public int Level => _baseStat?.Level ?? 1;
    public double CurrentExp => _baseStat?.CurrentExp ?? 0.0;
    public double MaxExp => _baseStat?.MaxExp ?? 10.0;

    public event Action<int> OnLevelUp;  // (newLevel)
    public event Action<double, double> OnExpChanged;  // (currentExp, maxExp)

    protected override void Initialize()
    {
        _repository = new PlayerStatRepository();
        _baseStat = _repository.Load();
    }

    public void AddExp(double exp)
    {
        if (_baseStat == null)
        {
            return;
        }

        _baseStat = _baseStat with
        {
            CurrentExp = _baseStat.CurrentExp + exp
        };

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
            _baseStat = _baseStat with
            {
                CurrentExp = _baseStat.CurrentExp - _baseStat.MaxExp,
                Level = _baseStat.Level + 1,
                MaxExp = CalculateMaxExp(_baseStat.Level + 1)
            };

            OnLevelUp?.Invoke(_baseStat.Level);
            OnExpChanged?.Invoke(_baseStat.CurrentExp, _baseStat.MaxExp);

            _repository.Save(_baseStat);

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
            _baseStat = _baseStat with
            {
                Level = level,
                CurrentExp = currentExp,
                MaxExp = maxExp
            };
            _repository.Save(_baseStat);
        }
    }
}
