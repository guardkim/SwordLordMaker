using System.Numerics;
using System.Threading.Tasks;
using BansheeGz.BGDatabase;
using UnityEngine;

public class CurrencyRepository : ICurrencyRepository
{
    private const string TableName = "PlayerProfile";
    private const string GoldField = "Gold";
    private const string RubyField = "Ruby";

    private BGMetaEntity _meta;
    private BGEntity _playerEntity;

    public CurrencyRepository()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _meta = BGRepo.I[TableName];
        if (_meta == null)
        {
            Debug.LogError($"[CurrencyRepository] 테이블을 찾을 수 없습니다: {TableName}");
            return;
        }

        if (_meta.CountEntities > 0)
        {
            _playerEntity = _meta.GetEntity(0);
        }
        else
        {
            _playerEntity = _meta.NewEntity();
            _playerEntity.Set(GoldField, "0");
            _playerEntity.Set(RubyField, "0");
        }
    }

    public Task<Currency> LoadAsync()
    {
        if (_playerEntity == null)
        {
            return Task.FromResult(new Currency(BigInteger.Zero, BigInteger.Zero));
        }

        string goldStr = _playerEntity.Get<string>(GoldField) ?? "0";
        string rubyStr = _playerEntity.Get<string>(RubyField) ?? "0";

        BigInteger gold = BigInteger.TryParse(goldStr, out var g) ? g : BigInteger.Zero;
        BigInteger ruby = BigInteger.TryParse(rubyStr, out var r) ? r : BigInteger.Zero;

        return Task.FromResult(new Currency(gold, ruby));
    }

    public Task SaveAsync(Currency currency)
    {
        if (_playerEntity == null)
        {
            return Task.CompletedTask;
        }

        _playerEntity.Set(GoldField, currency.Gold.ToString());
        _playerEntity.Set(RubyField, currency.Ruby.ToString());
        return Task.CompletedTask;
    }

    public Task SaveGoldAsync(BigInteger gold)
    {
        if (_playerEntity == null)
        {
            return Task.CompletedTask;
        }

        _playerEntity.Set(GoldField, gold.ToString());
        return Task.CompletedTask;
    }

    public Task SaveRubyAsync(BigInteger ruby)
    {
        if (_playerEntity == null)
        {
            return Task.CompletedTask;
        }

        _playerEntity.Set(RubyField, ruby.ToString());
        ForceSaveToDisk();
        return Task.CompletedTask;
    }

    public void ForceSaveToDisk()
    {
        BGRepo.I.Save();
    }
}
