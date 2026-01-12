using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MoveToMainScene : MonoBehaviour
{
    private readonly string NextSceneName = "MainScene";
    public void MoveScene()
    {
        SceneManager.LoadScene(NextSceneName);
    }
}
