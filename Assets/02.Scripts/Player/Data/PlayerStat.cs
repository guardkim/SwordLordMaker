// Player 스탯 데이터 (불변 Value Object)
// BGDatabase CodeGen 사용: Type-Safe 데이터 접근

public record PlayerStat(
    string Id,
    double BaseMaxHealth,   // 기본 최대 체력 (double)
    float BaseMoveSpeed,    // 기본 이동 속도 (float)
    int Level,              // 현재 레벨
    double CurrentExp,      // 현재 경험치
    double MaxExp           // 레벨업 필요 경험치
);
