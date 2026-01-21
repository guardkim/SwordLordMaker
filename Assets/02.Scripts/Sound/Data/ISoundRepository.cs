using System.Collections.Generic;

public interface ISoundRepository
{
    SfxData GetSfxData(string id);
    BgmData GetBgmData(string id);
    List<SfxData> LoadAllSfxData();
    List<BgmData> LoadAllBgmData();
}
