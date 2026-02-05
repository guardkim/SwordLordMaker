using UnityEngine;

public class SceneServiceInstaller : MonoBehaviour
{
    [Header("Scene Services (자동 감지 또는 수동 할당)")]
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private EnemySpawner _enemySpawner;

    private void Awake()
    {
        // 자동 감지
        if (_stageManager == null)
            _stageManager = FindFirstObjectByType<StageManager>();

        if (_enemySpawner == null)
            _enemySpawner = FindFirstObjectByType<EnemySpawner>();

        RegisterSceneServices();
    }

    private void RegisterSceneServices()
    {
        if (_stageManager != null)
        {
            ServiceLocator.Register<IStageService>(_stageManager);
        }

        if (_enemySpawner != null)
        {
            ServiceLocator.Register<IEnemySpawner>(_enemySpawner);
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IStageService>();
        ServiceLocator.Unregister<IEnemySpawner>();
    }
}
