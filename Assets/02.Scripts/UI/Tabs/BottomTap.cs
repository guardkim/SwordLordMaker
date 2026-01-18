using UnityEngine;

public class BottomTap : MonoBehaviour
{
    [Header("Tab Panels (MainTab is always in background)")]
    [SerializeField] private GameObject _heroTab;
    [SerializeField] private GameObject _swordTab;

    [Header("Tab Buttons")]
    [SerializeField] private TabButton[] _tabButtons;

    private TabType _currentTab = TabType.Main;

    private void Start()
    {
        foreach (var tabButton in _tabButtons)
        {
            tabButton.SetClickCallback(OnTabClicked);
        }

        SwitchTab(TabType.Main);
    }

    private void OnTabClicked(TabType tabType)
    {
        if (_currentTab == tabType)
        {
            return;
        }

        SwitchTab(tabType);
    }

    public void SwitchTab(TabType tabType)
    {
        _currentTab = tabType;

        _heroTab.SetActive(tabType == TabType.Hero);
        _swordTab.SetActive(tabType == TabType.Sword);
    }
}
