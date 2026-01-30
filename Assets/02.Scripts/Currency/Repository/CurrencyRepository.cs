using System.Threading.Tasks;
using BansheeGz.BGDatabase;
using UnityEngine;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly string _playerName;
    private DB_PlayerProfile _playerEntity;

    public CurrencyRepository(string playerName)
    {
        _playerName = playerName;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _playerEntity = DB_PlayerProfile.GetEntity(_playerName);

        if (_playerEntity == null)
        {
            _playerEntity = CreateNewPlayerEntity();
        }
    }

    private DB_PlayerProfile CreateNewPlayerEntity()
    {
        DB_PlayerProfile entity = DB_PlayerProfile.NewEntity(e =>
        {
            e.F_name = _playerName;
            e.F_Gold = 0;
            e.F_Ruby = 0;
        });
        return entity;
    }

    public Task<Currency> LoadAsync()
    {
        if (_playerEntity == null)
        {
            return Task.FromResult(new Currency(0, 0));
        }

        double gold = _playerEntity.F_Gold;
        double ruby = _playerEntity.F_Ruby;

        return Task.FromResult(new Currency(gold, ruby));
    }

    public Task SaveAsync(Currency currency)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Gold = currency.Gold;
        _playerEntity.F_Ruby = currency.Ruby;

        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        return Task.CompletedTask;
    }

    public Task SaveGoldAsync(double gold)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Gold = gold;
        ForceSaveToDisk();
        return Task.CompletedTask;
    }

    public Task SaveRubyAsync(double ruby)
    {
        if (_playerEntity == null) return Task.CompletedTask;

        _playerEntity.F_Ruby = ruby;
        ForceSaveToDisk();
        return Task.CompletedTask;
    }

    public void ForceSaveToDisk()
    {
        BGRepo.I.Save();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
