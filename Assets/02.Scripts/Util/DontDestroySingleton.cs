using UnityEngine;

public class DontDestroySingleton<T> : MonoBehaviour where T : DontDestroySingleton<T>
{
    private static T _instance;
    private bool _isInitialized;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    _instance = new GameObject(nameof(T)).AddComponent<T>();
                    // Initialize()는 Awake에서 호출됨
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);

            if (!_isInitialized)
            {
                _isInitialized = true;
                Initialize();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void Initialize()
    {
    }
}
