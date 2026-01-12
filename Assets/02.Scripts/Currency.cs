using System;
using System.Numerics;

public class Currency
{
    private BigInteger _gold;
    private BigInteger _ruby;

    public event Action<CurrencyType, BigInteger> OnChanged;

    public BigInteger Gold => _gold;
    public BigInteger Ruby => _ruby;

    // 재화 데이터 객체를 초기화합니다.
    // gold: 초기 골드 값 (일반 재화, 1분 주기 오토세이브).
    // ruby: 초기 루비 값 (유료 재화, 변경 즉시 저장).
    public Currency(BigInteger gold, BigInteger ruby)
    {
        _gold = gold;
        _ruby = ruby;
    }

    public void Add(CurrencyType type, BigInteger amount)
    {
        if (amount <= 0)
        {
            return;
        }

        switch (type)
        {
            case CurrencyType.Gold:
                _gold += amount;
                OnChanged?.Invoke(CurrencyType.Gold, _gold);
                break;
            case CurrencyType.Ruby:
                _ruby += amount;
                OnChanged?.Invoke(CurrencyType.Ruby, _ruby);
                break;
        }
    }

    public bool TrySpend(CurrencyType type, BigInteger amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        switch (type)
        {
            case CurrencyType.Gold:
                if (_gold < amount)
                {
                    return false;
                }
                _gold -= amount;
                OnChanged?.Invoke(CurrencyType.Gold, _gold);
                return true;

            case CurrencyType.Ruby:
                if (_ruby < amount)
                {
                    return false;
                }
                _ruby -= amount;
                OnChanged?.Invoke(CurrencyType.Ruby, _ruby);
                return true;

            default:
                return false;
        }
    }

    public BigInteger Get(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => _gold,
            CurrencyType.Ruby => _ruby,
            _ => BigInteger.Zero
        };
    }
}
