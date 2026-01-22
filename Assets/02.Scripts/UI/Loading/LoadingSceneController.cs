using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Image를 쓰기 위해 필요
using UnityEngine.SceneManagement;
using DG.Tweening; // DoTween
using MoreMountains.Feedbacks; // Feel

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image _loadingBarImage;
    
    [Header("Feel Feedback")]
    [SerializeField] private MMF_Player _completeFeedback;

    public static string NextSceneName = "MainScene"; 

    private void Start()
    {
        // 시작할 때 0으로 초기화
        _loadingBarImage.fillAmount = 0f;
        StartCoroutine(LoadSceneProcess());
    }

    IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(NextSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        
        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            // 0.9 미만일 때: 가짜 로딩 연출 (타이머 기반으로 천천히 채움)
            if (op.progress < 0.9f)
            {
                // 바로 대입하거나 Lerp 사용. 
                // fillAmount는 0~1 사이 값입니다.
                _loadingBarImage.fillAmount = Mathf.Lerp(_loadingBarImage.fillAmount, op.progress, timer);
                
                // 만약 게이지가 너무 빨리 차는 게 싫다면 timer 속도를 조절하세요.
            }
            else
            {
                // 로딩 준비 완료 (0.9 도달)
                // DoTween의 DOFillAmount를 사용하여 남은 구간을 부드럽게 채움
                _loadingBarImage.DOFillAmount(1f, 0.5f).OnComplete(() =>
                {
                    // Feel 재생 (임팩트!)
                    if (_completeFeedback != null)
                    {
                        _completeFeedback.PlayFeedbacks();
                    }

                    StartCoroutine(ActivateScene(op));
                });

                yield break;
            }
        }
    }

    IEnumerator ActivateScene(AsyncOperation op)
    {
        // Feel 효과 감상 시간 (1초)
        yield return new WaitForSeconds(1.0f);
        op.allowSceneActivation = true;
    }
}