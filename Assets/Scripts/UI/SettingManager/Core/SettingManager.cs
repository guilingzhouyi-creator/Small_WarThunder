using NNewUIFramework;
using NSettingSystem;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

/// <summary>
/// 设置管理器：管理 Tab 导航、apply/cancel 按钮路由分发。
/// 音频设置逻辑由 AudioSettingController 独立处理，按键设置逻辑由 KeyBindingController 独立处理。
/// </summary>
public partial class SettingManager : UGUIViewAdapter
{
    public override EUIIdentity identity => EUIIdentity.SettingsPanel;

    public static SettingManager Instance { get; private set; }

    /// <summary>外部系统（AudioManager 等）订阅此事件来响应设置变更。</summary>
    public event Action<SettingManager> OnApplyAllSettings;
    public event Action<SettingManager> OnCancelAllSettings;

    /// <summary>子设置面板条目，包含 controller 及该子面板的 apply/cancel 按钮。</summary>
    [Serializable]
    private struct SubSettingEntry
    {
        public MonoBehaviour controller;
        public Button applyButton;
        public Button cancelButton;
    }

    /// <summary>设置 Tab 导航按钮列表，支持鼠标点击和 Tab 键/方向键切换。</summary>
    [SerializeField] private List<Button> _settingTabNavigationButtons;
    /// <summary>子设置面板条目列表，管理 apply/cancel 路由分发。</summary>
    [SerializeField] private List<SubSettingEntry> _subSettingEntries;

