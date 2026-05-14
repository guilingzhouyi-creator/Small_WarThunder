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
    }

    private void Update()
    {
        HandleTabNavigationInput();
    }

    private void HandleTabNavigationInput()
    {
        if (_settingTabNavigationButtons == null || _settingTabNavigationButtons.Count <= 0) return;

        int newIndex = _currentTabIndex;
        bool shouldSwitch = false;

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            newIndex = (_currentTabIndex + 1) % _settingTabNavigationButtons.Count;
            shouldSwitch = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            newIndex = (_currentTabIndex - 1 + _settingTabNavigationButtons.Count) % _settingTabNavigationButtons.Count;
            shouldSwitch = true;
        }

        if (shouldSwitch && newIndex != _currentTabIndex)
        {
            SwitchTab(newIndex);
        }
    }

    /// <summary>遍历 SubSettingEntry 列表，构建映射并为 apply/cancel 按钮绑定统一路由，初始全部隐藏由 SwitchTab 激活。</summary>
    private void BindUIListeners()
    {
        _subSettingMap = new Dictionary<string, SubSettingEntry>();
        foreach (SubSettingEntry entry in _subSettingEntries)
        {
            ISettingTabController tabCtrl = entry.controller as ISettingTabController;
            if (tabCtrl == null) continue;

            string key = tabCtrl.tabKey;
            if (string.IsNullOrEmpty(key)) continue;

            _subSettingMap[key] = entry;

            if (entry.applyButton != null)
            {
                entry.applyButton.onClick.RemoveAllListeners();
                entry.applyButton.onClick.AddListener(OnApplyPressed);
                entry.applyButton.gameObject.SetActive(false);
            }

            if (entry.cancelButton != null)
            {
                entry.cancelButton.onClick.RemoveAllListeners();
                entry.cancelButton.onClick.AddListener(OnCancelPressed);
                entry.cancelButton.gameObject.SetActive(false);
            }
        }
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

        if (!string.IsNullOrEmpty(_activeTabKey) && _subSettingMap != null && _subSettingMap.TryGetValue(_activeTabKey, out SubSettingEntry oldEntry))
        {
            (oldEntry.controller as ISettingTabController)?.OnTabClosed();
        }

        _activeTabKey = newCtrl.tabKey;
        _currentTabIndex = tabIndex;
        newCtrl.OnTabOpened();

        RefreshApplyCancelButtonVisibility();
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
