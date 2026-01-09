using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private int _maxHP;

    public void Initialize(int maxHP)
    {
        _maxHP = maxHP;

        if (_slider != null)
        {
            _slider.value = 1f;
        }
    }

    public void UpdateHP(int currentHP)
    {
        if (_maxHP <= 0 || _slider == null)
        {
            return;
        }

        _slider.value = (float)currentHP / _maxHP;
    }

    public void Reset()
    {
        _maxHP = 0;

        if (_slider != null)
        {
            _slider.value = 1f;
        }
    }
}
