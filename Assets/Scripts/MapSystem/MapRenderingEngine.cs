using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 地图渲染引擎 — UIToolkit VisualElement，使用 Painter2D 绘制地图。
/// 
/// 时间驱动（非帧驱动）：
/// - 外部通过 Tick() 按 _config.UpdateInterval 间隔采集 MapSnapshot。
/// - 采集后调用 MarkDirtyRepaint() 触发 OnGenerateVisualContent。
/// - 未脏时跳过绘制，降低 GPU 开销。
/// 
/// 支持小地图（常驻右下角）和大地图（全屏 M 键）两种模式。
/// 世界坐标 → UI 像素坐标映射在 CollectSnapshot 中完成。
/// </summary>
public class MapRenderingEngine : VisualElement
{
    // ===== 外部依赖 =====
    private MapConfigSO _config;
    private MapCameraPosition _mapCamera;
    private Transform _playerTransform;

    // ===== 时间间隔控制 =====
    private float _updateInterval;
    private float _lastTickTime;

    // ===== 快照与脏标记 =====
    private MapSnapshot _currentSnapshot;
    private bool _dirty;

    // ===== 绘制模式 =====
    private bool _isFullMapOpen;
    private bool _isVisible;

    // ===== 备用标记数据存储（避免每次分配 GC） =====
    private readonly List<MapMarkerData> _alphaMarkers = new List<MapMarkerData>();

    public bool IsFullMapOpen => _isFullMapOpen;
    public bool IsVisible => _isVisible;

    /// <summary>
    /// 构造函数。必须在添加到 UIDocument.visualTree 之前设置 Config。
    /// </summary>
    public MapRenderingEngine(MapConfigSO config)
    {
        _config = config;
        _updateInterval = config != null ? config.UpdateInterval : 0.1f;
        _currentSnapshot = MapSnapshot.Empty;

        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<DetachFromPanelEvent>(_ => ReleaseResources());

        style.position = Position.Absolute;
        style.left = 0f;
        style.top = 0f;
        style.right = 0f;
        style.bottom = 0f;
        style.width = Length.Percent(100);
        style.height = Length.Percent(100);
        pickingMode = PickingMode.Ignore;
    }

    // ===== 公开配置方法 =====

    public void SetCamera(MapCameraPosition camera) => _mapCamera = camera;
    public void SetPlayer(Transform player) => _playerTransform = player;
    public void SetConfig(MapConfigSO config)
    {
        _config = config;
        _updateInterval = config != null ? config.UpdateInterval : 0.1f;
        _dirty = true;
    }

    /// <summary>切换为全屏大地图模式。</summary>
    public void OpenFullMap()
    {
        _isFullMapOpen = true;
        _dirty = true;
        ForceTick();
        MarkDirtyRepaint();
    }

    /// <summary>切换为小地图常驻模式。</summary>
    public void CloseFullMap()
    {
        _isFullMapOpen = false;
        _dirty = true;
        ForceTick();
        MarkDirtyRepaint();
    }

    /// <summary>控制地图引擎的显隐。</summary>
    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ===== 时间驱动 Tick =====

    /// <summary>
    /// 由外部 Update() 调用，内部判断时间间隔。
    /// 超过 _updateInterval 秒时采集快照并标记重绘。
    /// </summary>
    public void Tick()
    {
        if (!_isVisible) return;

        float now = Time.time;
        if (now - _lastTickTime < _updateInterval) return;
        _lastTickTime = now;

        CollectSnapshot();
        _dirty = true;

        if (panel != null)
            MarkDirtyRepaint();
    }

    /// <summary>强制立即刷新（打开/关闭大地图时使用）。</summary>
    private void ForceTick()
    {
        _lastTickTime = 0f; // 强制下一次 Tick 通过
        CollectSnapshot();
        _dirty = true;
    }

    // ===== 数据采集 =====

