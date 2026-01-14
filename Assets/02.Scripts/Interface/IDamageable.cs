using System.Numerics;

public interface IDamageable
{
    void TakeDamage(BigInteger damage, bool isCrit);
}
