using System.Threading.Tasks;

public interface IOfflineRewardRepository
{
    Task<long> LoadLastLoginTimeAsync();
    Task SaveLastLoginTimeAsync(long unixTimestamp);
    void ForceSaveToDisk();
}
