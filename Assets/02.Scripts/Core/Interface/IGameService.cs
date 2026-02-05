using System;

public interface IGameService
{
    // 플레이어 등록
    void RegisterPlayer(PlayerHealth player);
    void UnregisterPlayer();

    // 이벤트
    event Action OnPlayerDeath;
    event Action OnPlayerRevive;
    event Action<int> OnRequestStageRestart;
}
