using System.Threading.Tasks;

public interface ICurrencyRepository
{
    Task<Currency> LoadAsync();
    Task SaveAsync(Currency currency);
    Task SaveGoldAsync(double gold);
    Task SaveRubyAsync(double ruby);
    void ForceSaveToDisk();
}
