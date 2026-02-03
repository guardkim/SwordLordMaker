using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public abstract class PopupBase : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private EPopupType _popupType = EPopupType.None;
    [SerializeField] private EPopupPriority _priority = EPopupPriority.Normal;
    [SerializeField] private bool _closeOnBlockerClick = true;
    [SerializeField] private bool _showBlocker = true;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    private Canvas _canvas;
    private bool _isOpen;
    private Tween _currentTween;

    public EPopupType PopupType => _popupType;
    public EPopupPriority Priority => _priority;
    public bool CloseOnBlockerClick => _closeOnBlockerClick;
    public bool ShowBlocker => _showBlocker;
    public bool IsOpen => _isOpen;
    public Canvas Canvas => _canvas;

    public event Action<PopupBase> OnOpened;
    public event Action<PopupBase> OnClosed;

    protected virtual void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = true;

        RegisterToManager();
    }

    private void RegisterToManager()
    {
        if (_popupType == EPopupType.None)
        {
            return;
        }

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.Register(_popupType, this);
        }
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        gameObject.SetActive(true);

        KillCurrentTween();
        transform.localScale = Vector3.zero;
        _currentTween = transform
            .DOScale(Vector3.one, _animationDuration)
            .SetEase(_openEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                OnOpen();
                OnOpened?.Invoke(this);
            });
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;

        KillCurrentTween();
        _currentTween = transform
            .DOScale(Vector3.zero, _animationDuration)
            .SetEase(_closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                OnClose();
                OnClosed?.Invoke(this);
                gameObject.SetActive(false);
                transform.localScale = Vector3.one;
            });
    }

    private void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
            _currentTween = null;
        }
    }

    protected virtual void OnDestroy()
    {
        KillCurrentTween();
        UnregisterFromManager();
    }

    private void UnregisterFromManager()
    {
        if (_popupType == EPopupType.None)
        {
            return;
        }

        if (PopupManager.HasInstance)
        {
            PopupManager.Instance.Unregister(_popupType);
        }
    }

    public void SetSortingOrder(int order)
    {
        if (_canvas != null)
        {
            _canvas.sortingOrder = order;
        }
    }

    public void RequestClose()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    protected virtual void OnOpen()
    {
    }

    protected virtual void OnClose()
    {
    }
}
