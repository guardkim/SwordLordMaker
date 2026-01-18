using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RedDotView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RedDotKey _key = RedDotKey.None;
    [SerializeField] private GameObject _redDotObject;
    [SerializeField] private Image _redDotImage;

    [Header("Animation")]
    [SerializeField] private bool _useAnimation = true;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private float _pulseScale = 1.2f;
    [SerializeField] private float _pulseDuration = 0.5f;
    [SerializeField] private bool _usePulse = false;

    private Tween _currentTween;
    private Tween _pulseTween;

    public RedDotKey Key => _key;

    private void OnEnable()
    {
        if (_key == RedDotKey.None)
        {
            return;
        }

        if (RedDotManager.Instance != null)
        {
            RedDotManager.Instance.Subscribe(_key, HandleRedDotStateChanged);
        }
    }

    private void OnDisable()
    {
        KillTweens();

        if (_key == RedDotKey.None)
        {
            return;
        }

        if (RedDotManager.Instance != null)
        {
            RedDotManager.Instance.Unsubscribe(_key, HandleRedDotStateChanged);
        }
    }

    private void HandleRedDotStateChanged(RedDotKey key, bool isActive)
    {
        if (_redDotObject == null)
        {
            return;
        }

        if (_useAnimation)
        {
            AnimateRedDot(isActive);
        }
        else
        {
            _redDotObject.SetActive(isActive);
        }
    }

    private void AnimateRedDot(bool show)
    {
        KillTweens();

        if (show)
        {
            _redDotObject.SetActive(true);
            _redDotObject.transform.localScale = Vector3.zero;

            _currentTween = _redDotObject.transform
                .DOScale(Vector3.one, _animationDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_usePulse)
                    {
                        StartPulse();
                    }
                });
        }
        else
        {
            StopPulse();

            _currentTween = _redDotObject.transform
                .DOScale(Vector3.zero, _animationDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _redDotObject.SetActive(false);
                    _redDotObject.transform.localScale = Vector3.one;
                });
        }
    }

    private void StartPulse()
    {
        _pulseTween = _redDotObject.transform
            .DOScale(_pulseScale, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopPulse()
    {
        if (_pulseTween != null && _pulseTween.IsActive())
        {
            _pulseTween.Kill();
            _pulseTween = null;
        }
    }

    private void KillTweens()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
            _currentTween = null;
        }
        StopPulse();
    }

    public void SetKey(RedDotKey key)
    {
        if (_key != RedDotKey.None && RedDotManager.Instance != null)
        {
            RedDotManager.Instance.Unsubscribe(_key, HandleRedDotStateChanged);
        }

        _key = key;

        if (_key != RedDotKey.None && RedDotManager.Instance != null && gameObject.activeInHierarchy)
        {
            RedDotManager.Instance.Subscribe(_key, HandleRedDotStateChanged);
        }
    }

    public void ForceRefresh()
    {
        if (_key == RedDotKey.None || RedDotManager.Instance == null)
        {
            return;
        }

        bool isActive = RedDotManager.Instance.IsActive(_key);
        HandleRedDotStateChanged(_key, isActive);
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
