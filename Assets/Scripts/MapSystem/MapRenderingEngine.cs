using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MapRenderingEngine : VisualElement
{
    private MapConfigSO _config;
    private MapCameraPosition _mapCamera;
    private Transform _playerTransform;

    private float _updateInterval;
    private float _lastTickTime;
    private MapSnapshot _currentSnapshot;
    private bool _dirty;
    private bool _isFullMapOpen;
    private bool _isVisible;

    private readonly List<MapMarkerData> _alphaMarkers = new List<MapMarkerData>();

    private RenderTexture _renderTexture;
    private Vector2Int _renderTextureSize;

    private const float MINI_MAP_SIZE = 300f;
    private const float MINI_MAP_RIGHT_MARGIN = 10f;
    private const float MINI_MAP_BOTTOM_MARGIN = 60f;
    private const int DEFAULT_MINI_MAP_RT_SIZE = 512;
    private const int DEFAULT_FULL_MAP_RT_WIDTH = 1920;
    private const int DEFAULT_FULL_MAP_RT_HEIGHT = 1080;

    public bool IsFullMapOpen => _isFullMapOpen;
    public bool IsVisible => _isVisible;

    public MapRenderingEngine(MapConfigSO config)
    {
        _config = config;
        _updateInterval = config != null ? config.UpdateInterval : 0.1f;
        _currentSnapshot = MapSnapshot.Empty;

        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<DetachFromPanelEvent>(_ => ReleaseResources());

        style.position = Position.Absolute;
        style.overflow = Overflow.Hidden;
        style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        pickingMode = PickingMode.Ignore;

        ApplyMiniMapLayout();
    }

    public void SetCamera(MapCameraPosition camera)
    {
        _mapCamera = camera;

        if (_renderTexture != null && _mapCamera != null)
        {
            _mapCamera.SetTargetTexture(_renderTexture);
        }
    }

    public void SetPlayer(Transform player) => _playerTransform = player;

    public void SetConfig(MapConfigSO config)
    {
        _config = config;
        _updateInterval = config != null ? config.UpdateInterval : 0.1f;
        _dirty = true;
    }

    public void OpenFullMap()
    {
        if (_isFullMapOpen)
        {
            return;
        }

        _isFullMapOpen = true;
        ApplyFullMapLayout();
        _dirty = true;
        ForceTick();
        MarkDirtyRepaint();
    }

    public void CloseFullMap()
    {
        if (!_isFullMapOpen)
        {
            return;
        }

        _isFullMapOpen = false;
        ApplyMiniMapLayout();
        _dirty = true;
        ForceTick();
        MarkDirtyRepaint();
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (visible)
        {
            ForceTick();
            MarkDirtyRepaint();
        }
    }

    public void Tick()
    {
        if (!_isVisible)
        {
            return;
        }

        UpdateRenderTextureIfNeeded();

        float now = Time.time;
        if (now - _lastTickTime < _updateInterval)
        {
            return;
        }

        _lastTickTime = now;
        CollectSnapshot();
        _dirty = true;

        if (panel != null)
        {
            MarkDirtyRepaint();
        }
    }

    private void ForceTick()
    {
        _lastTickTime = 0f;
        UpdateRenderTextureIfNeeded();
        CollectSnapshot();
        _dirty = true;
    }

    private void UpdateRenderTextureIfNeeded()
    {
        Vector2Int desiredSize = ResolveRenderTextureSize();
        if (desiredSize.x <= 0 || desiredSize.y <= 0)
        {
            return;
        }

        if (_renderTexture != null && _renderTextureSize == desiredSize)
        {
            if (_mapCamera != null && _mapCamera.MapCamera != null && _mapCamera.MapCamera.targetTexture != _renderTexture)
            {
                _mapCamera.SetTargetTexture(_renderTexture);
            }

            return;
        }

        ReleaseRenderTextureOnly();

        _renderTexture = new RenderTexture(desiredSize.x, desiredSize.y, 24, RenderTextureFormat.ARGB32)
        {
            name = $"MapRT_{desiredSize.x}x{desiredSize.y}",
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
        _renderTextureSize = desiredSize;

        if (_mapCamera != null)
        {
            _mapCamera.SetTargetTexture(_renderTexture);
        }

        style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_renderTexture));
    }

    private Vector2Int ResolveRenderTextureSize()
    {
        int width = Mathf.RoundToInt(resolvedStyle.width);
        int height = Mathf.RoundToInt(resolvedStyle.height);

        if (width <= 0 || height <= 0)
        {
            if (_isFullMapOpen)
            {
                width = Mathf.Max(Screen.width, DEFAULT_FULL_MAP_RT_WIDTH);
                height = Mathf.Max(Screen.height, DEFAULT_FULL_MAP_RT_HEIGHT);
            }
            else
            {
                width = DEFAULT_MINI_MAP_RT_SIZE;
                height = DEFAULT_MINI_MAP_RT_SIZE;
            }
        }

        return new Vector2Int(Mathf.Max(64, width), Mathf.Max(64, height));
    }

    private void CollectSnapshot()
    {
        MapSnapshot snap = MapSnapshot.Empty;
        snap.IsFullMapMode = _isFullMapOpen;

        if (_playerTransform != null)
        {
            snap.PlayerWorldPosition = _playerTransform.position;
            snap.PlayerYaw = _playerTransform.eulerAngles.y;
        }

        if (_mapCamera != null)
        {
            snap.CameraWorldCenter = _playerTransform != null
                ? _playerTransform.position
                : _mapCamera.transform.position;
            snap.CameraOrthoSize = _mapCamera.OrthoSize;
        }

        if (panel != null)
        {
            Vector2 resolvedSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
            if (resolvedSize.x > 0f && resolvedSize.y > 0f)
            {
                if (_isFullMapOpen)
                {
                    snap.FullMapPixelSize = resolvedSize;
                }
                else
                {
                    snap.MiniMapPixelSize = resolvedSize;
                }
            }
        }

        snap.Markers = CollectMarkers();
        _currentSnapshot = snap;
    }

    private List<MapMarkerData> CollectMarkers()
    {
        _alphaMarkers.Clear();
        return _alphaMarkers;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (!_dirty || _config == null)
        {
            return;
        }

        _dirty = false;

        Painter2D painter = mgc.painter2D;
        MapSnapshot snap = _currentSnapshot;

        DrawBackgroundOverlay(painter, snap);

        bool showGrid = _isFullMapOpen ? _config.ShowGridOnFullMap : _config.ShowGridOnMiniMap;
        if (showGrid)
        {
            float spacing = _isFullMapOpen ? _config.FullMapGridSpacing : _config.MiniMapGridSpacing;
            Color gridColor = _isFullMapOpen ? _config.FullMapGridColor : _config.MiniMapGridColor;
            DrawGrid(painter, snap, spacing, gridColor);
        }

        DrawMarkers(painter, snap);
        DrawPlayerMarker(painter, snap);
    }

    private void DrawBackgroundOverlay(Painter2D painter, MapSnapshot snap)
    {
        Vector2 size = snap.CurrentPixelSize;
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        Color overlayColor = _isFullMapOpen
            ? new Color(0.02f, 0.03f, 0.05f, 0.18f)
            : new Color(0.02f, 0.03f, 0.05f, 0.10f);

        painter.fillColor = overlayColor;
        painter.BeginPath();
        painter.MoveTo(Vector2.zero);
        painter.LineTo(new Vector2(size.x, 0f));
        painter.LineTo(size);
        painter.LineTo(new Vector2(0f, size.y));
        painter.ClosePath();
        painter.Fill();

        painter.strokeColor = new Color(1f, 1f, 1f, 0.25f);
        painter.lineWidth = 1f;
        painter.BeginPath();
        DrawRect(painter, Vector2.zero, size);
        painter.Stroke();
    }

    private void DrawGrid(Painter2D painter, MapSnapshot snap, float worldSpacing, Color color)
    {
        if (worldSpacing <= 0f || snap.CameraOrthoSize <= 0f)
        {
            return;
        }

        Vector2 pixelSize = snap.CurrentPixelSize;
        if (pixelSize.x <= 0f || pixelSize.y <= 0f)
        {
            return;
        }

        float pixelsPerWorldUnit = pixelSize.y / (snap.CameraOrthoSize * 2f);
        float pixelSpacing = worldSpacing * pixelsPerWorldUnit;
        if (pixelSpacing <= 0.01f)
        {
            return;
        }

        Vector2 cameraPixelCenter = WorldToPixel(snap.CameraWorldCenter, snap);
        float gridOriginX = cameraPixelCenter.x % pixelSpacing;
        float gridOriginY = cameraPixelCenter.y % pixelSpacing;

        painter.strokeColor = color;
        painter.lineWidth = 0.5f;

        for (float x = gridOriginX; x < pixelSize.x; x += pixelSpacing)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x, pixelSize.y));
            painter.Stroke();
        }

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
        if (_playerTransform == null)
        {
            return;
        }

        Vector2 playerPixelPos = WorldToPixel(snap.PlayerWorldPosition, snap);
        float markerRadius = _isFullMapOpen ? _config.FullMapPlayerMarkerRadius : _config.MiniMapPlayerMarkerRadius;
        Color playerColor = _isFullMapOpen ? _config.FullMapPlayerColor : _config.MiniMapPlayerColor;

        painter.fillColor = playerColor;
        painter.BeginPath();
        painter.Arc(playerPixelPos, markerRadius, 0f, 360f);
        painter.Fill();

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
        if (snap.Markers == null || snap.Markers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < snap.Markers.Count; i++)
        {
            MapMarkerData marker = snap.Markers[i];
            Vector2 pixelPos = WorldToPixel(marker.WorldPosition, snap);
            float radius = Mathf.Max(2f, marker.Radius * 0.5f);

            painter.fillColor = marker.Color;
            painter.BeginPath();
            painter.Arc(pixelPos, radius, 0f, 360f);
            painter.Fill();
        }
    }

    private Vector2 WorldToPixel(Vector3 worldPos, MapSnapshot snap)
    {
        Vector2 pixelSize = snap.CurrentPixelSize;
        if (pixelSize.x <= 0f || pixelSize.y <= 0f)
        {
            return Vector2.zero;
        }

        float orthoHalfHeight = Mathf.Max(0.001f, snap.CameraOrthoSize);
        float pixelsPerWorldUnit = pixelSize.y / (orthoHalfHeight * 2f);

        float relativeX = worldPos.x - snap.CameraWorldCenter.x;
        float relativeZ = worldPos.z - snap.CameraWorldCenter.z;

        float pixelX = pixelSize.x * 0.5f + relativeX * pixelsPerWorldUnit;
        float pixelY = pixelSize.y * 0.5f - relativeZ * pixelsPerWorldUnit;

        return new Vector2(pixelX, pixelY);
    }

    private void DrawRect(Painter2D painter, Vector2 topLeft, Vector2 size)
    {
        Vector2 bottomRight = topLeft + size;
        painter.MoveTo(topLeft);
        painter.LineTo(new Vector2(bottomRight.x, topLeft.y));
        painter.LineTo(bottomRight);
        painter.LineTo(new Vector2(topLeft.x, bottomRight.y));
        painter.ClosePath();
    }

    private void ApplyFullMapLayout()
    {
        style.left = 0f;
        style.top = 0f;
        style.right = 0f;
        style.bottom = 0f;
        style.width = Length.Percent(100);
        style.height = Length.Percent(100);
    }

    private void ApplyMiniMapLayout()
    {
        style.right = MINI_MAP_RIGHT_MARGIN;
        style.bottom = MINI_MAP_BOTTOM_MARGIN;
        style.left = StyleKeyword.Auto;
        style.top = StyleKeyword.Auto;
        style.width = MINI_MAP_SIZE;
        style.height = MINI_MAP_SIZE;
    }

    private void ReleaseRenderTextureOnly()
    {
        if (_mapCamera != null)
        {
            _mapCamera.SetTargetTexture(null);
        }

        if (_renderTexture != null)
        {
            if (_renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }

            Object.Destroy(_renderTexture);
            _renderTexture = null;
        }

        _renderTextureSize = Vector2Int.zero;
    }

    private void ReleaseResources()
    {
        ReleaseRenderTextureOnly();
    }
}
