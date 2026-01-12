// Enemy 스탯 데이터 (불변 Value Object)
// BGDatabase EnemyStat 테이블과 매핑

using UnityEngine;

// 이 네임스페이스는 C# 9.0의 record 및 init 기능을 사용하기 위해 필요합니다.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
public record EnemyStat(
    string Id,          // PK (예: "SKELETON_001")
    int MaxHP,          // 최대 체력
    int AttackDamage,   // 공격력
    float MoveSpeed,    // 이동 속도
    int GoldReward      // 처치 시 골드 보상
);
