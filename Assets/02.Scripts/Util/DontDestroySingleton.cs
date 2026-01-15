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
                    _instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }
            }

            // Awake 이전에 접근 시에도 초기화 보장
            _instance.EnsureInitialized();
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
            EnsureInitialized();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            Initialize();
        }
    }

    protected virtual void Initialize()
    {
    }
}
