using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class PopupBlocker : MonoBehaviour
{
    [SerializeField] private Image _blockerImage;
    [SerializeField] private Color _dimmedColor = new Color(0f, 0f, 0f, 0.5f);

    private Canvas _canvas;
    private Button _blockerButton;

    public Canvas Canvas => _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = true;

        SetupBlockerButton();
    }

    private void SetupBlockerButton()
    {
        if (_blockerImage == null)
        {
            CreateBlockerImage();
        }

        _blockerButton = _blockerImage.GetComponent<Button>();
        if (_blockerButton == null)
        {
            _blockerButton = _blockerImage.gameObject.AddComponent<Button>();
        }

        _blockerButton.transition = Selectable.Transition.None;
    }

    private void CreateBlockerImage()
    {
        GameObject imageObj = new GameObject("BlockerImage");
        imageObj.transform.SetParent(transform, false);

        _blockerImage = imageObj.AddComponent<Image>();
        _blockerImage.color = _dimmedColor;
        _blockerImage.raycastTarget = true;

        RectTransform rect = _blockerImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void SetSortingOrder(int order)
    {
        _canvas.sortingOrder = order;
    }

    public void SetClickAction(UnityAction action)
    {
        _blockerButton.onClick.RemoveAllListeners();
        if (action != null)
        {
            _blockerButton.onClick.AddListener(action);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
