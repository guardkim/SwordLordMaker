// Stage 스탯 데이터 (불변 Value Object)
// BGDatabase StageStat 테이블과 매핑
public record StageStat(
    int StageId,        // 스테이지 번호 (1, 2, 3...)
    string StageName,   // 스테이지 이름 (예: "1-1 해골의숲")
    string EnemyStatId, // 스폰할 Enemy 타입 (EnemyStat.Id 참조)
    int SpawnCount      // 스폰할 Enemy 수량
);
