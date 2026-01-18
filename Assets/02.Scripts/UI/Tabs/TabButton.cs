using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TabButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TabType _tabType;

    public TabType TabType => _tabType;
    public event Action<TabType> OnTabClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnTabClicked?.Invoke(_tabType);
    }

    public void SetClickCallback(Action<TabType> callback)
    {
        OnTabClicked = callback;
    }
}

public enum TabType
{
    Main,
    Hero,
    Sword
}
