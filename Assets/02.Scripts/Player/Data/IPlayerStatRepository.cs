public interface IPlayerStatRepository
{
    PlayerStat Load();
    void Save(PlayerStat stat);
}
