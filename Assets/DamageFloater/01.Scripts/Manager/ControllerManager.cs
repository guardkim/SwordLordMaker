using System;
using UnityEngine;
using TMPro;

public class ControllerManager : MonoBehaviour
{
    //[Header("Controllers Assignment")]
    

    [Header("UI (Optional)")]
    public TextMeshProUGUI ModeText;

    [Header("■ 기본 쿨타임 설정")]
    [SerializeField] private float _baseCooldown = 20f;

    private bool _autoFireEnabled = true;

    private AdelFlyingSwordController _adelController;
    private HypoSwordController _hypoController;
    private PixelSwordController _pixelController;
    
    // 각 컨트롤러별 개별 쿨타임 타이머
    private float _adelCooldownTimer;
    private float _hypoCooldownTimer;
    private float _pixelCooldownTimer;

    private float GetCooldown(BaseSwordController controller)
    {
        float cooldownMultiplier = 1f;

        if (controller is AdelFlyingSwordController adel)
            cooldownMultiplier = adel.SwordStat?.Cooldown ?? 1f;
        else if (controller is HypoSwordController hypo)
            cooldownMultiplier = hypo.SwordStat?.Cooldown ?? 1f;
        else if (controller is PixelSwordController pixel)
            cooldownMultiplier = pixel.SwordStat?.Cooldown ?? 1f;

        return _baseCooldown * cooldownMultiplier;
    }
    private void Awake()
    {
        _adelController = GetComponentInChildren<AdelFlyingSwordController>();
        _hypoController = GetComponentInChildren<HypoSwordController>();
        _pixelController = GetComponentInChildren<PixelSwordController>();
    }

    private void Start()
    {
        

        FireAll();
    }

    private void Update()
    {
        if (!_autoFireEnabled) return;

        // Adel 쿨타임
        if (_adelController != null)
        {
            _adelCooldownTimer += Time.deltaTime;
            float adelCooldown = GetCooldown(_adelController);
            if (_adelCooldownTimer >= adelCooldown)
            {
                _adelController.Fire();
                _adelCooldownTimer = 0f;
            }
        }

        // Hypo 쿨타임
        if (_hypoController != null)
        {
            _hypoCooldownTimer += Time.deltaTime;
            float hypoCooldown = GetCooldown(_hypoController);
            if (_hypoCooldownTimer >= hypoCooldown)
            {
                _hypoController.Fire();
                _hypoCooldownTimer = 0f;
            }
        }

        // Pixel 쿨타임
        if (_pixelController != null)
        {
            _pixelCooldownTimer += Time.deltaTime;
            float pixelCooldown = GetCooldown(_pixelController);
            if (_pixelCooldownTimer >= pixelCooldown)
            {
                _pixelController.Fire();
                _pixelCooldownTimer = 0f;
            }
        }

        // [검증용] Space 키 수동 발사 (모두 발사)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireAll();
        }
    }

    public void SetAutoFire(bool enabled)
    {
        _autoFireEnabled = enabled;
        if (enabled)
        {
            _adelCooldownTimer = 0f;
            _hypoCooldownTimer = 0f;
            _pixelCooldownTimer = 0f;
        }
    }


    // 모든 검을 즉시 발사
    public void FireAll()
    {
        _adelController?.Fire();
        _hypoController?.Fire();
        _pixelController?.Fire();

        _adelCooldownTimer = 0f;
        _hypoCooldownTimer = 0f;
        _pixelCooldownTimer = 0f;
    }

    // 특정 타입의 검만 발사
    public void Fire(ESwordType type)
    {
        switch (type)
        {
            case ESwordType.Adel:
                _adelController?.Fire();
                _adelCooldownTimer = 0f;
                break;
            case ESwordType.Hypo:
                _hypoController?.Fire();
                _hypoCooldownTimer = 0f;
                break;
            case ESwordType.Pixel:
                _pixelController?.Fire();
                _pixelCooldownTimer = 0f;
                break;
        }
    }
}