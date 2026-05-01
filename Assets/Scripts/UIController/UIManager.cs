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


    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnPaused;

    private bool _isInputBound;
    private bool _isSettingBound;
    private bool _isPaused;
    private bool _isTabed;
    private bool _isSettingUIVisible;
    private bool _isSightUIVisible = true;   // 默认情况下，瞄准UI是可见的
    private bool _isAimMode;

    public bool IsPaused => _isPaused;
    public bool IsGameplayControlLocked => _isPaused || _isTabed || _isSettingUIVisible;
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

        if (pauseUIController == null) // 如果在 Inspector 中没有手动设置 pauseUIController，则尝试在子对象中查找一个 PauseUIController 组件并赋值给 pauseUIController 变量。这种做法可以提供一定的容错能力，确保即使开发者忘记在 Inspector 中设置引用，UIManager 仍然能够正常工作。
        {
            pauseUIController = GetComponentInChildren<PauseUIController>(true);
        }

        if (settingManager == null)
        {
            settingManager = GetComponentInChildren<SettingManager>(true);
        }

        if (tankStatsUIController == null)
        {
            tankStatsUIController = GetComponentInChildren<TankStatsUIController>(true);
        }

        if (missionPannelUIController == null)
        {
            missionPannelUIController = GetComponentInChildren<MissionPannelUIController>(true);
        }


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

    // 这里的输入绑定和解绑逻辑确保了当 UIManager 被启用或禁用时，能够正确地响应或停止响应输入事件，避免了潜在的输入处理问题，例如在 UIManager 被禁用后仍然响应输入导致的错误行为。
    private void BindInputEvents()
    {
        if (_isInputBound || MIddleInputingController.Instance == null)
        {
            return;
        }

        MIddleInputingController.Instance.OnPauseInputProcessed += HandlePauseInput;

        MIddleInputingController.Instance.OnTabInputProcessed += HandleTabInput;
        Debug.Log("UIManager: MissionPannelUIController 已成功绑定输入事件。");

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
        MIddleInputingController.Instance.OnPauseInputProcessed += HandlePauseInput;// 解绑事件后立即重新绑定事件，确保在 UIManager 被禁用后再次启用时能够正确响应输入事件。

        MIddleInputingController.Instance.OnTabInputProcessed -= HandleTabInput;
        MIddleInputingController.Instance.OnTabInputProcessed += HandleTabInput;// 解绑事件后立即重新绑定事件，确保在 UIManager 被禁用后再次启用时能够正确响应输入事件。

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

    // 这个方法用于处理暂停输入事件，当玩家按下暂停键时，UIManager 会调用 TogglePause 方法来切换游戏的暂停状态。
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

    // 这个方法用于处理 Tab 输入事件，当玩家按下 Tab 键时，UIManager 会调用 ToggleTab 方法来切换游戏的 Tab 状态。
    private void HandleTabInput(object sender, EventArgs e)
    {
        if (_isPaused || _isSettingUIVisible)
        {
            return;
        }

        if (_isAimMode)
        {
            SetAimMode(false);
        }

        ToggleTab();
    }

    public void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public void ToggleTab()
    {
        SetTabed(!_isTabed);
    }

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
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetPaused(bool paused)
    {
        if (_isPaused == paused)
        {
            return;
        }

        _isPaused = paused;
        _isTabed = false;

        if (!paused)
        {
            _isSettingUIVisible = false;
        }

        Time.timeScale = paused ? 0f : 1f;

        SetCursorLocked(!paused);   // 暂停时解锁，恢复时锁定

        RefreshUIState();

        if (paused)
        {
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            OnGameUnPaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetTabed(bool tabed)
    {
        if (_isTabed == tabed)
        {
            return;
        }

        _isTabed = tabed;// 切换 Tab 状态时更新 _isTabed 的值

        if (tabed)
        {
            _isSettingUIVisible = false;   // 激活 Tab 状态时隐藏设置界面
        }

        SetCursorLocked(!tabed);   // 切换 Tab 状态时更新鼠标锁定状态

        RefreshUIState();

    }

    public void ShowSettingsUI()
    {
        if (!_isPaused)
        {
            SetPaused(true);
        }

        _isSettingUIVisible = true;

        if (settingManager != null)
        {
            settingManager.gameObject.SetActive(true);
            settingManager.transform.SetAsLastSibling();
        }

        RefreshUIState();
    }

    /// <summary>
    /// 这个方法用于显示暂停界面，当玩家按下暂停键时，UIManager 会调用 ShowPauseUI 方法来显示暂停界面，并将游戏状态设置为暂停。如果游戏已经处于暂停状态，则调用 ShowPauseUI 方法会隐藏暂停界面并恢复游戏状态。
    /// </summary>
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

    /// <summary>
    /// 这个方法用于处理设置应用事件，当玩家在设置界面中应用设置时，UIManager 会调用 HandleSettingsApplied 方法来处理设置应用事件，并根据新的设置状态更新 UI 的显示状态。例如，如果玩家在设置界面中更改了音频设置，UIManager 可能会根据新的音频设置状态来决定是否显示暂停界面或其他相关 UI 元素。
    /// </summary>
    /// <param name="audioSettingState"></param>
    private void HandleSettingsApplied(SettingManager.AudioSettingState audioSettingState)
    {
        ShowPauseUI();
    }




    //刷新UI状态的方法根据当前的暂停状态和瞄准UI的可见性来更新相关UI元素的显示状态。
    private void RefreshUIState()
    {
        bool isGameplayScene = IsGameplayScene();

        if (pauseUIController != null)
        {
            bool shouldShowPauseUI = isGameplayScene && _isPaused && !_isSettingUIVisible;

            pauseUIController.gameObject.SetActive(shouldShowPauseUI);

            if (shouldShowPauseUI)
            {
                pauseUIController.transform.SetAsLastSibling();
            }
        }

        if (missionPannelUIController != null)
        {
            bool shouldShowMissionPanel = isGameplayScene && _isTabed && !_isSettingUIVisible;

            missionPannelUIController.gameObject.SetActive(shouldShowMissionPanel);

            if (shouldShowMissionPanel)
            {
                missionPannelUIController.transform.SetAsLastSibling();
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
            tankStatsUIController.gameObject.SetActive(isGameplayScene && !IsGameplayControlLocked);// 坦克状态UI在未暂停、未打开 Tab、未显示设置界面时才显示
        }

        if (sightUIPanel != null)
        {
            sightUIPanel.gameObject.SetActive(isGameplayScene && !IsGameplayControlLocked && _isSightUIVisible);// 瞄准UI在未暂停、未打开 Tab、未显示设置界面时才显示
        }
    }

    private bool IsGameplayScene()
    {
        return SceneLoader.IsScene(SceneManager.GetActiveScene(), SceneLoader.Scene.GameScene);
    }

}
