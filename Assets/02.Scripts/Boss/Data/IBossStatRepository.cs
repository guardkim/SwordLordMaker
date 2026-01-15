using System.Collections.Generic;

public interface IBossStatRepository
{
    List<BossStat> LoadAll();
    BossStat GetById(string id);
}
