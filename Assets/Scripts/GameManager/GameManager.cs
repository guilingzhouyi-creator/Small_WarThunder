using System.Collections.Generic;
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

    [Header("系统引用（可选，用于 Inspector 预绑定）")]
    [SerializeField] private SettingManager _settingManager;
    [SerializeField] private AudioManager _audioManager;

    private GameLevelManager _runtimeGameLevel;

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
        DontDestroyOnLoad(gameObject);
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
        BootstrapAudioAndSettings();
        StartValidationIfNeeded();
        HandleGameSceneEntered(SceneManager.GetActiveScene());
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
        HandleGameSceneEntered(scene);
    }

    /// <summary>
    /// GameScene 进入时，总控层协调各执行层。
    /// </summary>
    private void HandleGameSceneEntered(Scene scene)
    {
        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            return;
        }

        // 1. 执行层：玩家生成
        PlayerSpawnSystem.Instance?.SpawnPlayer();

        // 2. 执行层：摄像机绑定
        CameraSystem.Instance?.BindToPlayer();

        // 3. 执行层：音频设置引导
        _settingManager ??= SettingManager.Instance;

        _audioManager ??= AudioManager.Instance;
        _audioManager?.PlayBGM();

        // 4. 总控层：尝试触发关卡开始叙事字幕
        //    _runtimeGameLevel 由 LevelStreamingEngine → GameLevelManager.OnEnable → RegisterRuntimeGameLevel 注册
        //    此时地图可能尚未加载完成，字幕触发在 RegisterRuntimeGameLevel 中处理
        TryPrepareCurrentLevelBootstrap();
    }

    /// <summary>
    /// 引导音频和设置子系统的单例引用。
    /// </summary>
    private void BootstrapAudioAndSettings()
    {
        _settingManager ??= SettingManager.Instance;
        _audioManager ??= AudioManager.Instance;

        _settingManager?.Initialize(_audioManager);
    }

    // ───────────── 关卡生命周期管理 ─────────────

    public void RegisterRuntimeGameLevel(GameLevelManager gameLevel)
    {
        if (gameLevel == null)
        {
            return;
        }

        if (gameLevel.LevelIndex == _levelIndex)
        {
            _runtimeGameLevel = gameLevel;
            TryPrepareCurrentLevelBootstrap();
        }
    }

    public void UnregisterRuntimeGameLevel(GameLevelManager gameLevel)
    {
        if (_runtimeGameLevel == gameLevel)
        {
            _runtimeGameLevel = null;
        }
    }

    private void TryPrepareCurrentLevelBootstrap()
    {
        _runtimeGameLevel?.PrepareLevelStartNarrative();
    }

    public static void ResetStaticData()
    {
        _levelIndex = 1;
    }

    public void LoadNextLevel()
    {
        _levelIndex++;
        SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
    }
}
