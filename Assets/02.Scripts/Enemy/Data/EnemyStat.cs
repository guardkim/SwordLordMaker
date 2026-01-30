// Enemy 스탯 데이터 (불변 Value Object)
// BGDatabase CodeGen 사용: Type-Safe 데이터 접근

// C# 9.0의 record 및 init 기능을 위해 필요합니다.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

public record EnemyStat(
    string Id,              // PK (예: "SKELETON_001")
    double MaxHP,           // 최대 체력 (double)
    double AttackDamage,    // 공격력 (double)
    float MoveSpeed,        // 이동 속도 (float)
    double GoldReward,      // 처치 시 골드 보상 (double)
    double Exp              // 처치 시 경험치 보상 (double)
);
