using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 地图 UI 控制器 — UIToolkit 方案（UGUI-Free）。
/// 
/// 职责：
/// - 管理 UIDocument（创建/设置）。
/// - 托管 MapRenderingEngine（VisualElement，Painter2D 地图渲染）。
/// - 处理小地图/大地图模式切换。
/// - 由 UIManager 调用，实现地图显隐和模式控制。
/// 
/// 架构：
/// - MapRenderingEngine 继承 VisualElement，按时间间隔刷新（非帧驱动）。
/// - 此控制器持有配置资产 MapConfigSO，并传递给 Engine。
/// - 玩家 Transform 从此控制器传递到 Engine。
/// - MapCameraPosition 从此控制器传递到 Engine 以获取俯拍相机状态。
/// </summary>
public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }

    [Header("UIDocument")]
    [SerializeField] private UIDocument _uiDocument;

    [Header("地图配置")]
    [SerializeField] private MapConfigSO _config;

    [Header("俯拍相机引用")]
    [SerializeField] private MapCameraPosition _mapCamera;

    private MapRenderingEngine _engine;
    private Transform _playerTransform;
    private bool _isInitialized;

    public bool IsFullMapShown => _engine != null && _engine.IsFullMapOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeEngine();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (_engine != null && _uiDocument != null)
        {
            _uiDocument.rootVisualElement?.Remove(_engine);
        }
    }

    #region Initialization

    private void InitializeEngine()
    {
        if (_uiDocument == null)
        {
            Debug.LogError("[MapUIController] UIDocument 未赋值！请在 Inspector 中拖入。");
            return;
        }

        if (_config == null)
        {
            Debug.LogError("[MapUIController] MapConfigSO 未赋值！请在 Inspector 中拖入。");
            return;
        }

        // 创建并添加 MapRenderingEngine 到根 VisualElement
        _engine = new MapRenderingEngine(_config);
        _engine.SetCamera(_mapCamera);
        _engine.SetVisible(false); // 默认隐藏，由 UIManager 控制显隐

        _uiDocument.rootVisualElement.Add(_engine);
        _isInitialized = true;
    }

    #endregion

    #region Update

    private void Update()
    {
        if (!_isInitialized || _engine == null) return;
        _engine.Tick();
    }

    #endregion

    #region Public API (called by UIManager)

    /// <summary>
    /// 由 UIManager 设置当前玩家 Transform。
    /// </summary>
    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _engine?.SetPlayer(playerTransform);
    }

    /// <summary>
    /// 打开大地图：切换引擎为全屏模式（Painter2D 全屏覆盖）。
    /// </summary>
    public void OpenFullMap()
    {
        if (_engine == null) return;
        _engine.OpenFullMap();
    }

    /// <summary>
    /// 关闭大地图：引擎恢复小地图模式。
    /// </summary>
    public void CloseFullMap()
    {
        if (_engine == null) return;
        _engine.CloseFullMap();
    }

    /// <summary>
    /// 控制小地图常驻的显隐。
    /// TPS 时显示，AIM / 暂停 / Tab / 大地图时隐藏。
    /// </summary>
    public void SetMiniMapVisible(bool visible)
    {
        if (_engine == null) return;
        _engine.SetVisible(visible);
    }

    #endregion
}
