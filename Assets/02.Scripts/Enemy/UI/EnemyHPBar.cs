using UnityEngine;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _hpBarSprite;

    private int _maxHP;
    private Vector3 _originalScale; 

    private void Awake()
    {
        if (_hpBarSprite)
        {
            _originalScale = _hpBarSprite.transform.localScale;
        }
    }

    public void Initialize(int maxHP)
    {
        _maxHP = maxHP;

        if (_hpBarSprite)
        {
            _hpBarSprite.transform.localScale = _originalScale;
        }
    }

    public void UpdateHP(int currentHP)
    {
        if (_maxHP <= 0 || !_hpBarSprite)
        {
            return;
        }

        float ratio = Mathf.Clamp01((float)currentHP / _maxHP);

        _hpBarSprite.transform.localScale = new Vector3(
            _originalScale.x * ratio, 
            _originalScale.y, 
            _originalScale.z
        );
    }

    public void Reset()
    {
        _maxHP = 0;

        if (_hpBarSprite)
        {
            _hpBarSprite.transform.localScale = _originalScale;
        }
    }
}