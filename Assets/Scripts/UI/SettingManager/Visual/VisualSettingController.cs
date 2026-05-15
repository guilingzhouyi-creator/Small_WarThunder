using NSettingSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面设置 Tab 控制器占位。
/// 当前只负责接入 SettingManager 路由，后续可按 KeyBindingController 的模式继续扩展具体画面设置项。
/// </summary>
public partial class VisualSettingController : MonoBehaviour, ISettingTabController
{
    public string tabKey => SettingConstants.TabKeyVisual;

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
            Debug.Log("[VisualSettingController] 有未保存的画面设置更改，请先保存或取消", this);
            return false;
        }

        return true;
    }

    /// <summary>SettingManager 统一调度的 Apply 入口。</summary>
    public void OnApplyRequested()
    {
        Debug.Log("[VisualSettingController] OnApplyRequested", this);
        SaveSettings();
    }

    /// <summary>SettingManager 统一调度的 Cancel 入口。</summary>
    public void OnCancelRequested()
    {
        Debug.Log("[VisualSettingController] OnCancelRequested", this);
        CancelSettings();
    }

    private void Awake()
    {
        Debug.Log("[VisualSettingController] Awake", this);
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
        Debug.Log("[VisualSettingController] Placeholder initialized", this);
    }

    private void ResolveResetDefaultsButton()
    {
        if (_resetDefaultsButton != null && _resetDefaultsButton.gameObject.name == SettingConstants.ButtonNameVisualReset)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button != null && button.gameObject.name == SettingConstants.ButtonNameVisualReset)
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
        Debug.Log("[VisualSettingController] Reset defaults placeholder", this);
        _hasChanges = true;
    }

    private void SaveSettings()
    {
        _hasChanges = false;
        Debug.Log("[VisualSettingController] Placeholder save completed", this);
    }

    private void CancelSettings()
    {
        _hasChanges = false;
        Debug.Log("[VisualSettingController] Placeholder cancel completed", this);
    }
}
