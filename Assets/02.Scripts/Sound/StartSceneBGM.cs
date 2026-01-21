using UnityEngine;

public class StartSceneBGM : MonoBehaviour
{
    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(BgmId.Title);
        }
    }
}
