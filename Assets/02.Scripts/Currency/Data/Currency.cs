using System;

public class Currency
{
    private double _gold;
    private double _ruby;

    public double Gold => _gold;
    public double Ruby => _ruby;

    public event Action<ECurrencyType, double> OnChanged;

    // 재화 데이터 객체를 초기화합니다.
    // gold: 초기 골드 값 (일반 재화, 1분 주기 오토세이브).
    // ruby: 초기 루비 값 (유료 재화, 변경 즉시 저장).
    public Currency(double gold, double ruby)
    {
        _gold = gold;
        _ruby = ruby;
    }

    public void Add(ECurrencyType type, double amount)
    {
        if (amount <= 0)
        {
            return;
        }

        switch (type)
        {
            case ECurrencyType.Gold:
                _gold += amount;
                OnChanged?.Invoke(ECurrencyType.Gold, _gold);
                break;
            case ECurrencyType.Ruby:
                _ruby += amount;
                OnChanged?.Invoke(ECurrencyType.Ruby, _ruby);
                break;
        }
    }

    public bool TrySpend(ECurrencyType type, double amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        switch (type)
        {
            case ECurrencyType.Gold:
                if (_gold < amount)
                {
                    return false;
                }
                _gold -= amount;
                OnChanged?.Invoke(ECurrencyType.Gold, _gold);
                return true;

            case ECurrencyType.Ruby:
                if (_ruby < amount)
                {
                    return false;
                }
                _ruby -= amount;
                OnChanged?.Invoke(ECurrencyType.Ruby, _ruby);
                return true;

            default:
                return false;
        }
    }

    public double Get(ECurrencyType type)
    {
        return type switch
        {
            ECurrencyType.Gold => _gold,
            ECurrencyType.Ruby => _ruby,
            _ => 0
        };
    }
}