    private Dictionary<string, SubSettingEntry> _subSettingMap;
    private string _activeTabKey;
    private int _currentTabIndex;
    private bool _isInitialized;
    private SettingInteractionRouter _interactionRouter;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        _interactionRouter?.EnableInput();
    }

    private void OnDisable()
    {
        _interactionRouter?.DisableInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _interactionRouter?.Dispose();
    }

    private void EnsureInteractionRouter()
    {
        if (_interactionRouter == null)
        {
            _interactionRouter = new SettingInteractionRouter(SwitchToNextTab, SwitchToPreviousTab);
        }
    }

    /// <summary>遍历 SubSettingEntry 列表，构建映射并为 apply/cancel 按钮绑定统一路由，初始全部隐藏由 SwitchTab 激活。</summary>
    private void BindUIListeners()
    {
        EnsureInteractionRouter();
        NormalizeTabConfiguration();
        _subSettingMap = new Dictionary<string, SubSettingEntry>();
        _interactionRouter.BindTabButtons(_settingTabNavigationButtons, SwitchTab);

        if (_subSettingEntries == null)
        {
            _interactionRouter.BindApplyButtons(null, OnApplyPressed);
            _interactionRouter.BindCancelButtons(null, OnCancelPressed);
            return;
        }

        List<Button> applyButtons = new List<Button>(_subSettingEntries.Count);
        List<Button> cancelButtons = new List<Button>(_subSettingEntries.Count);

        foreach (SubSettingEntry entry in _subSettingEntries)
        {
            ISettingTabController tabCtrl = entry.controller as ISettingTabController;
            if (tabCtrl == null) continue;

            string key = tabCtrl.tabKey;
            if (string.IsNullOrEmpty(key)) continue;

            _subSettingMap[key] = entry;
            SetControllerVisible(entry.controller, false);

            if (entry.applyButton != null)
            {
                applyButtons.Add(entry.applyButton);
                entry.applyButton.gameObject.SetActive(false);
            }

            if (entry.cancelButton != null)
            {
                cancelButtons.Add(entry.cancelButton);
                entry.cancelButton.gameObject.SetActive(false);
            }
        }

        _interactionRouter.BindApplyButtons(applyButtons, OnApplyPressed);
        _interactionRouter.BindCancelButtons(cancelButtons, OnCancelPressed);
    }

    /// <summary>根据索引切换到对应的子设置面板，同时控制 apply/cancel 按钮仅对当前活跃 Tab 可见。</summary>
    public void SwitchTab(int tabIndex)
    {
        if (_subSettingEntries == null || tabIndex < 0 || tabIndex >= _subSettingEntries.Count)
        {
            Debug.LogWarning($"SettingManager: 无法切换到索引 {tabIndex}，超出范围。", this);
            return;
        }

        SubSettingEntry entry = _subSettingEntries[tabIndex];
        ISettingTabController newCtrl = entry.controller as ISettingTabController;
        if (newCtrl == null)
        {
            Debug.LogWarning($"SettingManager: 索引 {tabIndex} 的 controller 未实现 ISettingTabController。", this);
            return;
        }

        if (_currentTabIndex == tabIndex && !string.IsNullOrEmpty(_activeTabKey) && _activeTabKey == newCtrl.tabKey)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_activeTabKey) && _subSettingMap != null && _subSettingMap.TryGetValue(_activeTabKey, out SubSettingEntry oldEntry))
        {
            (oldEntry.controller as ISettingTabController)?.OnTabClosed();
            SetControllerVisible(oldEntry.controller, false);
        }

        _activeTabKey = newCtrl.tabKey;
        _currentTabIndex = tabIndex;
        SetControllerVisible(entry.controller, true);
        newCtrl.OnTabOpened();

        RefreshApplyCancelButtonVisibility();
    }

    private void SwitchTabRelative(int step)
    {
        if (_subSettingEntries == null || _subSettingEntries.Count <= 0)
        {
            return;
        }

        int buttonCount = _subSettingEntries.Count;
        int newIndex = (_currentTabIndex + step + buttonCount) % buttonCount;
        SwitchTab(newIndex);
    }

    private static void SetControllerVisible(MonoBehaviour controller, bool visible)
    {
        if (controller == null || controller.gameObject.activeSelf == visible)
        {
            return;
        }

        controller.gameObject.SetActive(visible);
    }

    private void SwitchToNextTab()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        SwitchTabRelative(1);
    }

    private void SwitchToPreviousTab()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        SwitchTabRelative(-1);
    }

    /// <summary>仅显示当前活跃 Tab 的 apply/cancel 按钮，隐藏其他 Tab 的按钮。</summary>
    private void RefreshApplyCancelButtonVisibility()
    {
        if (_subSettingEntries == null) return;

        foreach (SubSettingEntry entry in _subSettingEntries)
        {
            bool isActive = entry.controller != null &&
                           (entry.controller as ISettingTabController)?.tabKey == _activeTabKey;

            if (entry.applyButton != null)
                entry.applyButton.gameObject.SetActive(isActive);

            if (entry.cancelButton != null)
                entry.cancelButton.gameObject.SetActive(isActive);
        }
    }

    private ICallbackRouterCallback callbackRouterCallback;
    public interface ICallbackRouterCallback
    {
        bool RouteApplyToSubControllers(List<MonoBehaviour> controllers);
    }

    private void OnApplyPressed()
    {
        if (TryGetActiveController(out ISettingTabController controller))
        {
            controller.OnApplyRequested();
        }

        OnApplyAllSettings?.Invoke(this);
    }

    private void OnCancelPressed()
    {
        if (TryGetActiveController(out ISettingTabController controller))
        {
            controller.OnCancelRequested();
        }

        OnCancelAllSettings?.Invoke(this);
    }

    private bool TryGetActiveController(out ISettingTabController controller)
    {
        controller = null;
        if (_subSettingMap == null || string.IsNullOrEmpty(_activeTabKey))
        {
            Debug.LogWarning("SettingManager: 无活跃 Tab，无法路由。", this);
            return false;
        }

        if (!_subSettingMap.TryGetValue(_activeTabKey, out SubSettingEntry entry))
        {
            Debug.LogWarning($"SettingManager: 活跃 Tab 键 '{_activeTabKey}' 在映射中未找到。", this);
            return false;
        }

        controller = entry.controller as ISettingTabController;
        return controller != null;
    }

    public void ShowSettingsPanel()
    {
        NewUIManager.instance?.ShowSettingsUI();
    }

    public void HideSettingsPanel()
    {
        NewUIManager.instance?.CloseSettingsUI();
    }
}
