using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameManager : MonoBehaviour
{
    //核心数据储存与处理类，负责管理游戏的核心数据和逻辑，例如坦克状态、关卡信息、游戏进度等
    //可以通过单例模式来实现全局访问，确保游戏中的各个系统都能方便地访问和修改核心数据

    public static GameManager Instance { get; private set; }
    private static int lvevlIndex = 1; // 当前关卡索引，初始值为1，表示第一关
    // private Transform playerSpawnPoint; // 玩家出生点位置，后续会从 GameLevelManager 获取并设置
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private GameObject playerTank;
    [SerializeField] private CameraPosition cameraPosition;
    [SerializeField] private ZoomCameraPosition zoomCameraPosition;
    [SerializeField] private AimCameraPosition aimCameraPosition;
    [SerializeField] private SettingManager settingManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private List<GameLevelManager> gameLevelList;
    private GameLevelManager runtimeGameLevel;

    public GameObject PlayerTank => playerTank; // 公开只读属性，允许其他系统访问玩家坦克实例，但不允许直接修改

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameManager: 已存在一个 GameManager 实例，当前实例将被销毁以保持单例模式。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    private void Start()
    {
        ValidateComponentsInThis();

        ValidateComponentsInExternal();

        BootstrapSystems();

        SpawnPlayerTankIfNeeded();

        HandleGameSceneEntered(SceneManager.GetActiveScene());

        // playerSpawnPoint = ;
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
        BootstrapSystems();

        HandleGameSceneEntered(scene);
    }

    private void HandleGameSceneEntered(Scene scene)
    {
        bool isGameScene = SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene);

        if (!isGameScene)
        {
            return;
        }

        SpawnPlayerTankIfNeeded();

        if (settingManager != null)
        {
            settingManager.ApplyCurrentSettingsToAudio();
        }

        audioManager ??= AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayBGM();
        }

        LevelStreamingEngine.Instance?.RefreshVisibleRegionsNow();
        TryPrepareCurrentLevelBootstrap(scene);
    }



    private void BootstrapSystems()
    {

        if (settingManager == null)
        {
            settingManager = SettingManager.Instance;
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (settingManager != null)
        {
            settingManager.Initialize(audioManager);
        }

    }

    private void SpawnPlayerTankIfNeeded()
    {
        if (playerTank != null)
        {
            BindCameraTargets(playerTank.transform);
            return;
        }

        SpawnPlayerTankValidate();

        playerTank = Instantiate(tankPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        playerTank.name = tankPrefab.name;

        BindCameraTargets(playerTank.transform);
    }

    private void LoadCurrentLevel()
    {
        var gameLevel = GetGameLevel();

        var spawnedGameLevel = Instantiate(gameLevel, Vector3.zero, Quaternion.identity);

    }

    public void RegisterRuntimeGameLevel(GameLevelManager gameLevel)
    {
        if (gameLevel == null)
        {
            return;
        }

        if (gameLevel.LevelIndex == lvevlIndex)
        {
            runtimeGameLevel = gameLevel;
            TryPrepareCurrentLevelBootstrap(gameLevel.gameObject.scene);
        }
    }

    public void UnregisterRuntimeGameLevel(GameLevelManager gameLevel)
    {
        if (runtimeGameLevel == gameLevel)
        {
            runtimeGameLevel = null;
        }
    }


    private GameLevelManager GetGameLevel()
    {
        foreach (var gameLevel in gameLevelList)
        {
            if (gameLevel.GetLevelIndex() == lvevlIndex)
            {
                return gameLevel;
            }
        }

        return null;
    }

    private void TryPrepareCurrentLevelBootstrap(Scene scene)
    {
        GameLevelManager currentLevel = ResolveRuntimeGameLevel(scene);
        currentLevel?.PrepareLevelStartNarrative();
    }

    private GameLevelManager ResolveRuntimeGameLevel(Scene scene)
    {
        if (runtimeGameLevel != null
            && runtimeGameLevel.LevelIndex == lvevlIndex
            && runtimeGameLevel.gameObject.scene == scene)
        {
            return runtimeGameLevel;
        }

        GameLevelManager[] runtimeLevels = FindObjectsByType<GameLevelManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int index = 0; index < runtimeLevels.Length; index++)
        {
            GameLevelManager candidate = runtimeLevels[index];
            if (candidate == null || candidate.LevelIndex != lvevlIndex)
            {
                continue;
            }

            if (candidate.gameObject.scene != scene)
            {
                continue;
            }

            runtimeGameLevel = candidate;
            return runtimeGameLevel;
        }

        return null;
    }

    public static void ResetStaticData()
    {
        lvevlIndex = 1; // 重置关卡索引，回到第一关

    }

    public void LoadNextLevel()
    {
        lvevlIndex++;


        if (GetGameLevel() == null)
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameOverScene);
            return;
        }
        else
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }
    }



}