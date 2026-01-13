using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private bool _isInitialized;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(T)).AddComponent<T>();
                // Initialize()는 Awake에서 호출됨
            }
            return _instance;
        }
    }

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