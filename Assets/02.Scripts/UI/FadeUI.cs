using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeUI : MonoBehaviour
{
    private Image _image;

    public Image Image
    {
        get
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
            return _image;
        }
    }

    private void Start()
    {
        // FadeManager에 자동 등록
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.BindFadeUI(this);
        }
    }
}
