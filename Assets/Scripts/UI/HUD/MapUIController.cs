using UnityEngine;
using UnityEngine.UIElements;
using NNewUIFramework;

public class MapUIController : UIToolkitViewAdapter<object>
{
    public override EUIIdentity identity => EUIIdentity.MapPanel;

    public static MapUIController Instance { get; private set; }

    [Header("地图配置")]
    [SerializeField] private MapConfigSO _config;

    [Header("俯拍相机引用")]
    [SerializeField] private MapCameraPosition _mapCamera;

    private MapRenderingEngine _engine;
    private Transform _playerTransform;
    private bool _isInitialized;

    public bool IsFullMapShown => _engine != null && _engine.IsFullMapOpen;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveSceneReferences();
    }

    /// <summary>
    /// 当 GameObject 被激活时（跨场景 / RefreshUIState 触发），
    /// 强制重置引擎为小地图布局，防止上一次全屏地图的布局残留。
    /// 同时确保引擎已初始化（懒初始化，不依赖框架 Open/OnOpened 路径）。
    /// </summary>
    private void OnEnable()
    {
        // MapPanel 由 RefreshUIState 直接管理，不走框架 Open/Close 路径。
        // UIToolkitViewAdapter.Awake() 将 _rootVisual.display 设为 None，
        // 且 UIDocument 重新激活时不会重建视觉树（display 不会自动恢复为 Flex），
        // 因此必须在此显式将 rootVisualElement 设为可见。
        if (_rootVisual == null)
        {
            ResolveUIDocumentIfNeeded();
        }

        if (_rootVisual != null)
        {
            _rootVisual.style.display = DisplayStyle.Flex;
        }

        // 确保引擎已初始化（MapPanel 由 RefreshUIState 直接管理，不走框架 Open/Close 路径）
        EnsureEngineInitialized();

        if (_engine != null)
        {
            // 无条件关闭全屏地图，确保引擎布局为小地图
            _engine.CloseFullMap();
            _engine.SetVisible(false);
        }
    }

    /// <summary>懒初始化引擎，供 OnEnable 及各个公开方法复用。
    /// 同时处理跨场景 / UIDocument 重建后引擎脱落的情况。</summary>
    private void EnsureEngineInitialized()
    {
        ResolveSceneReferences();

        // 引擎存在但已从视觉树脱落（跨场景 / UIDocument 重建时 DetachFromPanelEvent 触发），
        // 需要重新挂回当前 rootVisualElement，并清除 _isInitialized 标志以重建 RenderTexture
        bool engineDetached = _engine != null && _isInitialized
                              && (_engine.parent == null || _engine.panel == null);

        if (engineDetached)
        {
            // 强制重新初始化：RenderTexture 已被 ReleaseResources 释放，需走完整创建流程
            _isInitialized = false;

            // 如果旧引擎仍可复用（仅布局/样式需重置），直接重挂
            if (_uiDocument != null && _engine != null)
            {
                _uiDocument.rootVisualElement.Add(_engine);
            }
        }

        if (_engine != null && _isInitialized)
            return;

        InitializeEngine();
    }

    protected override void OnOpened(object data)
    {
        ResolveSceneReferences();
        InitializeEngine();
    }

    protected override void OnClosing()
    {
        CloseFullMap();
    }

    private void Update()
    {
        ResolveSceneReferences();

        if (!_isInitialized || _engine == null)
        {
            return;
        }

        _engine.Tick();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_engine != null && _uiDocument != null)
        {
            _uiDocument.rootVisualElement?.Remove(_engine);
        }
    }

    private void ResolveSceneReferences()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = GetComponentInChildren<UIDocument>(true);
            }
        }

        if (_mapCamera == null)
        {
            _mapCamera = FindFirstObjectByType<MapCameraPosition>(FindObjectsInactive.Include);
        }

        GameObject playerTank = GameManager.Instance != null ? GameManager.Instance.PlayerTank : null;
        Transform nextPlayerTransform = playerTank != null ? playerTank.transform : null;
        if (_playerTransform != nextPlayerTransform)
        {
            _playerTransform = nextPlayerTransform;
            _engine?.SetPlayer(_playerTransform);
            if (_mapCamera != null && _playerTransform != null)
            {
                _mapCamera.BindTarget(_playerTransform);
            }
        }

        if (_engine != null)
        {
            _engine.SetCamera(_mapCamera);
        }
    }

    private void InitializeEngine()
    {
        if (_isInitialized)
        {
            if (_engine != null)
            {
                _engine.SetCamera(_mapCamera);
                _engine.SetPlayer(_playerTransform);
            }

            return;
        }

        if (_uiDocument == null)
        {
            Debug.LogError("[MapUIController] UIDocument 未赋值，请检查 UI 预制体。", this);
            return;
        }

        if (_config == null)
        {
            Debug.LogError("[MapUIController] MapConfigSO 未赋值，请在 Inspector 中配置。", this);
            return;
        }

        _engine = new MapRenderingEngine(_config);
        _engine.SetCamera(_mapCamera);
        _engine.SetPlayer(_playerTransform);
        _engine.SetVisible(false);

        _uiDocument.rootVisualElement.Add(_engine);

        _isInitialized = true;
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _engine?.SetPlayer(playerTransform);
        if (_mapCamera != null && playerTransform != null)
        {
            _mapCamera.BindTarget(playerTransform);
        }
    }

    public void OpenFullMap()
    {
        EnsureEngineInitialized();
        if (_engine == null) return;

        _engine.SetVisible(true);
        _engine.OpenFullMap();
    }

    public void CloseFullMap()
    {
        EnsureEngineInitialized();
        if (_engine == null) return;

        _engine.CloseFullMap();
    }


    public void SetMiniMapVisible(bool visible)
    {
        EnsureEngineInitialized();
        if (_engine == null) return;

        if (_engine.IsFullMapOpen)
        {
            _engine.SetVisible(true);
            return;
        }

        _engine.SetVisible(visible);
    }

    public float GetUIDocumentSortingOrder()
    {
        return _uiDocument != null ? _uiDocument.sortingOrder : 0f;
    }

    public void SetUIDocumentSortingOrder(float sortingOrder)
    {
        if (_uiDocument == null)
        {
            return;
        }

        _uiDocument.sortingOrder = sortingOrder;
    }
}
