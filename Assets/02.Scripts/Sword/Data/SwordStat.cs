// Sword 스탯 데이터 (불변 Value Object)
// BGDatabase CodeGen 사용: Type-Safe 데이터 접근

public record SwordStat(
    string Id,
    double AttackDamage,        // 기본 공격력 (double)
    float Cooldown,             // 쿨타임 (초)
    float MoveSpeed,            // 이동 속도 (float)
    float CritDamageMultiplier, // 치명타 데미지 배율 (2.0 = 2배)
    float CritChance            // 치명타 확률 0~1 (float)
)
{
    // 치명타 시 최종 데미지 계산 (배율 적용)
    public double CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;
        return AttackDamage * CritDamageMultiplier;
    }
};
