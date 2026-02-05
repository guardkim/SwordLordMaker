using System;

public interface IEnemySpawner
{
    // 상태 조회
    int AliveEnemyCount { get; }
    bool IsBossSpawned { get; }
    bool IsBossAlive { get; }

    // 스폰 요청
    EnemyAI SpawnEnemy(string statId, StageStat stageStat);
    EnemyAI SpawnBoss(string bossStatId, StageStat stageStat);

    // 관리 요청
    void ReturnEnemy(EnemyAI enemy);
    void ReturnAll();
    void ResetBossState();

    // 이벤트 (사실 알림)
    event Action<EnemyAI> OnEnemyDied;
    event Action<EnemyAI> OnBossDied;
    event Action<EnemyAI> OnBossSpawned;
    event Action<EnemyStat> OnEnemyDefeatedWithStat;
}
