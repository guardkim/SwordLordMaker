using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class FadeManager : DontDestroySingleton<FadeManager>
{
    [SerializeField] private Image fadeImage; 
    [SerializeField] [Range(0.1f, 3.0f)] private float fadeDuration = 1.0f;

    public void FadeIn(Action onComplete = null)
    {
        if (fadeImage == null) return;

        fadeImage.DOKill();

        // Image의 투명도를 0으로 (투명해짐 = 화면 보임)
        fadeImage.DOFade(0f, fadeDuration)
            .OnStart(() => 
            {
                // 페이드 중에는 클릭 막기
                fadeImage.raycastTarget = true; 
            })
            .OnComplete(() => 
            {
                // 다 끝나면 클릭 통과시켜서 게임 할 수 있게 함
                fadeImage.raycastTarget = false; 
                onComplete?.Invoke();
            });
    }

    public void FadeOut(Action onComplete = null)
    {
        if (fadeImage == null) return;

        fadeImage.DOKill();

        // Image의 투명도를 1로 (불투명해짐 = 검은 화면)
        fadeImage.DOFade(1f, fadeDuration)
            .OnStart(() => 
            {
                // 시작하자마자 클릭 막기
                fadeImage.raycastTarget = true; 
            })
            .OnComplete(() => 
            {
                // 어두워진 상태 유지 (클릭 계속 막음)
                onComplete?.Invoke();
            });
    }
}