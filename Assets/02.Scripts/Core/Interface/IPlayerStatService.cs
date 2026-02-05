using System;

public interface IPlayerStatService
{
    // 상태 조회
    int Level { get; }
    double CurrentExp { get; }
    double MaxExp { get; }
    double BaseMaxHealth { get; }
    float BaseMoveSpeed { get; }

    // 경험치 추가
    void AddExp(double amount);

    // 이벤트
    event Action<int> OnLevelUp;
    event Action<double, double> OnExpChanged;
}
