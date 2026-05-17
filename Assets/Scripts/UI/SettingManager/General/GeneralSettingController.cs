using NSettingSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用设置 Tab 控制器占位。
/// 当前只负责接入 SettingManager 路由，后续可按 KeyBindingController 的模式继续扩展具体设置项。
/// </summary>
public partial class GeneralSettingController : MonoBehaviour, ISettingTabController
{
    public string tabKey => SettingConstants.TabKeyGeneral;

    [SerializeField] private Transform _contentRoot;
    [SerializeField] private Button _resetDefaultsButton;

    private bool _hasChanges;
    private bool _isInitialized;

    public void OnTabOpened()
    {
        EnsureInitialized();
    }

    public void OnTabClosed()
    {
    }

    public bool OnBackRequested()
    {
        if (_hasChanges)
        {
            Debug.Log("[GeneralSettingController] 有未保存的通用设置更改，请先保存或取消", this);
            return false;
        }

        return true;
    }

    /// <summary>SettingManager 统一调度的 Apply 入口。</summary>
    public SettingActionResult OnApplyRequested()
    {
        Debug.Log("[GeneralSettingController] OnApplyRequested", this);
        return SaveSettings();
    }

    public SettingActionResult OnResetRequested()
    {
        Debug.Log("[GeneralSettingController] OnResetRequested", this);
        return ResetSettings();
    }

    /// <summary>SettingManager 统一调度的 Cancel 入口。</summary>
    public SettingActionResult OnCancelRequested()
    {
        Debug.Log("[GeneralSettingController] OnCancelRequested", this);
        return CancelSettings();
    }

    private void Awake()
    {
        Debug.Log("[GeneralSettingController] Awake", this);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        BindUIListeners();
    }

    private void OnDisable()
    {
        UnbindUIListeners();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_contentRoot == null)
        {
            _contentRoot = transform;
        }

        ResolveResetDefaultsButton();

        _isInitialized = true;
        Debug.Log("[GeneralSettingController] Placeholder initialized", this);
    }

    private void ResolveResetDefaultsButton()
    {
        if (_resetDefaultsButton != null && _resetDefaultsButton.gameObject.name == SettingConstants.ButtonNameGeneralReset)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button != null && button.gameObject.name == SettingConstants.ButtonNameGeneralReset)
            {
                _resetDefaultsButton = button;
                return;
            }
        }
    }

    private void BindUIListeners()
    {
        if (_resetDefaultsButton != null)
        {
            _resetDefaultsButton.onClick.RemoveListener(OnResetDefaultsClicked);
            _resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
        }
    }

    private void UnbindUIListeners()
    {
        if (_resetDefaultsButton != null)
        {
            _resetDefaultsButton.onClick.RemoveListener(OnResetDefaultsClicked);
        }
    }

    private void OnResetDefaultsClicked()
    {
        SettingActionResult result = OnResetRequested();
        SettingManager.Instance?.HandleActionResult(result);
    }

    private SettingActionResult SaveSettings()
    {
        _hasChanges = false;
        Debug.Log("[GeneralSettingController] Placeholder save completed", this);
        return SettingActionResult.Success(tabKey, SettingActionType.Apply, "通用设置已应用");
    }

    private SettingActionResult CancelSettings()
    {
        bool hadChanges = _hasChanges;
        _hasChanges = false;
        Debug.Log("[GeneralSettingController] Placeholder cancel completed", this);
        return hadChanges ? SettingActionResult.CancelRollback(tabKey) : SettingActionResult.CancelExit(tabKey);
    }

    private SettingActionResult ResetSettings()
    {
        _hasChanges = true;
        Debug.Log("[GeneralSettingController] Reset defaults placeholder", this);
        return SettingActionResult.Success(tabKey, SettingActionType.Reset, "通用设置已重置");
    }
}
