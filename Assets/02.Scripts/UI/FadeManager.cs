using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System;

public class FadeManager : DontDestroySingleton<FadeManager>
{
    [SerializeField] [Range(0.1f, 3.0f)] private float _fadeDuration = 1.0f;

    private Image _fadeImage;

    protected override void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 시 FadeUI를 찾아서 바인딩
        FindAndBindFadeUI();
    }

    private void FindAndBindFadeUI()
    {
        FadeUI fadeUI = FindFirstObjectByType<FadeUI>();
        if (fadeUI != null)
        {
            BindFadeUI(fadeUI);
        }
    }

    public void BindFadeUI(FadeUI fadeUI)
    {
        if (fadeUI == null) return;

        _fadeImage = fadeUI.Image;
        Debug.Log($"[FadeManager] FadeUI 바인딩 완료: {fadeUI.gameObject.name}");
    }

    public void FadeIn(Action onComplete = null)
    {
        if (_fadeImage == null)
        {
            Debug.LogWarning("[FadeManager] FadeImage가 없습니다.");
            onComplete?.Invoke();
            return;
        }

        _fadeImage.DOKill();

        _fadeImage.DOFade(0f, _fadeDuration)
            .OnStart(() =>
            {
                _fadeImage.raycastTarget = true;
            })
            .OnComplete(() =>
            {
                _fadeImage.raycastTarget = false;
                onComplete?.Invoke();
            });
    }

    public void FadeOut(Action onComplete = null)
    {
        if (_fadeImage == null)
        {
            Debug.LogWarning("[FadeManager] FadeImage가 없습니다.");
            onComplete?.Invoke();
            return;
        }

        _fadeImage.DOKill();

        _fadeImage.DOFade(1f, _fadeDuration)
            .OnStart(() =>
            {
                _fadeImage.raycastTarget = true;
            })
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }
}
