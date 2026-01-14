using System.Numerics;
using UnityEngine;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _hpBarSprite;

    private BigInteger _maxHP;
    private Vector3 _originalScale;

    private void Awake()
    {
        if (_hpBarSprite)
        {
            _originalScale = _hpBarSprite.transform.localScale;
        }
    }

    public void Initialize(BigInteger maxHP)
    {
        _maxHP = maxHP;

        if (_hpBarSprite)
        {
            _hpBarSprite.transform.localScale = _originalScale;
        }
    }

    public void UpdateHP(BigInteger currentHP)
    {
        if (_maxHP <= BigInteger.Zero || !_hpBarSprite)
        {
            return;
        }

        // BigInteger 비율 계산 (float 변환)
        float ratio = CalculateRatio(currentHP, _maxHP);

        _hpBarSprite.transform.localScale = new Vector3(
            _originalScale.x * ratio,
            _originalScale.y,
            _originalScale.z
        );
    }

    private float CalculateRatio(BigInteger current, BigInteger max)
    {
        if (max == BigInteger.Zero) return 0f;

        // 큰 숫자도 정확한 비율 계산을 위해 double 사용
        double ratio = (double)current / (double)max;
        return Mathf.Clamp01((float)ratio);
    }

    public void Reset()
    {
        _maxHP = BigInteger.Zero;

        if (_hpBarSprite)
        {
            _hpBarSprite.transform.localScale = _originalScale;
        }
    }
}