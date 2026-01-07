using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(T)).AddComponent<T>();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (_instance != null) _instance.Initialize();
        }
    }

    protected virtual void Start()
    {
        if (_instance != this)
            Destroy(gameObject);
    }

    protected virtual void Initialize()
    {

    }
}