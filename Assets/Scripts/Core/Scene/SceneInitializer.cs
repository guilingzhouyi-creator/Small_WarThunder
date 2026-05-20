using UnityEngine.SceneManagement;
using NNewUIFramework;

/// <summary>
/// 场景初始化时序中枢。
/// 负责承接当前项目的 GameScene 初始化执行流：玩家生成、摄像机绑定、音频启动。
/// 该类作为纯执行工具，由 GameManager 持有并调用，不依赖场景挂载。
/// </summary>
public class SceneInitializer
{
    public enum InitializationPhase
    {
        None,
        ResetRuntimeState,
        InitializeGameplaySystems,
        InitializeUiSystems,
        InitializeHudSystems,
        Completed,
    }

    public InitializationPhase CurrentPhase { get; private set; }

    private bool _isInitializing;
    private int _initializedSceneHandle = -1;

    /// <summary>
    /// 统一的场景初始化入口。
    /// 当前版本先接入项目里已经存在的游戏执行流：GameScene 的玩家、摄像机、音频三段。
    /// </summary>
    public bool InitializeForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        if (_isInitializing)
        {
            return false;
        }

        if (_initializedSceneHandle == scene.handle)
        {
            return true;
        }

        _isInitializing = true;

        try
        {
            SetPhase(InitializationPhase.ResetRuntimeState);
            ResetSceneRuntimeState(scene);

            if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
            {
                SetPhase(InitializationPhase.Completed);
                _initializedSceneHandle = scene.handle;
                return true;
            }

            SetPhase(InitializationPhase.InitializeGameplaySystems);
            InitializeGameplaySystems(scene);

            SetPhase(InitializationPhase.InitializeUiSystems);
            InitializeUiSystems(scene);

            SetPhase(InitializationPhase.InitializeHudSystems);
            InitializeHudSystems(scene);

            SetPhase(InitializationPhase.Completed);
            _initializedSceneHandle = scene.handle;
            return true;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// 场景级运行时状态复位。
    /// 当前先接入叙事运行时重置，避免旧场景包和字幕播放状态跨场景残留。
    /// </summary>
    protected virtual void ResetSceneRuntimeState(Scene scene)
    {
        MissionNarrativeRuntime.ResetAll();
    }

    /// <summary>
    /// 当前项目的 GameScene Gameplay 执行流：先生成玩家，再绑定摄像机，最后播放 BGM。
    /// 这三个步骤对应当前项目里已经存在且最稳定的场景级初始化顺序。
    /// </summary>
    protected virtual void InitializeGameplaySystems(Scene scene)
    {
        PlayerSpawnSystem.Instance?.SpawnPlayer();

        UnityEngine.GameObject playerTank = PlayerSpawnSystem.Instance?.PlayerTank;
        InitializePlayerRuntimeComponents(playerTank);

        CameraSystem.Instance?.BindToPlayer();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM();
        }
    }

    /// <summary>
    /// UI 相关初始化执行流。
    /// 先让 UI 中枢完成本场景的注册与刷新，再把地图面板绑定到当前玩家上。
    /// </summary>
    protected virtual void InitializeUiSystems(Scene scene)
    {
        if (NewUIManager.instance != null)
        {
            NewUIManager.instance.InitializeForScene(scene);
        }
    }

    /// <summary>
    /// HUD 相关初始化执行流。
    /// 在 UI 中枢完成注册与状态刷新后，再显式初始化 HUD 各面板，避免各控制器依赖 Start/OnEnable/Update 自我兜底。
    /// </summary>
    protected virtual void InitializeHudSystems(Scene scene)
    {
        UnityEngine.Transform playerTransform = PlayerSpawnSystem.Instance?.PlayerTank != null
            ? PlayerSpawnSystem.Instance.PlayerTank.transform
            : null;

        SubtitleOverlayController.Instance?.InitializeForScene(scene);
        MissionPannelUIController.Instance?.InitializeForScene(scene);
        TankStatsUIController.Instance?.InitializeForScene(scene);
        TankAImUIController.Instance?.InitializeForScene(scene);

        if (MapUIController.Instance != null)
        {
            MapUIController.Instance.InitializeForScene(scene, playerTransform);
        }
    }

    private void SetPhase(InitializationPhase phase)
    {
        CurrentPhase = phase;
    }

    private void InitializePlayerRuntimeComponents(UnityEngine.GameObject playerTank)
    {
        if (playerTank == null)
        {
            return;
        }

        playerTank.GetComponentInChildren<Tank>(true)?.EnsureRuntimeStateInitialized();
        playerTank.GetComponentInChildren<TankFireController>(true)?.EnsureRuntimeStateInitialized();
        playerTank.GetComponentInChildren<TankMoveController>(true)?.EnsureRuntimeStateInitialized();
    }
}

/// <summary>
/// 兼容旧项目里 PlayerInitializer 组件名的过渡壳。
/// 该组件已不再承担初始化职责，仅用于避免旧场景上的序列化引用立即失效。
/// </summary>
public sealed class PlayerInitializer : UnityEngine.MonoBehaviour
{
}
