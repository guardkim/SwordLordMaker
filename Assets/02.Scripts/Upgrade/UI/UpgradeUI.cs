using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    [Header("▼ 플레이어 강화 슬롯")]
    [SerializeField] private UpgradeSlotUI _healthSlot;
    [SerializeField] private UpgradeSlotUI _moveSpeedSlot;

    [Header("▼ 검 강화 슬롯")]
    [SerializeField] private UpgradeSlotUI _attackDamageSlot;
    [SerializeField] private UpgradeSlotUI _cooldownSlot;
    [SerializeField] private UpgradeSlotUI _swordMoveSpeedSlot;
    [SerializeField] private UpgradeSlotUI _critDamageSlot;
    [SerializeField] private UpgradeSlotUI _critChanceSlot;

    [Header("▼ 패널")]
    [SerializeField] private GameObject _panel;

    private void Start()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        // 플레이어 강화
        if (_healthSlot != null)
        {
            _healthSlot.Initialize(UpgradeId.PlayerHealth);
        }

        if (_moveSpeedSlot != null)
        {
            _moveSpeedSlot.Initialize(UpgradeId.PlayerMoveSpeed);
        }

        // 검 강화
        if (_attackDamageSlot != null)
        {
            _attackDamageSlot.Initialize(UpgradeId.SwordAttackDamage);
        }

        if (_cooldownSlot != null)
        {
            _cooldownSlot.Initialize(UpgradeId.SwordCooldown);
        }

        if (_swordMoveSpeedSlot != null)
        {
            _swordMoveSpeedSlot.Initialize(UpgradeId.SwordMoveSpeed);
        }

        if (_critDamageSlot != null)
        {
            _critDamageSlot.Initialize(UpgradeId.SwordCritDamage);
        }

        if (_critChanceSlot != null)
        {
            _critChanceSlot.Initialize(UpgradeId.SwordCritChance);
        }
    }

    public void Show()
    {
        if (_panel != null)
        {
            _panel.SetActive(true);
        }

        RefreshAll();
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (_panel != null)
        {
            if (_panel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    private void RefreshAll()
    {
        _healthSlot?.Refresh();
        _moveSpeedSlot?.Refresh();
        _attackDamageSlot?.Refresh();
        _cooldownSlot?.Refresh();
        _swordMoveSpeedSlot?.Refresh();
        _critDamageSlot?.Refresh();
        _critChanceSlot?.Refresh();
    }
}
