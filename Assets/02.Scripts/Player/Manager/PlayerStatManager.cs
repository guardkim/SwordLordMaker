using System.Numerics;

public class PlayerStatManager : DontDestroySingleton<PlayerStatManager>
{
    private IPlayerStatRepository _repository;
    private PlayerStat _baseStat;

    public BigInteger BaseMaxHealth => _baseStat?.BaseMaxHealth ?? new BigInteger(100);
    public float BaseMoveSpeed => _baseStat?.BaseMoveSpeed ?? 5f;

    protected override void Initialize()
    {
        _repository = new PlayerStatRepository();
        _baseStat = _repository.Load();
    }
}
