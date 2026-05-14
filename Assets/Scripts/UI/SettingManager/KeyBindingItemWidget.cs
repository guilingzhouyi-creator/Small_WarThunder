using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 按键设置面板中的单行条目组件，显示按键名称与当前绑定，支持点击重绑定。
/// </summary>
public class KeyBindingItemWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text _actionLabel;
    [SerializeField] private TMP_Text _bindingLabel;
    [SerializeField] private Button _rebindButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private GameObject _listeningIndicator;

    private InputAction _action;
    private KeyBindingManager _manager;
    private string _actionId;

    /// <summary>当前条目的绑定发生变化时触发。</summary>
    public event System.Action onBindingChanged;

    /// <summary>
    /// 初始化条目，传入对应的 InputAction 和 KeyBindingManager。
    /// </summary>
    public void Initialize(InputAction action, KeyBindingManager manager)
    {
        _action = action;
        _manager = manager;
        _actionId = action?.id.ToString();

        if (_actionLabel != null)
        {
            _actionLabel.text = ActionDisplayNameMapping.GetDisplayName(action?.name);
        }

        RefreshDisplay();

        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveListener(OnRebindClicked);
            _rebindButton.onClick.AddListener(OnRebindClicked);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(OnResetClicked);
            _resetButton.onClick.AddListener(OnResetClicked);
        }

        if (_manager != null)
        {
            _manager.BindingChanged += OnManagerBindingChanged;
            _manager.RebindStarted += OnManagerRebindStarted;
            _manager.RebindCancelled += OnManagerRebindCancelled;
        }
    }

    private void OnDestroy()
    {
        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveListener(OnRebindClicked);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(OnResetClicked);
        }

        if (_manager != null)
        {
            _manager.BindingChanged -= OnManagerBindingChanged;
            _manager.RebindStarted -= OnManagerRebindStarted;
            _manager.RebindCancelled -= OnManagerRebindCancelled;
        }
    }

    /// <summary>
    /// 刷新面板上显示的绑定文本。
    /// </summary>
    public void RefreshDisplay()
    {
        if (_bindingLabel == null || _manager == null || string.IsNullOrEmpty(_actionId))
        {
            return;
        }

        string display = _manager.GetCurrentBindingDisplayString(_actionId);
        _bindingLabel.text = string.IsNullOrEmpty(display) ? "未绑定" : display;
    }

    private void OnRebindClicked()
    {
        if (_manager == null || string.IsNullOrEmpty(_actionId))
        {
            return;
        }

        if (_manager.IsRebinding)
        {
            Debug.LogWarning("[KeyBindingItemWidget] 已在重绑定中，请先完成当前操作");
            return;
        }

        _manager.PerformRebind(_actionId);
        SetListeningState(true);
    }

    private void OnResetClicked()
    {
        if (_manager == null || string.IsNullOrEmpty(_actionId))
        {
            return;
        }

        _manager.ResetToDefault(_actionId);
    }

    private void OnManagerBindingChanged(string changedActionId, string displayString)
    {
        if (changedActionId == _actionId)
        {
            RefreshDisplay();
            SetListeningState(false);
            onBindingChanged?.Invoke();
        }
    }

    private void OnManagerRebindStarted(string startedActionId)
    {
        if (startedActionId != _actionId)
        {
            SetListeningState(false);
        }
    }

    private void OnManagerRebindCancelled(string cancelledActionId)
    {
        if (cancelledActionId == _actionId)
        {
            SetListeningState(false);
            RefreshDisplay();
        }
    }

    private void SetListeningState(bool listening)
    {
        if (_listeningIndicator != null)
        {
            _listeningIndicator.SetActive(listening);
        }

        if (_bindingLabel != null && listening)
        {
            _bindingLabel.text = KeyBindingConstants.UiLabelListeningPrompt;
        }

        if (_rebindButton != null)
        {
            _rebindButton.interactable = !listening;
        }
    }
}
