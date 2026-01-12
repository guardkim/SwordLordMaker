using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("▼ 참조")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Image _healthFillImage;

    private void Start()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(int current, int max)
    {
        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = (float)current / max;
        }
    }
}
