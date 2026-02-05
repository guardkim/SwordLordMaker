using UnityEngine;

public class ServiceInstaller : MonoBehaviour
{
    private static ServiceInstaller _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterGlobalServices();
    }

    private void RegisterGlobalServices()
    {
        // DontDestroy Manager들은 자체 Initialize에서 ServiceLocator에 등록
        // 여기서는 MarkAsReady 호출 타이밍만 관리

        // 모든 DontDestroySingleton이 Awake에서 등록되므로,
        // Start에서 Ready 마킹
    }

    private void Start()
    {
        // DontDestroySingleton들이 모두 Awake에서 등록된 후 Ready 마킹
        ServiceLocator.MarkAsReady();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            ServiceLocator.Clear();
            _instance = null;
        }
    }
}
