using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private bool _isInitialized;

    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }

        if (_instance == this && !_isInitialized)
        {
            _isInitialized = true;
            Initialize();
        }
    }

    protected virtual void Start()
    {
        if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void Initialize()
    {
    }
}