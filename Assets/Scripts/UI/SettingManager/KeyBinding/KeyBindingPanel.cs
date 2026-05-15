using System.Collections.Generic;
using NNewUIFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 按键设置面板UI，管理所有 KeyBindingItemWidget 并协调重绑定流程。
/// </summary>
public class KeyBindingPanel : UGUIViewAdapter
{
    public override EUIIdentity identity => EUIIdentity.KeyBindingPanel;

    [SerializeField] private Transform _contentRoot;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Button _resetDefaultsButton;
    [SerializeField] private Button _backButton;

    private readonly List<KeyBindingItemWidget> _items = new List<KeyBindingItemWidget>();
    private readonly List<KeyBindingItemWidget> _prePlacedItems = new List<KeyBindingItemWidget>();
    private readonly List<KeyBindingItemWidget> _dynamicItems = new List<KeyBindingItemWidget>();
    private KeyBindingManager _manager;

    protected override void Awake()
    {
        base.Awake();
        if (_manager == null)
        {
            _manager = new KeyBindingManager(MIddleInputingController.Instance?.InputAsset);
        }

        ScanPrePlacedItems();

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(OnBackClicked);
        }

        if (_resetDefaultsButton != null)
        {
            _resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
        }
    }

    /// <summary>
    /// 扫描 Content 下已有的 KeyBindingItemWidget，作为预置条目缓存。
    /// </summary>
    private void ScanPrePlacedItems()
    {
        _prePlacedItems.Clear();
        if (_contentRoot == null)
        {
            Debug.LogWarning("[KeyBindingPanel] _contentRoot 为空，无法扫描预置条目");
            return;
        }

        KeyBindingItemWidget[] existing = _contentRoot.GetComponentsInChildren<KeyBindingItemWidget>();
        _prePlacedItems.AddRange(existing);
    }

    private void OnDestroy()
    {
        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(OnBackClicked);
        }

        if (_resetDefaultsButton != null)
        {
            _resetDefaultsButton.onClick.RemoveListener(OnResetDefaultsClicked);
        }
    }

    protected override void OnOpened(object data)
    {
        BuildActionList();
    }

    protected override void OnClosing()
    {
        ClearItems();
    }

    private void BuildActionList()
    {
        ClearItems();

        if (_contentRoot == null)
        {
            Debug.LogError("[KeyBindingPanel] 未找到有效的 KeyBinding ContentRoot");
            return;
        }

        InputActionMap tankerMap = MIddleInputingController.Instance?.TankerDriverMap;
        if (tankerMap == null)
        {
            Debug.LogError("[KeyBindingPanel] TankerDriver ActionMap 为空");
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
                _items.Add(widget);
            }
            else
            {
                if (_itemPrefab == null)
                {
                    Debug.LogWarning("[KeyBindingPanel] _itemPrefab 为空，无法动态创建条目");
                    continue;
                }

                GameObject itemGo = Instantiate(_itemPrefab, _contentRoot);
                KeyBindingItemWidget widget = itemGo.GetComponent<KeyBindingItemWidget>();
                if (widget != null)
                {
                    widget.Initialize(action, _manager);
                    _items.Add(widget);
                    _dynamicItems.Add(widget);
                }
            }
        }

        // 隐藏多余的预置条目
        for (int i = actions.Count; i < prePlacedCount; i++)
        {
            _prePlacedItems[i].gameObject.SetActive(false);
        }
    }

    private void ClearItems()
    {
        // 销毁动态创建的条目
        foreach (KeyBindingItemWidget item in _dynamicItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        _dynamicItems.Clear();

        // 隐藏预置条目
        foreach (KeyBindingItemWidget item in _prePlacedItems)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }

        _items.Clear();
    }

    private void OnResetDefaultsClicked()
    {
        _manager?.ResetAllBindings();
        RefreshAllItems();
    }

    private void OnBackClicked()
    {
        _manager?.SaveBindings();
        if (NewUIManager.instance != null)
        {
            NewUIManager.instance.CloseKeyBindingUI();
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
