// Sword 스탯 데이터 (불변 Value Object)
// BigInteger 사용: 방치형 게임의 무한 스케일링 지원

using System.Numerics;

public record SwordStat(
    string Id,
    BigInteger AttackDamage,    // 기본 공격력 (BigInteger)
    float Cooldown,             // 쿨타임 (초)
    float MoveSpeed,            // 이동 속도 (float)
    float CritDamageMultiplier, // 치명타 데미지 배율 (2.0 = 2배)
    float CritChance            // 치명타 확률 0~1 (float)
)
{
    // 치명타 시 최종 데미지 계산 (배율 적용)
    public BigInteger CalculateDamage(bool isCrit)
    {
        if (!isCrit) return AttackDamage;

        // AttackDamage * CritDamageMultiplier (배율)
        return new BigInteger((double)AttackDamage * CritDamageMultiplier);
    }
};
