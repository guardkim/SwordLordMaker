using System.Collections.Generic;

public interface IUpgradeRepository
{
    List<UpgradeData> LoadAllUpgradeData();
    UpgradeData GetUpgradeData(string id);
    PlayerUpgradeLevels LoadPlayerLevels();
    void SavePlayerLevels(PlayerUpgradeLevels levels);
}
