using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject _nicknameUIPrefab;

    public void LoadMainScene()
    {
        _nicknameUIPrefab.SetActive(true);
    }
}
