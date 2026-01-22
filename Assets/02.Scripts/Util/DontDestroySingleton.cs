using UnityEngine;

public class DontDestroySingleton<T> : MonoBehaviour where T : DontDestroySingleton<T>
{
    private static T _instance;
    private bool _isInitialized;

    // 인스턴스 존재 여부만 확인 (새로 생성하지 않음)
    public static bool HasInstance => _instance != null;

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

                // Awake 전에 Instance 접근 시 DontDestroyOnLoad 설정
                if (_instance.transform.parent == null && _instance.gameObject.scene.buildIndex != -1)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }

            // Awake 이전에 접근 시에도 초기화 보장
            _instance.EnsureInitialized();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        UnityEngine.Debug.Log($"[DontDestroySingleton] {typeof(T).Name}.Awake() 시작 - parent: {transform.parent}, _instance: {(_instance == null ? "null" : "exists")}");

        // 부모가 있으면 분리
        if (transform.parent != null)
        {
            UnityEngine.Debug.Log($"[DontDestroySingleton] {typeof(T).Name} 부모에서 분리");
            transform.SetParent(null);
        }

        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            UnityEngine.Debug.Log($"[DontDestroySingleton] {typeof(T).Name} DontDestroyOnLoad 설정 완료");
            EnsureInitialized();
        }
        else if (_instance != this)
        {
            UnityEngine.Debug.LogWarning($"[DontDestroySingleton] {typeof(T).Name} 중복 → 파괴");
            Destroy(gameObject);
        }
        else
        {
            // _instance == this인 경우, DontDestroyOnLoad가 설정되었는지 확인
            if (gameObject.scene.buildIndex != -1)
            {
                DontDestroyOnLoad(gameObject);
                UnityEngine.Debug.Log($"[DontDestroySingleton] {typeof(T).Name} _instance와 동일, DontDestroyOnLoad 추가 설정");
            }
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
