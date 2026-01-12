using System.Collections.Generic;

public interface IEnemyStatRepository
{
    List<EnemyStat> LoadAll();
    EnemyStat GetById(string id);
}
