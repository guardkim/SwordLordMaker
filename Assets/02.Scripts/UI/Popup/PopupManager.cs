using System;
using System.Collections.Generic;
using UnityEngine;

public class PopupManager : DontDestroySingleton<PopupManager>
{
    [Header("Settings")]
    [SerializeField] private int _baseSortingOrder = 100;
    [SerializeField] private int _sortingOrderStep = 10;
    [SerializeField] private PopupBlocker _blockerPrefab;

    private readonly SortedDictionary<PopupPriority, List<PopupBase>> _popupStacks
        = new SortedDictionary<PopupPriority, List<PopupBase>>(
            Comparer<PopupPriority>.Create((a, b) => b.CompareTo(a)));

    private readonly Dictionary<PopupBase, PopupBlocker> _popupBlockers
        = new Dictionary<PopupBase, PopupBlocker>();

    private readonly Stack<PopupBlocker> _blockerPool = new Stack<PopupBlocker>();

    private readonly Dictionary<PopupType, PopupBase> _registeredPopups
        = new Dictionary<PopupType, PopupBase>();

    private int _totalPopupCount;

    public event Action<PopupBase> OnPopupOpened;
    public event Action<PopupBase> OnPopupClosed;
    public event Action OnAllPopupsClosed;

    public int OpenPopupCount => _totalPopupCount;
    public bool HasOpenPopup => _totalPopupCount > 0;

    protected override void Initialize()
    {
        InitializePriorityStacks();
    }

    private void InitializePriorityStacks()
    {
        foreach (PopupPriority priority in Enum.GetValues(typeof(PopupPriority)))
        {
            _popupStacks[priority] = new List<PopupBase>();
        }
    }

    public void OpenPopup(PopupBase popup)
    {
        if (popup == null)
        {
            Debug.LogWarning("[PopupManager] popup is null.");
            return;
        }

        if (popup.IsOpen)
        {
            Debug.LogWarning($"[PopupManager] {popup.name} is already open.");
            return;
        }

        PopupPriority priority = popup.Priority;
        List<PopupBase> stack = _popupStacks[priority];

        int orderInPriority = stack.Count;
        int sortingOrder = CalculateSortingOrder(priority, orderInPriority);

        stack.Add(popup);
        _totalPopupCount++;

        if (popup.ShowBlocker)
        {
            ShowBlockerForPopup(popup, sortingOrder - 1);
        }

        popup.SetSortingOrder(sortingOrder);
        popup.Open();

        OnPopupOpened?.Invoke(popup);
    }

    public void ClosePopup(PopupBase popup)
    {
        if (popup == null || !popup.IsOpen)
        {
            return;
        }

        PopupPriority priority = popup.Priority;
        List<PopupBase> stack = _popupStacks[priority];

        if (!stack.Contains(popup))
        {
            return;
        }

        stack.Remove(popup);
        _totalPopupCount--;

        HideBlockerForPopup(popup);

        popup.Close();

        OnPopupClosed?.Invoke(popup);

        if (_totalPopupCount == 0)
        {
            OnAllPopupsClosed?.Invoke();
        }
    }

    public void CloseTopPopup()
    {
        PopupBase topPopup = GetTopPopup();
        if (topPopup != null)
        {
            ClosePopup(topPopup);
        }
    }

    public void CloseAllPopups()
    {
        List<PopupBase> popupsToClose = new List<PopupBase>();

        foreach (var kvp in _popupStacks)
        {
            foreach (PopupBase popup in kvp.Value)
            {
                popupsToClose.Add(popup);
            }
        }

        foreach (PopupBase popup in popupsToClose)
        {
            ClosePopup(popup);
        }
    }

    public void ClosePopupsBelowPriority(PopupPriority priority)
    {
        List<PopupBase> popupsToClose = new List<PopupBase>();

        foreach (var kvp in _popupStacks)
        {
            if (kvp.Key < priority)
            {
                foreach (PopupBase popup in kvp.Value)
                {
                    popupsToClose.Add(popup);
                }
            }
        }

        foreach (PopupBase popup in popupsToClose)
        {
            ClosePopup(popup);
        }
    }

