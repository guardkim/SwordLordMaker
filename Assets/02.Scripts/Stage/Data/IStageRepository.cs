using System.Collections.Generic;

public interface IStageRepository
{
    List<StageStat> LoadAll();
    StageStat GetByStageId(int stageId);
    int GetMaxStageId();
}
