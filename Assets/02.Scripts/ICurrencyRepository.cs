using System.Numerics;
using System.Threading.Tasks;

public interface ICurrencyRepository
{
    Task<Currency> LoadAsync();
    Task SaveAsync(Currency currency);
    Task SaveGoldAsync(BigInteger gold);
    Task SaveRubyAsync(BigInteger ruby);
    void ForceSaveToDisk();
}