    public PopupBase GetTopPopup()
    {
        foreach (var kvp in _popupStacks)
        {
            List<PopupBase> stack = kvp.Value;
            if (stack.Count > 0)
            {
                return stack[stack.Count - 1];
            }
        }
        return null;
    }

    public bool HasPopupWithPriority(PopupPriority priority)
    {
        return _popupStacks.TryGetValue(priority, out List<PopupBase> stack) && stack.Count > 0;
    }

    #region Registration

    public void Register(PopupType type, PopupBase popup)
    {
        if (type == PopupType.None || popup == null)
        {
            return;
        }

        if (_registeredPopups.ContainsKey(type))
        {
            Debug.LogWarning($"[PopupManager] PopupType.{type} is already registered. Overwriting.");
        }

        _registeredPopups[type] = popup;
    }

    public void Unregister(PopupType type)
    {
        if (type == PopupType.None)
        {
            return;
        }

        _registeredPopups.Remove(type);
    }

    #endregion

    #region Enum-based Open/Close

    public PopupBase Open(PopupType type)
    {
        if (!_registeredPopups.TryGetValue(type, out PopupBase popup))
        {
            Debug.LogWarning($"[PopupManager] PopupType.{type} is not registered.");
            return null;
        }

        OpenPopup(popup);
        return popup;
    }

    public T Open<T>(PopupType type) where T : PopupBase
    {
        PopupBase popup = Open(type);
        return popup as T;
    }

    public void Close(PopupType type)
    {
        if (!_registeredPopups.TryGetValue(type, out PopupBase popup))
        {
            return;
        }

        ClosePopup(popup);
    }

    public T Get<T>(PopupType type) where T : PopupBase
    {
        if (_registeredPopups.TryGetValue(type, out PopupBase popup))
        {
            return popup as T;
        }
        return null;
    }

    public bool IsRegistered(PopupType type)
    {
        return _registeredPopups.ContainsKey(type);
    }

    #endregion

    private int CalculateSortingOrder(PopupPriority priority, int orderInPriority)
    {
        int priorityBase = (int)priority * 100;
        return _baseSortingOrder + priorityBase + (orderInPriority * _sortingOrderStep);
    }

    private void ShowBlockerForPopup(PopupBase popup, int sortingOrder)
    {
        PopupBlocker blocker = GetBlockerFromPool();
        blocker.SetSortingOrder(sortingOrder);

        if (popup.CloseOnBlockerClick)
        {
            blocker.SetClickAction(() => ClosePopup(popup));
        }
        else
        {
            blocker.SetClickAction(null);
        }

        blocker.Show();
        _popupBlockers[popup] = blocker;
    }

    private void HideBlockerForPopup(PopupBase popup)
    {
        if (_popupBlockers.TryGetValue(popup, out PopupBlocker blocker))
        {
            blocker.Hide();
            blocker.SetClickAction(null);
            ReturnBlockerToPool(blocker);
            _popupBlockers.Remove(popup);
        }
    }

    private PopupBlocker GetBlockerFromPool()
    {
        if (_blockerPool.Count > 0)
        {
            return _blockerPool.Pop();
        }

        if (_blockerPrefab != null)
        {
            return Instantiate(_blockerPrefab, transform);
        }

        GameObject blockerObj = new GameObject("PopupBlocker");
        blockerObj.transform.SetParent(transform, false);

        Canvas canvas = blockerObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        blockerObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        return blockerObj.AddComponent<PopupBlocker>();
    }

    private void ReturnBlockerToPool(PopupBlocker blocker)
    {
        _blockerPool.Push(blocker);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && HasOpenPopup)
        {
            PopupBase topPopup = GetTopPopup();
            if (topPopup != null && topPopup.CloseOnBlockerClick)
            {
                CloseTopPopup();
            }
        }
    }
}
