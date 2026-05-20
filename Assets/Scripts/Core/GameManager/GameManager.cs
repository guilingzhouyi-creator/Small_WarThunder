using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡总控（总控层）。
/// 负责关卡生命周期协调：场景加载时调度各执行层系统（玩家生成、摄像机绑定、音频/设置引导、字幕触发）。
/// 不直接执行具体逻辑，只做调度和协调。
/// </summary>
public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private static int _levelIndex = 1;
    private readonly SceneInitializer _sceneInitializer = new SceneInitializer();

    [Header("系统引用（可选，用于 Inspector 预绑定）")]
    [SerializeField] private SettingManager _settingManager;
    [SerializeField] private AudioManager _audioManager;

    /// <summary>
    /// 提供统一的玩家坦克查询入口，委托给执行层。
    /// 供 LevelStreamingEngine 等外部系统使用。
    /// </summary>
    public GameObject PlayerTank => PlayerSpawnSystem.Instance?.PlayerTank;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] 已存在一个 GameManager 实例，当前实例将被销毁以保持单例模式。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        BootstrapAudioAndSettings();
        StartValidationIfNeeded();
        InitializeSceneFlow(scene);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        BootstrapAudioAndSettings();
        OnSceneLoadedValidationIfNeeded(scene);
        InitializeSceneFlow(scene);
    }

    /// <summary>
    /// 统一的场景进入调度入口。
    /// GameManager 持有首轮场景级初始化调用权，SceneInitializer 负责执行初始化步骤。
    /// </summary>
    private void InitializeSceneFlow(Scene scene)
    {
        MissionNarrativeRuntime.ResetAll();
        _sceneInitializer.InitializeForScene(scene);
    }

    /// <summary>
    /// 引导音频和设置子系统的单例引用。
    /// 新版架构中 AudioManager 自行订阅 SettingManager 事件，无需手动初始化。
    /// </summary>
    private void BootstrapAudioAndSettings()
    {
        _settingManager ??= SettingManager.Instance;
        _audioManager ??= AudioManager.Instance;
    }

    public static void ResetStaticData()
    {
        _levelIndex = 1;
        MissionNarrativeRuntime.ResetAll();
    }

    public void LoadNextLevel()
    {
        _levelIndex++;
        SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
    }
}
