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
    [SerializeField] private bool _showMiniMapInTPS = true;
    [SerializeField] private bool _showMiniMapInAim = false;

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
    private readonly UIOverlayStack _overlayStack = new UIOverlayStack();

    [Header("跨 UI 层级（UGUI vs UI Toolkit）处理")]
    [Tooltip("Pause 打开时：把 Map 的 UIDocument.sortingOrder 降到这个值，以确保 Pause Canvas 在其上方。")]
    [SerializeField] private float _pauseMapUIDocumentLowering = -1000f;

    [Tooltip("Pause 打开时：把 Pause 所在 Canvas.sortingOrder 提到这个值。")]
    [SerializeField] private int _pauseCanvasRaise = 1000;

    [Tooltip("Setting 打开时（Pause 期间）：在 Pause Canvas Raise 基础上额外抬高 Setting Canvas。")]
    [SerializeField] private int _settingCanvasRaiseAdditional = 10;

    private bool _hasCachedLayering;
    private float _originMapUIDocumentSortingOrder;
    private int _originPauseCanvasSortingOrder;
    private int _originSettingCanvasSortingOrder;

    private Canvas _pauseCanvas;
    private Canvas _settingCanvas;

    private bool _isLayeringApplied;

    public bool IsPaused => _isPaused;
    public bool IsGameplayControlLocked => _overlayStack.HasAnyOverlay || _isCgPlaying;
    public bool IsAimMode => _isAimMode;
    public bool IsMapShown => _isMapShown;
    public bool IsTabed => _isTabed;
    public bool IsSettingUIVisible => _isSettingUIVisible;
    public bool IsCgPlaying => _isCgPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UIManager: duplicate instance detected, destroying the new one.", this);
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
        {
            SetCursorLocked(false);
        }

        _overlayStack.Clear();
        _isPaused = false;
        _isTabed = false;
        _isMapShown = false;
        _isSettingUIVisible = false;
        _isAimMode = false;

        // 避免跨场景残留排序层级
        RestoreCrossLayering();
        _hasCachedLayering = false;
        _isLayeringApplied = false;

        RefreshCursorLockState();
        RefreshUIState();
    }

    private void Update()
    {
        if (IsGameplayControlLocked)
        {
            return;
        }

        if (MIddleInputingController.Instance != null && MIddleInputingController.Instance.IsAimingPressed())
        {
            ToggleAimMode();
        }
    }

    private void BindInputEvents()
    {
        if (_isInputBound || MIddleInputingController.Instance == null)
        {
            return;
        }

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
        MIddleInputingController.Instance.OnTabInputProcessed -= HandleTabInput;
        MIddleInputingController.Instance.OnMapShowInputProcessed -= HandleMapShowInput;
        _isInputBound = false;
    }

    private void BindSettingEvents()
    {
        if (_isSettingBound || settingManager == null)
        {
            return;
        }

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
        if (_isSettingUIVisible)
        {
            CloseOverlay(UIOverlayId.Setting);
            return;
        }

        ToggleOverlay(UIOverlayId.Pause);
    }

    private void HandleTabInput(object sender, EventArgs e)
    {
        if (_isPaused || _isSettingUIVisible)
        {
            return;
        }

        if (_isMapShown)
        {
            SubtitleOverlayController.Instance?.ToggleManualVisibility();
            return;
        }

        ToggleOverlay(UIOverlayId.Tab);
    }

    private void HandleMapShowInput(object sender, EventArgs e)
    {
        if (_isPaused || _isSettingUIVisible)
        {
            return;
        }

        ToggleOverlay(UIOverlayId.Map);
    }

    public void TogglePause() => ToggleOverlay(UIOverlayId.Pause);
    public void ToggleTab() => ToggleOverlay(UIOverlayId.Tab);
    public void ToggleMap() => ToggleOverlay(UIOverlayId.Map);

    public void ToggleAimMode()
    {
        SetAimMode(!_isAimMode);
    }

    public void SetAimMode(bool isAimMode)
    {
        if (_isAimMode == isAimMode)
        {
            return;
        }

        _isAimMode = isAimMode;
        RefreshUIState();
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            OpenOverlay(UIOverlayId.Pause);
        }
        else
        {
            CloseOverlay(UIOverlayId.Pause);
        }
    }

    public void SetMapShown(bool shown)
    {
        if (shown)
        {
            OpenOverlay(UIOverlayId.Map);
        }
        else
        {
            CloseOverlay(UIOverlayId.Map);
        }
    }

    public void SetTabed(bool tabed)
    {
        if (tabed)
        {
            OpenOverlay(UIOverlayId.Tab);
        }
        else
        {
            CloseOverlay(UIOverlayId.Tab);
        }
    }

    public void SetCgPlaying(bool playing)
    {
        if (_isCgPlaying == playing)
        {
            return;
        }

        _isCgPlaying = playing;

        if (playing)
        {
            _isAimMode = false;
            _isSettingUIVisible = false;
            _overlayStack.Close(UIOverlayId.Setting);
            _overlayStack.Close(UIOverlayId.Pause);
            _isPaused = false;
        }

        RefreshCursorLockState();
        RefreshUIState();
    }

    public void ShowSettingsUI() => OpenOverlay(UIOverlayId.Setting);
    public void ShowPauseUI() => OpenOverlay(UIOverlayId.Pause);

    private void HandleSettingsApplied(SettingManager.AudioSettingState audioSettingState)
    {
        OpenOverlay(UIOverlayId.Pause);
    }

    public void ToggleOverlay(UIOverlayId overlay)
    {
        if (overlay == UIOverlayId.None)
        {
            return;
        }

        if (_overlayStack.Contains(overlay))
        {
            CloseOverlay(overlay);
        }
        else
        {
            OpenOverlay(overlay);
        }
    }

    public void OpenOverlay(UIOverlayId overlay)
    {
        switch (overlay)
        {
            case UIOverlayId.Map:
                _isMapShown = true;
                _overlayStack.Open(UIOverlayId.Map);
                break;

            case UIOverlayId.Tab:
                _isTabed = true;
                _overlayStack.Open(UIOverlayId.Tab);
                break;

            case UIOverlayId.Pause:
                if (!_isPaused)
                {
                    _isPaused = true;
                    Time.timeScale = 0f;
                    OnGamePaused?.Invoke(this, EventArgs.Empty);
                }

                CacheCrossLayeringIfNeeded();
                ApplyCrossLayeringForPause();
                _overlayStack.Open(UIOverlayId.Pause);
                break;

            case UIOverlayId.Setting:
                if (!_isPaused)
                {
                    OpenOverlay(UIOverlayId.Pause);
                }

                _isSettingUIVisible = true;
                _overlayStack.Open(UIOverlayId.Setting);

                CacheCrossLayeringIfNeeded();
                if (_isPaused)
                {
                    ApplyCrossLayeringForSetting();
                }
                break;
        }

        RefreshCursorLockState();
        RefreshUIState();
    }

    public void CloseOverlay(UIOverlayId overlay)
    {
        switch (overlay)
        {
            case UIOverlayId.Map:
                _isMapShown = false;
                _overlayStack.Close(UIOverlayId.Map);
                break;

            case UIOverlayId.Tab:
                _isTabed = false;
                _overlayStack.Close(UIOverlayId.Tab);
                break;

            case UIOverlayId.Setting:
                _isSettingUIVisible = false;
                _overlayStack.Close(UIOverlayId.Setting);

                // Pause 仍在时：只恢复 Setting Canvas 的排序，继续保持 Pause 覆盖 Map
                if (_isPaused)
                {
                    RestoreSettingCanvasSortingOrder();
                }
                break;

            case UIOverlayId.Pause:
                _isSettingUIVisible = false;
                _overlayStack.Close(UIOverlayId.Setting);
                _overlayStack.Close(UIOverlayId.Pause);

                if (_isPaused)
                {
                    _isPaused = false;
                    Time.timeScale = 1f;
                    OnGameUnPaused?.Invoke(this, EventArgs.Empty);
                }

                // Pause 退出：恢复 Map / Pause / Setting 的原始层级
                RestoreCrossLayering();
                break;
        }

        RefreshCursorLockState();
        RefreshUIState();
    }

    private void CacheCrossLayeringIfNeeded()
    {
        if (_hasCachedLayering)
        {
            return;
        }

        if (mapUIController == null || pauseUIController == null || settingManager == null)
        {
            return;
        }

        _pauseCanvas = pauseUIController.GetComponentInParent<Canvas>(true);
        _settingCanvas = settingManager.GetComponentInParent<Canvas>(true);

        _originMapUIDocumentSortingOrder = mapUIController.GetUIDocumentSortingOrder();
        _originPauseCanvasSortingOrder = _pauseCanvas != null ? _pauseCanvas.sortingOrder : 0;
        _originSettingCanvasSortingOrder = _settingCanvas != null ? _settingCanvas.sortingOrder : 0;

        _hasCachedLayering = true;
    }

    private void ApplyCrossLayeringForPause()
    {
        if (!_hasCachedLayering)
        {
            return;
        }

        // Map 下调
        mapUIController?.SetUIDocumentSortingOrder(_pauseMapUIDocumentLowering);

        // Pause 上调
        if (_pauseCanvas != null)
        {
            _pauseCanvas.sortingOrder = _originPauseCanvasSortingOrder + _pauseCanvasRaise;
        }

        // 如果 Setting 之前已抬过，至少保证它不覆盖 Pause（一般 Setting 会在 Pause 内显示）
        if (_settingCanvas != null && _isSettingUIVisible)
        {
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder + _pauseCanvasRaise + _settingCanvasRaiseAdditional;
        }

        _isLayeringApplied = true;
    }

    private void ApplyCrossLayeringForSetting()
    {
        if (!_hasCachedLayering)
        {
            return;
        }

        if (!_isPaused)
        {
            return;
        }

        if (_settingCanvas != null)
        {
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder + _pauseCanvasRaise + _settingCanvasRaiseAdditional;
        }

        _isLayeringApplied = true;
    }

    private void RestoreSettingCanvasSortingOrder()
    {
        if (!_hasCachedLayering)
        {
            return;
        }

        if (_settingCanvas != null)
        {
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder;
        }
    }

    private void RestoreCrossLayering()
    {
        if (!_hasCachedLayering)
        {
            return;
        }

        // Map 恢复
        mapUIController?.SetUIDocumentSortingOrder(_originMapUIDocumentSortingOrder);

        // Canvas 恢复
        if (_pauseCanvas != null)
        {
            _pauseCanvas.sortingOrder = _originPauseCanvasSortingOrder;
        }

        if (_settingCanvas != null)
        {
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder;
        }

        _isLayeringApplied = false;
    }

    private void RefreshUIState()
    {
        bool isGameplayScene = IsGameplayScene();

        if (mapUIController != null)
        {
            bool mapPanelActive = isGameplayScene;
            if (mapUIController.gameObject.activeSelf != mapPanelActive)
            {
                mapUIController.gameObject.SetActive(mapPanelActive);
            }

            bool shouldShowFullMap = mapPanelActive && _isMapShown;
            if (shouldShowFullMap)
            {
                mapUIController.OpenFullMap();
            }
            else
            {
                mapUIController.CloseFullMap();

                bool miniMapVisible = mapPanelActive &&
                                      !_isMapShown &&
                                      !_isPaused &&
                                      !_isSettingUIVisible &&
                                      _overlayStack.Top == UIOverlayId.None;
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
        }

        if (missionPannelUIController != null)
        {
            bool shouldActivateMissionPanel = isGameplayScene && _isTabed && _overlayStack.Top == UIOverlayId.Tab;
            missionPannelUIController.gameObject.SetActive(shouldActivateMissionPanel);
        }

        SubtitleOverlayController.Instance?.ApplyVisibility();

        if (pauseUIController != null)
        {
            bool shouldShowPauseUI = isGameplayScene && _isPaused && !_isSettingUIVisible;
            pauseUIController.gameObject.SetActive(shouldShowPauseUI);
            if (shouldShowPauseUI)
            {
                pauseUIController.transform.SetAsLastSibling();
            }
        }

        if (settingManager != null)
        {
            bool shouldShowSettingUI = isGameplayScene && _isPaused && _isSettingUIVisible;
            settingManager.gameObject.SetActive(shouldShowSettingUI);
            if (shouldShowSettingUI)
            {
                settingManager.transform.SetAsLastSibling();
            }
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

    private void RefreshCursorLockState()
    {
        bool shouldUnlockCursor = _overlayStack.HasAnyOverlay || _isCgPlaying;
        SetCursorLocked(!shouldUnlockCursor);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private bool IsGameplayScene()
    {
        return SceneLoader.IsScene(SceneManager.GetActiveScene(), SceneLoader.Scene.GameScene);
    }
}