    private void CollectSnapshot()
    {
        MapSnapshot snap = MapSnapshot.Empty;
        snap.IsFullMapMode = _isFullMapOpen;

        // 玩家数据
        if (_playerTransform != null)
        {
            snap.PlayerWorldPosition = _playerTransform.position;
            snap.PlayerYaw = _playerTransform.eulerAngles.y;
        }

        // 相机数据
        if (_mapCamera != null)
        {
            snap.CameraWorldCenter = _mapCamera.transform.position;
            snap.CameraOrthoSize = _mapCamera.OrthoSize;
        }

        // UI 像素尺寸（从当前 VisualElement 的解析尺寸获取）
        if (panel != null)
        {
            Vector2 resolvedSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
            if (resolvedSize.x > 0f && resolvedSize.y > 0f)
            {
                if (_isFullMapOpen)
                    snap.FullMapPixelSize = resolvedSize;
                else
                    snap.MiniMapPixelSize = resolvedSize;
            }
        }

        // 标记列表
        snap.Markers = CollectMarkers();

        _currentSnapshot = snap;
    }

    /// <summary>
    /// 收集当前场景中的敌友标记数据。
    /// 当前为占位实现；后续接入 TankRegistry 或 MissionRegistry 后替换。
    /// </summary>
    private List<MapMarkerData> CollectMarkers()
    {
        _alphaMarkers.Clear();
        // TODO: 从 TankRegistry / MissionRegistry 获取 敌方/友方 坐标
        // foreach (var tank in TankRegistry.Instance.AllTanks)
        // {
        //     _alphaMarkers.Add(new MapMarkerData
        //     {
        //         WorldPosition = tank.transform.position,
        //         Color = tank.IsAlly ? _config.AllyColor : _config.EnemyColor,
        //         Radius = _config.MarkerRadius,
        //         Label = _config.ShowLabels ? tank.DisplayName : null
        //     });
        // }
        return _alphaMarkers;
    }

    // ===== Painter2D 绘制入口 =====

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (!_dirty) return;
        _dirty = false;

        if (_config == null) return;

        Painter2D painter = mgc.painter2D;
        MapSnapshot snap = _currentSnapshot;

        // 1. 绘制背景（纯色 + 相机 RT 快照纹理，如果有）
        DrawBackground(painter, snap);

        // 2. 绘制网格
        bool showGrid = _isFullMapOpen ? _config.ShowGridOnFullMap : _config.ShowGridOnMiniMap;
        if (showGrid)
        {
            float spacing = _isFullMapOpen ? _config.FullMapGridSpacing : _config.MiniMapGridSpacing;
            Color gridColor = _isFullMapOpen ? _config.FullMapGridColor : _config.MiniMapGridColor;
            DrawGrid(painter, snap, spacing, gridColor);
        }

        // 3. 绘制敌友标记
        DrawMarkers(painter, snap);

