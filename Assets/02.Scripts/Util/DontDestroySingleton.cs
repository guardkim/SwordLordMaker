using UnityEngine;

public class DontDestroySingleton<T> : MonoBehaviour where T : DontDestroySingleton<T>
{
    private static T _instance;

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
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (this.transform.parent != null)
        {
            this.transform.SetParent(null);
        }

        if (_instance == null || _instance == this)
        {
            _instance = this as T;
            DontDestroyOnLoad(transform.gameObject);
            Initialize();
        }
        else
        {
            if (this != _instance)
            {
                Destroy(this.gameObject);
            }
        }
    }

    protected virtual void Initialize()
    {

    }
}
