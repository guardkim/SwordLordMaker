using System;

public interface ICurrencyService
{
    // 상태 조회
    double Gold { get; }
    double Ruby { get; }

    // 재화 추가
    void AddGold(double amount);
    void AddRuby(double amount);

    // 재화 소비
    bool TrySpendGold(double amount);
    bool TrySpendRuby(double amount);

    // 조회
    double GetCurrency(ECurrencyType type);

    // 이벤트
    event Action<ECurrencyType, double> OnCurrencyChanged;
}
