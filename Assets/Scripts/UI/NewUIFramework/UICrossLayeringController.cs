using UnityEngine;

/// <summary>
/// 跨 UI 层级排序控制器 —— 处理 UGUI Canvas 与 UI Toolkit UIDocument 间的排序冲突。
/// Pause/Setting 打开时将 Map 的 UIDocument.sortingOrder 下调、Canvas.sortingOrder 上调，
/// 确保 Pause/Setting Canvas 始终在 UI Toolkit 面板上方。
/// 从 UIManager 提取的独立策略组件，遵循解耦原则。
/// </summary>
public class UICrossLayeringController : MonoBehaviour
{
    [Header("UGUI vs UI Toolkit 跨层级排序配置")]
    [Tooltip("Pause 打开时：把 Map 的 UIDocument.sortingOrder 降到这个值")]
    [SerializeField] private float _pauseMapUIDocumentLowering = -1000f;

    [Tooltip("Pause 打开时：把 Pause Canvas.sortingOrder 提到这个增加值")]
    [SerializeField] private int _pauseCanvasRaise = 1000;

    [Tooltip("Setting 打开时（Pause 期间）：在 Pause Canvas Raise 基础上额外抬高 Setting Canvas")]
    [SerializeField] private int _settingCanvasRaiseAdditional = 10;

    [Header("依赖引用")]
    [Tooltip("Map UIController（提供 UIDocument.sortingOrder 访问）")]
    [SerializeField] private MapUIController _mapUIController;

    [Tooltip("Pause UIController（提供 Canvas 引用）")]
    [SerializeField] private PauseUIController _pauseUIController;

    [Tooltip("Setting Manager（提供 Canvas 引用）")]
    [SerializeField] private SettingManager _settingManager;

    private bool _hasCachedLayering;
    private float _originMapUIDocumentSortingOrder;
    private int _originPauseCanvasSortingOrder;
    private int _originSettingCanvasSortingOrder;

    private Canvas _pauseCanvas;
    private Canvas _settingCanvas;

    /// <summary>缓存 Canvas 引用和原始排序值（首次调用时执行）</summary>
    public void CacheIfNeeded()
    {
        if (_hasCachedLayering) return;
        if (_mapUIController == null || _pauseUIController == null || _settingManager == null) return;

        _pauseCanvas = _pauseUIController.GetComponentInParent<Canvas>(true);
        _settingCanvas = _settingManager.GetComponentInParent<Canvas>(true);

        _originMapUIDocumentSortingOrder = _mapUIController.GetUIDocumentSortingOrder();
        _originPauseCanvasSortingOrder = _pauseCanvas != null ? _pauseCanvas.sortingOrder : 0;
        _originSettingCanvasSortingOrder = _settingCanvas != null ? _settingCanvas.sortingOrder : 0;

        _hasCachedLayering = true;
    }

    /// <summary>Pause 打开时的跨层级排序调整</summary>
    public void ApplyForPause()
    {
        if (!_hasCachedLayering) return;

        _mapUIController?.SetUIDocumentSortingOrder(_pauseMapUIDocumentLowering);

        if (_pauseCanvas != null)
            _pauseCanvas.sortingOrder = _originPauseCanvasSortingOrder + _pauseCanvasRaise;
    }

    /// <summary>Setting 打开时的附加排序调整</summary>
    public void ApplyForSetting()
    {
        if (!_hasCachedLayering) return;

        if (_settingCanvas != null)
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder + _pauseCanvasRaise + _settingCanvasRaiseAdditional;
    }

    /// <summary>关闭 Setting 时恢复 Setting Canvas 排序</summary>
    public void RestoreSettingCanvas()
    {
        if (!_hasCachedLayering) return;

        if (_settingCanvas != null)
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder;
    }

    /// <summary>退出 Pause 时全部恢复原始排序</summary>
    public void RestoreAll()
    {
        if (!_hasCachedLayering) return;

        _mapUIController?.SetUIDocumentSortingOrder(_originMapUIDocumentSortingOrder);

        if (_pauseCanvas != null)
            _pauseCanvas.sortingOrder = _originPauseCanvasSortingOrder;

        if (_settingCanvas != null)
            _settingCanvas.sortingOrder = _originSettingCanvasSortingOrder;

        _hasCachedLayering = false;
    }
}
