using System;

public interface IStageService
{
    // 상태 조회
    int CurrentStageId { get; }
    string CurrentStageName { get; }
    StageStat CurrentStageStat { get; }

    // 게임 진행
    void StartGame();
    void StartStage(int stageId);
    void SpawnBoss();
    void RestartCurrentStage();
    void RestartFromStage(int stageId);
    void OnPlayerDied();

    // 이벤트
    event Action<int> OnStageStarted;
    event Action<int> OnStageCleared;
    event Action OnAllStagesCleared;
    event Action<EnemyAI> OnBossSpawned;
}
