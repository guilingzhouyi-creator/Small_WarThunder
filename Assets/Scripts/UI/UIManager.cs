using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 引用")]
    [SerializeField] private PauseUIController pauseUIController;
    [SerializeField] private MissionPannelUIController missionPannelUIController;
    [SerializeField] private SettingManager settingManager;
    [SerializeField] private TankStatsUIController tankStatsUIController;
    [SerializeField] private TankAImUIController sightUIPanel;
    [SerializeField] private MapUIController mapUIController;

    [Header("小地图状态")]
    [SerializeField] private bool _showMiniMapInTPS = true;   // 第三人称时显示小地图
    [SerializeField] private bool _showMiniMapInAim = false;  // 瞄准模式时隐藏小地图

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnPaused;

    private bool _isInputBound;
    private bool _isSettingBound;
    private bool _isPaused;
    private bool _isTabed;
    private bool _isMapShown;
    private bool _isSettingUIVisible;
    private bool _isSightUIVisible = true;
    private bool _isAimMode;
    private bool _isCgPlaying;

    public bool IsPaused => _isPaused;
    public bool IsGameplayControlLocked => _isPaused || _isTabed || _isMapShown || _isSettingUIVisible || _isCgPlaying;
    public bool IsAimMode => _isAimMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UIManager: 已存在一个 UIManager 实例，当前实例将被销毁以保持单例模式。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (pauseUIController == null)
            pauseUIController = GetComponentInChildren<PauseUIController>(true);
        if (settingManager == null)
            settingManager = GetComponentInChildren<SettingManager>(true);
        if (tankStatsUIController == null)
            tankStatsUIController = GetComponentInChildren<TankStatsUIController>(true);
        if (missionPannelUIController == null)
            missionPannelUIController = GetComponentInChildren<MissionPannelUIController>(true);
        if (mapUIController == null)
            mapUIController = GetComponentInChildren<MapUIController>(true);

        BindSettingEvents();
    }

    private void Start()
    {
        BindInputEvents();
        BindSettingEvents();
        RefreshUIState();
    }

    private void OnEnable()
    {
        BindInputEvents();
        BindSettingEvents();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnbindInputEvents();
        UnbindSettingEvents();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
            SetCursorLocked(false);

        RefreshUIState();
    }

    private void Update()
    {
        if (IsGameplayControlLocked)
            return;

        if (MIddleInputingController.Instance != null && MIddleInputingController.Instance.IsAimingPressed())
            ToggleAimMode();
    }

    private void BindInputEvents()
    {
        if (_isInputBound || MIddleInputingController.Instance == null)
            return;

        MIddleInputingController.Instance.OnPauseInputProcessed += HandlePauseInput;
        MIddleInputingController.Instance.OnTabInputProcessed += HandleTabInput;
        MIddleInputingController.Instance.OnMapShowInputProcessed += HandleMapShowInput;

        _isInputBound = true;
    }

    private void UnbindInputEvents()
    {
        if (!_isInputBound || MIddleInputingController.Instance == null)
        {
            _isInputBound = false;
            return;
        }

        MIddleInputingController.Instance.OnPauseInputProcessed -= HandlePauseInput;
        MIddleInputingController.Instance.OnPauseInputProcessed += HandlePauseInput;

        MIddleInputingController.Instance.OnTabInputProcessed -= HandleTabInput;
        MIddleInputingController.Instance.OnTabInputProcessed += HandleTabInput;

        MIddleInputingController.Instance.OnMapShowInputProcessed -= HandleMapShowInput;
        MIddleInputingController.Instance.OnMapShowInputProcessed += HandleMapShowInput;

        _isInputBound = false;
    }

    private void BindSettingEvents()
    {
        if (_isSettingBound || settingManager == null)
            return;

        settingManager.SettingsApplied += HandleSettingsApplied;
        _isSettingBound = true;
    }

    private void UnbindSettingEvents()
    {
        if (!_isSettingBound || settingManager == null)
        {
            _isSettingBound = false;
            return;
        }

        settingManager.SettingsApplied -= HandleSettingsApplied;
        _isSettingBound = false;
    }

    private void HandlePauseInput(object sender, EventArgs e)
    {
        if (_isTabed)
        {
            SetPaused(true);
            return;
        }

        if (_isSettingUIVisible)
        {
            ShowPauseUI();
            return;
        }

        TogglePause();
    }

    private void HandleTabInput(object sender, EventArgs e)
    {
        if (_isPaused || _isSettingUIVisible || _isMapShown)
            return;

        if (_isAimMode)
            SetAimMode(false);

        ToggleTab();
    }

    private void HandleMapShowInput(object sender, EventArgs e)
    {
        if (_isPaused || _isSettingUIVisible || _isTabed)
            return;

        if (_isAimMode)
            SetAimMode(false);

        ToggleMap();
    }

    public void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public void ToggleTab()
    {
        SetTabed(!_isTabed);
    }

    public void ToggleMap()
    {
        SetMapShown(!_isMapShown);
    }

    public void ToggleAimMode()
    {
        SetAimMode(!_isAimMode);
    }

    public void SetAimMode(bool isAimMode)
    {
        if (_isAimMode == isAimMode)
            return;

        _isAimMode = isAimMode;
        RefreshUIState();
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetPaused(bool paused)
    {
        if (_isPaused == paused)
            return;

        _isPaused = paused;
        _isTabed = false;

        if (!paused)
            _isSettingUIVisible = false;

        Time.timeScale = paused ? 0f : 1f;
        SetCursorLocked(!paused);
        RefreshUIState();

        if (paused)
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        else
            OnGameUnPaused?.Invoke(this, EventArgs.Empty);
    }

    public void SetMapShown(bool shown)
    {
        if (_isMapShown == shown)
            return;

        _isMapShown = shown;

        if (shown)
        {
            _isSettingUIVisible = false;

            if (mapUIController != null)
            {
                mapUIController.gameObject.SetActive(true);
                mapUIController.OpenFullMap();
            }
        }
        else
        {
            if (mapUIController != null)
                mapUIController.CloseFullMap();
        }

        SetCursorLocked(!shown);
        RefreshUIState();
    }

    public void SetTabed(bool tabed)
    {
        if (_isTabed == tabed)
            return;

        _isTabed = tabed;

        if (tabed)
            _isSettingUIVisible = false;

        SetCursorLocked(!tabed);
        RefreshUIState();
    }

    public void SetCgPlaying(bool playing)
    {
        if (_isCgPlaying == playing)
            return;

        _isCgPlaying = playing;

        if (playing)
        {
            _isAimMode = false;
            _isTabed = false;
            _isSettingUIVisible = false;
        }

        SetCursorLocked(!playing);
        RefreshUIState();
    }

    public void ShowSettingsUI()
    {
        if (!_isPaused)
            SetPaused(true);

        _isSettingUIVisible = true;

        if (settingManager != null)
        {
            settingManager.gameObject.SetActive(true);
            settingManager.transform.SetAsLastSibling();
        }

        RefreshUIState();
    }

    public void ShowPauseUI()
    {
        if (!_isPaused)
        {
            SetPaused(true);
            return;
        }

        _isSettingUIVisible = false;
        RefreshUIState();
    }

    private void HandleSettingsApplied(SettingManager.AudioSettingState audioSettingState)
    {
        ShowPauseUI();
    }

    private void RefreshUIState()
    {
        bool isGameplayScene = IsGameplayScene();

        if (pauseUIController != null)
        {
            bool shouldShowPauseUI = isGameplayScene && _isPaused && !_isSettingUIVisible;
            pauseUIController.gameObject.SetActive(shouldShowPauseUI);
            if (shouldShowPauseUI)
                pauseUIController.transform.SetAsLastSibling();
        }

        if (missionPannelUIController != null)
        {
            bool shouldShowMissionPanel = isGameplayScene && _isTabed && !_isSettingUIVisible && !_isMapShown;
            missionPannelUIController.gameObject.SetActive(shouldShowMissionPanel);
            if (shouldShowMissionPanel)
                missionPannelUIController.transform.SetAsLastSibling();
        }

        if (mapUIController != null)
        {
            bool mapPanelActive = isGameplayScene;
            if (mapUIController.gameObject.activeSelf != mapPanelActive)
                mapUIController.gameObject.SetActive(mapPanelActive);

            bool miniMapVisible = mapPanelActive && !_isMapShown && !_isSettingUIVisible && !_isPaused;
            if (miniMapVisible)
            {
                bool show = _isAimMode ? _showMiniMapInAim : _showMiniMapInTPS;
                mapUIController.SetMiniMapVisible(show);
            }
            else
            {
                mapUIController.SetMiniMapVisible(false);
            }
        }

        if (settingManager != null)
        {
            bool shouldShowSettingUI = isGameplayScene && _isPaused && _isSettingUIVisible;
            settingManager.gameObject.SetActive(shouldShowSettingUI);
            if (shouldShowSettingUI)
                settingManager.transform.SetAsLastSibling();
        }

        if (tankStatsUIController != null)
        {
            bool shouldShowTankStatsUI = isGameplayScene && !IsGameplayControlLocked && !_isAimMode;
            tankStatsUIController.gameObject.SetActive(shouldShowTankStatsUI);
        }

        if (sightUIPanel != null)
        {
            bool shouldShowSightUI = isGameplayScene && !IsGameplayControlLocked && _isSightUIVisible;
            sightUIPanel.gameObject.SetActive(shouldShowSightUI);
        }
    }

    private bool IsGameplayScene()
    {
        return SceneLoader.IsScene(SceneManager.GetActiveScene(), SceneLoader.Scene.GameScene);
    }
}
