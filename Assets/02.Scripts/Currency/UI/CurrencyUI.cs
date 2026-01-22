using System.Numerics;
using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _rubyText;

    private bool _isGoldDirty;
    private bool _isRubyDirty;

    private BigInteger _cachedGold;
    private BigInteger _cachedRuby;

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

            _cachedGold = CurrencyManager.Instance.Gold;
            _cachedRuby = CurrencyManager.Instance.Ruby;
            _isGoldDirty = true;
            _isRubyDirty = true;
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.HasInstance)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }

    private void HandleCurrencyChanged(CurrencyType type, BigInteger newValue)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                _cachedGold = newValue;
                _isGoldDirty = true;
                break;
            case CurrencyType.Ruby:
                _cachedRuby = newValue;
                _isRubyDirty = true;
                break;
        }
    }

    private void LateUpdate()
    {
        if (_isGoldDirty && _goldText != null)
        {
            _goldText.text = CurrencyFormatter.FormatAbbreviated(_cachedGold);
            _isGoldDirty = false;
        }

        if (_isRubyDirty && _rubyText != null)
        {
            _rubyText.text = CurrencyFormatter.FormatAbbreviated(_cachedRuby);
            _isRubyDirty = false;
        }
    }
}
