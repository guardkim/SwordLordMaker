using System.Collections.Generic;

public interface ISwordStatRepository
{
    List<SwordStat> LoadAll();
    SwordStat GetById(string id);
}
