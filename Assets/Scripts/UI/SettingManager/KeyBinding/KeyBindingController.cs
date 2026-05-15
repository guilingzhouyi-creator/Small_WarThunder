using System.Collections.Generic;
using NSettingSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 按键设置 Tab 控制器：管理按键重绑定列表 UI、重置默认、保存。
/// apply/cancel 按钮由 SettingManager 统一管理，通过 ISettingTabController 接口调度。
/// </summary>
public partial class KeyBindingController : MonoBehaviour, ISettingTabController
{
    public string tabKey => SettingConstants.TabKeyKeyBinding;

    [SerializeField] private Transform _contentRoot;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Button _resetDefaultsButton;

    private readonly List<KeyBindingItemWidget> _items = new List<KeyBindingItemWidget>();
    private readonly List<KeyBindingItemWidget> _prePlacedItems = new List<KeyBindingItemWidget>();
    private readonly List<KeyBindingItemWidget> _dynamicItems = new List<KeyBindingItemWidget>();
    private KeyBindingManager _manager;
    private bool _hasChanges;

    public void OnTabOpened()
    {
        EnsureManager();
        ScanPrePlacedItems();
        BuildActionList();
    }

    public void OnTabClosed()
    {
        ClearItems();
    }

    public bool OnBackRequested()
    {
        if (_hasChanges)
        {
            Debug.Log("[KeyBindingController] 有未保存的按键更改，请先保存或取消");
            return false;
        }

        return true;
    }

    /// <summary>SettingManager 统一调度的 Apply 入口。</summary>
    public void OnApplyRequested()
    {
        Debug.Log("[KeyBindingController] OnApplyRequested");
        SaveSettings();
    }

    /// <summary>SettingManager 统一调度的 Cancel 入口。</summary>
    public void OnCancelRequested()
    {
        Debug.Log("[KeyBindingController] OnCancelRequested");
        CancelSettings();
    }

    private void ScanPrePlacedItems()
    {
        _prePlacedItems.Clear();
        if (_contentRoot == null)
        {
            Debug.LogWarning("[KeyBindingController] _contentRoot 为空，无法扫描预置条目");
            return;
        }

        KeyBindingItemWidget[] existing = _contentRoot.GetComponentsInChildren<KeyBindingItemWidget>();
        _prePlacedItems.AddRange(existing);
    }

    private void Awake()
    {
        Debug.Log("[KeyBindingController] Awake");
    }

    private void OnEnable()
    {
        EnsureManager();
        BindUIListeners();
    }

    private void OnDisable()
    {
        UnbindUIListeners();
    }

    private void EnsureManager()
    {
        if (_manager != null) return;

        InputActionAsset inputAsset = MIddleInputingController.Instance?.InputAsset;
        if (inputAsset == null)
        {
            Debug.LogError("[KeyBindingController] InputAsset 为空，无法创建 KeyBindingManager");
            return;
        }

        _manager = new KeyBindingManager(inputAsset);
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
        if (_resetDefaultsButton != null) _resetDefaultsButton.onClick.RemoveListener(OnResetDefaultsClicked);
    }

    private void BuildActionList()
    {
        ClearItems();

        if (_contentRoot == null)
        {
            Debug.LogError("[KeyBindingController] 未找到有效的 KeyBinding ContentRoot");
            return;
        }

        InputActionMap tankerMap = MIddleInputingController.Instance?.TankerDriverMap;
        if (tankerMap == null)
        {
            Debug.LogError("[KeyBindingController] TankerDriver ActionMap 为空");
            return;
        }

        var actions = tankerMap.actions;
        int prePlacedCount = _prePlacedItems.Count;

        for (int i = 0; i < actions.Count; i++)
        {
            InputAction action = actions[i];

            if (i < prePlacedCount)
            {
                KeyBindingItemWidget widget = _prePlacedItems[i];
                widget.gameObject.SetActive(true);
                widget.Initialize(action, _manager);
                widget.onBindingChanged += OnItemBindingChanged;
                _items.Add(widget);
            }
            else
            {
                if (_itemPrefab == null)
                {
                    Debug.LogWarning("[KeyBindingController] _itemPrefab 为空，无法动态创建条目");
                    continue;
                }

                GameObject itemGo = Instantiate(_itemPrefab, _contentRoot);
                KeyBindingItemWidget widget = itemGo.GetComponent<KeyBindingItemWidget>();
                if (widget != null)
                {
                    widget.Initialize(action, _manager);
                    widget.onBindingChanged += OnItemBindingChanged;
                    _items.Add(widget);
                    _dynamicItems.Add(widget);
                }
            }
        }

        for (int i = actions.Count; i < prePlacedCount; i++)
        {
            _prePlacedItems[i].gameObject.SetActive(false);
        }
    }

    private void ClearItems()
    {
        foreach (KeyBindingItemWidget item in _dynamicItems)
        {
            if (item != null)
            {
                item.onBindingChanged -= OnItemBindingChanged;
                Destroy(item.gameObject);
            }
        }

        _dynamicItems.Clear();

        foreach (KeyBindingItemWidget item in _prePlacedItems)
        {
            if (item != null)
            {
                item.onBindingChanged -= OnItemBindingChanged;
                item.gameObject.SetActive(false);
            }
        }

        _items.Clear();
    }

    private void OnItemBindingChanged()
    {
        _hasChanges = true;
    }

    private void OnResetDefaultsClicked()
    {
        _manager?.ResetAllBindings();
        RefreshAllItems();
        _hasChanges = true;
    }

    private void SaveSettings()
    {
        _manager?.SaveBindings();
        _hasChanges = false;
        Debug.Log("[KeyBindingController] 按键设置已保存");
    }

    private void CancelSettings()
    {
        if (_hasChanges)
        {
            RefreshAllItems();
            _hasChanges = false;
        }
    }

    private void RefreshAllItems()
    {
        foreach (KeyBindingItemWidget item in _items)
        {
            item?.RefreshDisplay();
        }
    }
}