        // 4. 绘制玩家标记 + 朝向线
        DrawPlayerMarker(painter, snap);
    }

    // ===== 绘制方法 =====

    private void DrawBackground(Painter2D painter, MapSnapshot snap)
    {
        Vector2 size = snap.CurrentPixelSize;
        Color bgColor = _isFullMapOpen
            ? new Color(0.08f, 0.08f, 0.12f, 0.95f)
            : new Color(0.05f, 0.05f, 0.08f, 0.8f);

        painter.fillColor = bgColor;
        painter.BeginPath();
        painter.MoveTo(Vector2.zero);
        painter.LineTo(new Vector2(size.x, 0f));
        painter.LineTo(size);
        painter.LineTo(new Vector2(0f, size.y));
        painter.ClosePath();
        painter.Fill();

        // 边框
        painter.strokeColor = new Color(1f, 1f, 1f, 0.25f);
        painter.lineWidth = 1f;
        painter.BeginPath();
        DrawRect(painter, Vector2.zero, size);
        painter.Stroke();
    }

    private void DrawGrid(Painter2D painter, MapSnapshot snap, float worldSpacing, Color color)
    {
        if (worldSpacing <= 0f || snap.CameraOrthoSize <= 0f) return;

        Vector2 pixelSize = snap.CurrentPixelSize;
        if (pixelSize.x <= 0f || pixelSize.y <= 0f) return;

        // 将世界间距映射为像素间距
        float pixelsPerWorldUnit = pixelSize.y / (snap.CameraOrthoSize * 2f);
        float pixelSpacing = worldSpacing * pixelsPerWorldUnit;

        // 将相机中心映射到 UI 像素中心
        Vector2 cameraPixelCenter = WorldToPixel(snap.CameraWorldCenter, snap);
        float gridOriginX = cameraPixelCenter.x % pixelSpacing;
        float gridOriginY = cameraPixelCenter.y % pixelSpacing;

        painter.strokeColor = color;
        painter.lineWidth = 0.5f;

        // 绘制垂直线
        for (float x = gridOriginX; x < pixelSize.x; x += pixelSpacing)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x, pixelSize.y));
            painter.Stroke();
        }

        // 绘制水平线
        for (float y = gridOriginY; y < pixelSize.y; y += pixelSpacing)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, y));
            painter.LineTo(new Vector2(pixelSize.x, y));
            painter.Stroke();
        }
    }

    private void DrawPlayerMarker(Painter2D painter, MapSnapshot snap)
    {
        if (_playerTransform == null) return;

        Vector2 playerPixelPos = WorldToPixel(snap.PlayerWorldPosition, snap);
        float markerRadius = _isFullMapOpen ? _config.FullMapPlayerMarkerRadius : _config.MiniMapPlayerMarkerRadius;
        Color playerColor = _isFullMapOpen ? _config.FullMapPlayerColor : _config.MiniMapPlayerColor;

        // 填充圆
        painter.fillColor = playerColor;
        painter.BeginPath();
        painter.Arc(playerPixelPos, markerRadius, 0f, 360f);
        painter.Fill();

        // 朝向线
        float headingLength = markerRadius * 2f;
        float headingRad = snap.PlayerYaw * Mathf.Deg2Rad;
        Vector2 headingEnd = playerPixelPos + new Vector2(
            Mathf.Sin(headingRad) * headingLength,
            Mathf.Cos(headingRad) * headingLength
        );

        painter.strokeColor = playerColor;
        painter.lineWidth = 1f;
        painter.BeginPath();
        painter.MoveTo(playerPixelPos);
        painter.LineTo(headingEnd);
        painter.Stroke();
    }

    private void DrawMarkers(Painter2D painter, MapSnapshot snap)
    {
        if (snap.Markers == null || snap.Markers.Count == 0) return;

        for (int i = 0; i < snap.Markers.Count; i++)
        {
            MapMarkerData marker = snap.Markers[i];
            Vector2 pixelPos = WorldToPixel(marker.WorldPosition, snap);
            float radius = Mathf.Max(2f, marker.Radius * 0.5f);

            // 填充圆
            painter.fillColor = marker.Color;
            painter.BeginPath();
            painter.Arc(pixelPos, radius, 0f, 360f);
            painter.Fill();
        }
    }

    // ===== 坐标映射 =====

    /// <summary>
    /// 将世界坐标 (XZ 平面) 映射到当前地图 UI 像素坐标。
    /// Y 轴：世界 Z → 像素 Y（0 在顶部，height 在底部）。
    /// X 轴：世界 X → 像素 X（0 在左侧）。
    /// </summary>
    private Vector2 WorldToPixel(Vector3 worldPos, MapSnapshot snap)
    {
        Vector2 pixelSize = snap.CurrentPixelSize;
        if (pixelSize.x <= 0f || pixelSize.y <= 0f) return Vector2.zero;

        float orthoHalfHeight = Mathf.Max(0.001f, snap.CameraOrthoSize);
        float pixelsPerWorldUnit = pixelSize.y / (orthoHalfHeight * 2f);

        float relativeX = worldPos.x - snap.CameraWorldCenter.x;
        float relativeZ = worldPos.z - snap.CameraWorldCenter.z;

        float pixelX = pixelSize.x * 0.5f + relativeX * pixelsPerWorldUnit;
        float pixelY = pixelSize.y * 0.5f - relativeZ * pixelsPerWorldUnit;

        return new Vector2(pixelX, pixelY);
    }

    // ===== 辅助 =====

    private void DrawRect(Painter2D painter, Vector2 topLeft, Vector2 size)
    {
        Vector2 bottomRight = topLeft + size;
        painter.MoveTo(topLeft);
        painter.LineTo(new Vector2(bottomRight.x, topLeft.y));
        painter.LineTo(bottomRight);
        painter.LineTo(new Vector2(topLeft.x, bottomRight.y));
        painter.ClosePath();
    }

    private void ReleaseResources()
    {
        // 预留：后续如果持有 Texture2D 等资源在此释放
    }
}
