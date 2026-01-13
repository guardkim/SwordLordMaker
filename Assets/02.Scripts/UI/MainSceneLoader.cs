using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneLoader : MonoBehaviour
{
    private const string MAIN_SCENE_NAME = "MainScene";

    public void LoadMainScene()
    {
        SceneManager.LoadScene(MAIN_SCENE_NAME);
    }
}
