using UnityEngine;
using UnityEngine.UIElements;

public class FcsHudPainter : VisualElement
{
    private FCSSnapshot _state;
    private NewAimConfigData _config;
    private Vector2 _currentVisualPos;
    private Vector2 _smoothVelocity;
    private bool _isAimMode;
    private Texture2D _vignetteTexture;
    private Texture2D _transparentTexture;
    private int _cachedVignetteWidth;
    private int _cachedVignetteHeight;
    private float _cachedCornerShadowLongSideRange;
    private float _cachedCornerShadowShortSideRange;
    private float _cachedCenterViewLongSideSize;
    private float _cachedCenterViewShortSideSize;
    private Color _cachedShadowColor;
    private Texture2D _appliedBackgroundTexture;

    public FcsHudPainter()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<DetachFromPanelEvent>(_ => ReleaseVignetteTextures());
        style.position = Position.Absolute;
        style.left = 0f;
        style.top = 0f;
        style.right = 0f;
        style.bottom = 0f;
        style.width = Length.Percent(100);
        style.height = Length.Percent(100);
        pickingMode = PickingMode.Ignore;
    }

    public void Refresh(Vector2 targetPos, FCSSnapshot state, NewAimConfigData config, bool isAimMode, float smoothTime)
    {
        _state = state;
        _config = config;
        _isAimMode = isAimMode;

        if (_config == null)
        {
            return;
        }

        _currentVisualPos = isAimMode
            ? targetPos
            : Vector2.SmoothDamp(_currentVisualPos, targetPos, ref _smoothVelocity, smoothTime);

        UpdateVignetteMaskBackground();

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_config == null)
        {
            return;
        }

        float zoomScale = GetZoomHudScale();
        Painter2D painter = mgc.painter2D;

        painter.strokeColor = _config.HudThemeColor;
        painter.lineWidth = Mathf.Max(1f, _config.LineThickness * zoomScale);

        if (_config.HudLayout == NewAimConfigData.HudPreset.Custom && _config.HudElements != null && _config.HudElements.Count > 0)
        {
            DrawCustomHud(painter, zoomScale);
            return;
        }

        if (_config.HudLayout == NewAimConfigData.HudPreset.TacticalRing)
        {
            DrawTpsRing(painter, _currentVisualPos, zoomScale);
            return;
        }

        DrawModernHud(painter, _currentVisualPos, zoomScale);
    }

    private float GetZoomHudScale()
    {
        if (_config == null)
        {
            return 1f;
        }

        float currentFov = _state.CurrentFov > 0.01f ? _state.CurrentFov : _config.HudFovReference;
        float referenceFov = _config.HudFovReference > 0.01f ? _config.HudFovReference : 60f;
        float exponent = Mathf.Max(0f, _config.HudFovScaleExponent);
        float scale = _config.HudScale * Mathf.Pow(referenceFov / currentFov, exponent);
        return Mathf.Clamp(scale, Mathf.Max(0.1f, _config.HudFovScaleMin), Mathf.Max(_config.HudFovScaleMin, _config.HudFovScaleMax));
    }

    private void DrawModernHud(Painter2D painter, Vector2 center, float zoomScale)
    {
        DrawCrosshair(painter, center, null, zoomScale);
        DrawGraticules(painter, center, zoomScale);
        DrawCenterDot(painter, center, null, zoomScale);
        DrawCornerBrackets(painter, center, zoomScale, null);
    }

    private void DrawCustomHud(Painter2D painter, float zoomScale)
    {
        for (int index = 0; index < _config.HudElements.Count; index++)
        {
            NewAimConfigData.HudElementDefinition element = _config.HudElements[index];
            if (element == null || !element.Enabled)
            {
                continue;
            }

            painter.strokeColor = element.Color.a > 0f ? element.Color : _config.HudThemeColor;
            painter.lineWidth = Mathf.Max(1f, element.Thickness * zoomScale);

            Vector2 anchorPoint = ResolveAnchorPoint(element.Anchor);
            Vector2 elementCenter = anchorPoint + element.Offset * zoomScale;

            switch (element.ElementType)
            {
                case NewAimConfigData.HudElementType.Crosshair:
                    DrawCrosshair(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.Ring:
                    DrawRing(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.Graticule:
                    DrawGraticules(painter, elementCenter, zoomScale, element);
                    break;
                case NewAimConfigData.HudElementType.CornerBracket:
                    DrawCornerBracketSet(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.CenterDot:
                    DrawCenterDot(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.HorizontalScale:
                    DrawHorizontalScale(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.VerticalScale:
                    DrawVerticalScale(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.RectangleFrame:
                    DrawRectangleFrame(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.ReadoutBox:
                    DrawReadoutBox(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.CornerTick:
                    DrawCornerTick(painter, elementCenter, element, zoomScale);
                    break;
                case NewAimConfigData.HudElementType.TextSlot:
                    DrawTextSlotPlaceholder(painter, elementCenter, element, zoomScale);
                    break;
            }
        }
    }

    private Vector2 ResolveAnchorPoint(NewAimConfigData.HudAnchor anchor)
    {
        float width = _state.ScreenWidth;
        float height = _state.ScreenHeight;

        switch (anchor)
        {
            case NewAimConfigData.HudAnchor.TopLeft:
                return new Vector2(0f, 0f);
            case NewAimConfigData.HudAnchor.TopRight:
                return new Vector2(width, 0f);
            case NewAimConfigData.HudAnchor.BottomLeft:
                return new Vector2(0f, height);
            case NewAimConfigData.HudAnchor.BottomRight:
                return new Vector2(width, height);
            case NewAimConfigData.HudAnchor.TopCenter:
                return new Vector2(width * 0.5f, 0f);
            case NewAimConfigData.HudAnchor.BottomCenter:
                return new Vector2(width * 0.5f, height);
            case NewAimConfigData.HudAnchor.LeftCenter:
                return new Vector2(0f, height * 0.5f);
            case NewAimConfigData.HudAnchor.RightCenter:
                return new Vector2(width, height * 0.5f);
            default:
                return new Vector2(width * 0.5f, height * 0.5f);
        }
    }

    private void UpdateVignetteMaskBackground()
    {
        if (_config == null || !_config.EnableVignetteMask)
        {
            SetBackgroundTexture(GetTransparentTexture());
            return;
        }

        int textureWidth = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(1f, _state.ScreenWidth) * 0.25f), 128, 512);
        int textureHeight = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(1f, _state.ScreenHeight) * 0.25f), 128, 512);
        float cornerShadowLongSideRange = Mathf.Clamp01(_config.CornerShadowLongSideRange);
        float cornerShadowShortSideRange = Mathf.Clamp01(_config.CornerShadowShortSideRange);
        float centerViewLongSideSize = Mathf.Clamp01(_config.CenterViewLongSideSize);
        float centerViewShortSideSize = Mathf.Clamp01(_config.CenterViewShortSideSize);
        Color shadowColor = _config.ShadowColor;
        float shadowAlpha = Mathf.Clamp01(shadowColor.a);

        if (shadowAlpha <= 0f)
        {
            SetBackgroundTexture(GetTransparentTexture());
            return;
        }

        bool needsRebuild = _vignetteTexture == null
            || _cachedVignetteWidth != textureWidth
            || _cachedVignetteHeight != textureHeight
            || !Mathf.Approximately(_cachedCornerShadowLongSideRange, cornerShadowLongSideRange)
            || !Mathf.Approximately(_cachedCornerShadowShortSideRange, cornerShadowShortSideRange)
            || !Mathf.Approximately(_cachedCenterViewLongSideSize, centerViewLongSideSize)
            || !Mathf.Approximately(_cachedCenterViewShortSideSize, centerViewShortSideSize)
            || _cachedShadowColor != shadowColor;

        if (!needsRebuild)
        {
            SetBackgroundTexture(_vignetteTexture);
            return;
        }

        ReleaseVignetteTexture();

        _vignetteTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, true);
        _vignetteTexture.wrapMode = TextureWrapMode.Clamp;
        _vignetteTexture.filterMode = FilterMode.Bilinear;
        _vignetteTexture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[textureWidth * textureHeight];
        bool isWidthLonger = textureWidth >= textureHeight;
        float centerHalfLong = Mathf.Lerp(0.26f, 0.78f, centerViewLongSideSize);
        float centerHalfShort = Mathf.Lerp(0.16f, 0.62f, centerViewShortSideSize);
        float fadeLong = Mathf.Lerp(0.04f, 0.34f, cornerShadowLongSideRange);
        float fadeShort = Mathf.Lerp(0.04f, 0.34f, cornerShadowShortSideRange);
        float roundRadius = Mathf.Min(centerHalfLong, centerHalfShort) * Mathf.Lerp(0.18f, 0.42f, (cornerShadowLongSideRange + cornerShadowShortSideRange) * 0.5f);
        float sideShadowWeight = 0.2f;

        Color32 baseColor = shadowColor;
        for (int y = 0; y < textureHeight; y++)
        {
            float uvY = (y + 0.5f) / textureHeight;
            for (int x = 0; x < textureWidth; x++)
            {
                float uvX = (x + 0.5f) / textureWidth;
                float centeredX = Mathf.Abs(uvX - 0.5f) * 2f;
                float centeredY = Mathf.Abs(uvY - 0.5f) * 2f;

                float longAxis = isWidthLonger ? centeredX : centeredY;
                float shortAxis = isWidthLonger ? centeredY : centeredX;

                float coreLong = Mathf.Max(0.0001f, centerHalfLong - roundRadius);
                float coreShort = Mathf.Max(0.0001f, centerHalfShort - roundRadius);
                float overflowLong = Mathf.Max(longAxis - coreLong, 0f);
                float overflowShort = Mathf.Max(shortAxis - coreShort, 0f);
                float normalizedLong = overflowLong / Mathf.Max(fadeLong, 0.0001f);
                float normalizedShort = overflowShort / Mathf.Max(fadeShort, 0.0001f);
                float arcDistance = Mathf.Max(
                    0f,
                    Mathf.Sqrt(normalizedLong * normalizedLong + normalizedShort * normalizedShort) - Mathf.Max(roundRadius / Mathf.Max(Mathf.Min(fadeLong, fadeShort), 0.0001f), 0f));
                float cornerShadow = Mathf.SmoothStep(0f, 1f, arcDistance);
                float sideShadow = Mathf.SmoothStep(0f, 1f, Mathf.Max(normalizedLong, normalizedShort)) * sideShadowWeight;
                float alpha = shadowAlpha * Mathf.Clamp01(Mathf.Max(cornerShadow, sideShadow));
                pixels[y * textureWidth + x] = new Color32(baseColor.r, baseColor.g, baseColor.b, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        _vignetteTexture.SetPixels32(pixels);
        _vignetteTexture.Apply(false, false);

        _cachedVignetteWidth = textureWidth;
        _cachedVignetteHeight = textureHeight;
        _cachedCornerShadowLongSideRange = cornerShadowLongSideRange;
        _cachedCornerShadowShortSideRange = cornerShadowShortSideRange;
        _cachedCenterViewLongSideSize = centerViewLongSideSize;
        _cachedCenterViewShortSideSize = centerViewShortSideSize;
        _cachedShadowColor = shadowColor;

        SetBackgroundTexture(_vignetteTexture);
    }

    private void SetBackgroundTexture(Texture2D texture)
    {
        if (texture == null || _appliedBackgroundTexture == texture)
        {
            return;
        }

        style.backgroundImage = new StyleBackground(texture);
        _appliedBackgroundTexture = texture;
    }

    private Texture2D GetTransparentTexture()
    {
        if (_transparentTexture == null)
        {
            _transparentTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _transparentTexture.hideFlags = HideFlags.HideAndDontSave;
            _transparentTexture.SetPixel(0, 0, Color.clear);
            _transparentTexture.Apply(false, false);
            _transparentTexture.wrapMode = TextureWrapMode.Clamp;
            _transparentTexture.filterMode = FilterMode.Bilinear;
        }

        return _transparentTexture;
    }

    private void ReleaseVignetteTexture()
    {
        if (_vignetteTexture != null)
        {
            if (_appliedBackgroundTexture == _vignetteTexture)
            {
                _appliedBackgroundTexture = null;
            }

            Object.DestroyImmediate(_vignetteTexture);
            _vignetteTexture = null;
        }
    }

    private void ReleaseVignetteTextures()
    {
        ReleaseVignetteTexture();

        if (_transparentTexture != null)
        {
            Object.DestroyImmediate(_transparentTexture);
            _transparentTexture = null;
        }

        _appliedBackgroundTexture = null;
    }

    private void DrawGraticules(Painter2D painter, Vector2 center, float zoomScale, NewAimConfigData.HudElementDefinition element = null)
    {
        float vFovRad = _state.CurrentFov * Mathf.Deg2Rad;
        float pixelPerRad = (_state.ScreenHeight / vFovRad) * zoomScale;
        float spacingMil = element != null
            ? Mathf.Max(0.1f, element.RepeatSpacing)
            : Mathf.Max(0.1f, _config.GraticuleSpacingMil);
        float halfWidth = element != null
            ? Mathf.Max(2f, element.Size.x * 0.5f * zoomScale)
            : Mathf.Max(2f, _config.GraticuleLineHalfWidth * zoomScale);
        int repeatCount = element != null ? Mathf.Max(1, element.RepeatCount) : Mathf.Max(1, _config.GraticuleCount);
        float startOffsetMil = element != null ? 0f : Mathf.Max(0f, _config.GraticuleStartOffsetMil);

        for (int i = 1; i <= repeatCount; i++)
        {
            float angleOffset = (startOffsetMil + i * spacingMil) * 0.001f;
            float yOffset = angleOffset * pixelPerRad;

            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - halfWidth, center.y + yOffset));
            painter.LineTo(new Vector2(center.x + halfWidth, center.y + yOffset));
            painter.Stroke();
        }
    }

    private void DrawCrosshair(Painter2D painter, Vector2 pos)
    {
        DrawCrosshair(painter, pos, null, 1f);
    }

    private void DrawCrosshair(Painter2D painter, Vector2 pos, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float scale = Mathf.Max(0.1f, (element != null ? element.Size.x / 24f : _config.HudScale) * zoomScale);
        float halfLength = Mathf.Max(4f, (element != null ? element.Size.x : _config.CrosshairLength) * scale * 0.5f);
        float halfGap = Mathf.Clamp((element != null ? element.Size.y : _config.CrosshairGap) * scale * 0.5f, 0f, halfLength * 0.8f);

        painter.BeginPath();
        painter.MoveTo(pos + Vector2.up * halfLength); painter.LineTo(pos + Vector2.up * halfGap);
        painter.MoveTo(pos - Vector2.up * halfGap); painter.LineTo(pos - Vector2.up * halfLength);
        painter.MoveTo(pos + Vector2.right * halfLength); painter.LineTo(pos + Vector2.right * halfGap);
        painter.MoveTo(pos - Vector2.right * halfGap); painter.LineTo(pos - Vector2.right * halfLength);
        painter.Stroke();
    }

    private void DrawTpsRing(Painter2D painter, Vector2 center, float zoomScale)
    {
        float scale = Mathf.Max(0.1f, _config.HudScale * zoomScale);
        painter.lineWidth = Mathf.Max(1f, _config.TpsRingThickness * scale);
        painter.BeginPath();
        painter.Arc(center, Mathf.Max(4f, _config.TpsRingRadius * scale), 0f, 360f);
        painter.Stroke();
    }

    private void DrawRing(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        painter.lineWidth = Mathf.Max(1f, element.Thickness * zoomScale);
        painter.BeginPath();
        painter.Arc(center, Mathf.Max(4f, element.Radius * zoomScale), 0f, 360f);
        painter.Stroke();
    }

    private void DrawCenterDot(Painter2D painter, Vector2 center)
    {
        DrawCenterDot(painter, center, null, 1f);
    }

    private void DrawCenterDot(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float radius = Mathf.Max(1.5f, (element != null ? element.Radius : 2.5f) * zoomScale);
        painter.BeginPath();
        painter.Arc(center, radius, 0f, 360f);
        painter.Stroke();
    }

    private void DrawCornerBrackets(Painter2D painter, Vector2 center, float zoomScale, NewAimConfigData.HudElementDefinition element)
    {
        DrawCornerBracketSet(painter, center, element, zoomScale);
    }

    private void DrawCornerBracketSet(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float halfSize = Mathf.Max(8f, (element != null ? element.Size.x * 0.5f : _config.CrosshairLength * 0.75f) * zoomScale);
        float bracket = Mathf.Max(6f, (element != null ? element.Size.y * 0.4f : halfSize * 0.4f) * zoomScale);

        DrawCornerBracketAt(painter, center + new Vector2(-halfSize, -halfSize), bracket, false, false);
        DrawCornerBracketAt(painter, center + new Vector2(halfSize, -halfSize), bracket, true, false);
        DrawCornerBracketAt(painter, center + new Vector2(-halfSize, halfSize), bracket, false, true);
        DrawCornerBracketAt(painter, center + new Vector2(halfSize, halfSize), bracket, true, true);
    }

    private void DrawCornerBracketAt(Painter2D painter, Vector2 corner, float bracketSize, bool rightSide, bool bottomSide)
    {
        float horizontal = rightSide ? -bracketSize : bracketSize;
        float vertical = bottomSide ? -bracketSize : bracketSize;

        painter.BeginPath();
        painter.MoveTo(corner);
        painter.LineTo(corner + new Vector2(horizontal, 0f));
        painter.MoveTo(corner);
        painter.LineTo(corner + new Vector2(0f, vertical));
        painter.Stroke();
    }

    private void DrawHorizontalScale(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float spacing = Mathf.Max(4f, element.RepeatSpacing * zoomScale);
        int repeatCount = Mathf.Max(1, element.RepeatCount);
        float totalLength = Mathf.Max(0f, (element.ScaleTotalLength > 0f ? element.ScaleTotalLength : element.Size.y) * zoomScale);
        float decayPerStep = Mathf.Max(0f, element.ScaleDecayPerStep * zoomScale);
        float halfLength = totalLength * 0.5f;

        for (int i = -repeatCount; i <= repeatCount; i++)
        {
            float x = center.x + i * spacing;
            float lineHeight = halfLength - Mathf.Abs(i) * decayPerStep;
            if (lineHeight <= 0f)
            {
                continue;
            }

            painter.BeginPath();
            painter.MoveTo(new Vector2(x, center.y - lineHeight));
            painter.LineTo(new Vector2(x, center.y + lineHeight));
            painter.Stroke();
        }
    }

    private void DrawVerticalScale(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float spacing = Mathf.Max(4f, element.RepeatSpacing * zoomScale);
        int repeatCount = Mathf.Max(1, element.RepeatCount);
        float totalLength = Mathf.Max(0f, (element.ScaleTotalLength > 0f ? element.ScaleTotalLength : element.Size.x) * zoomScale);
        float decayPerStep = Mathf.Max(0f, element.ScaleDecayPerStep * zoomScale);
        float halfLength = totalLength * 0.5f;

        for (int i = -repeatCount; i <= repeatCount; i++)
        {
            float y = center.y + i * spacing;
            float lineWidth = halfLength - Mathf.Abs(i) * decayPerStep;
            if (lineWidth <= 0f)
            {
                continue;
            }

            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - lineWidth, y));
            painter.LineTo(new Vector2(center.x + lineWidth, y));
            painter.Stroke();
        }
    }

    private void DrawRectangleFrame(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        Vector2 halfSize = new Vector2(Mathf.Max(4f, element.Size.x * 0.5f * zoomScale), Mathf.Max(4f, element.Size.y * 0.5f * zoomScale));
        DrawFramePath(painter, center, halfSize);
    }

    private void DrawReadoutBox(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        Vector2 halfSize = new Vector2(Mathf.Max(4f, element.Size.x * 0.5f * zoomScale), Mathf.Max(4f, element.Size.y * 0.5f * zoomScale));

        if (element.Filled)
        {
            DrawFramePath(painter, center, halfSize);
        }

        DrawFramePath(painter, center, halfSize);
        DrawTextSlotPlaceholder(painter, center, element, zoomScale);
    }

    private void DrawCornerTick(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float size = Mathf.Max(4f, element.Size.x * 0.5f * zoomScale);
        painter.BeginPath();
        painter.MoveTo(center);
        painter.LineTo(center + new Vector2(size, 0f));
        painter.MoveTo(center);
        painter.LineTo(center + new Vector2(0f, size));
        painter.Stroke();
    }

    private void DrawTextSlotPlaceholder(Painter2D painter, Vector2 center, NewAimConfigData.HudElementDefinition element, float zoomScale)
    {
        float width = Mathf.Max(8f, element.Size.x * zoomScale);
        float height = Mathf.Max(8f, element.Size.y * zoomScale);
        Vector2 halfSize = new Vector2(width * 0.5f, height * 0.5f);
        DrawFramePath(painter, center, halfSize);

        float lineY = center.y - height * 0.15f;
        painter.BeginPath();
        painter.MoveTo(new Vector2(center.x - width * 0.32f, lineY));
        painter.LineTo(new Vector2(center.x + width * 0.32f, lineY));
        painter.Stroke();
    }

    private void DrawFramePath(Painter2D painter, Vector2 center, Vector2 halfSize)
    {
        Vector2 topLeft = center + new Vector2(-halfSize.x, -halfSize.y);
        Vector2 topRight = center + new Vector2(halfSize.x, -halfSize.y);
        Vector2 bottomRight = center + new Vector2(halfSize.x, halfSize.y);
        Vector2 bottomLeft = center + new Vector2(-halfSize.x, halfSize.y);

        painter.BeginPath();
        painter.MoveTo(topLeft);
        painter.LineTo(topRight);
        painter.LineTo(bottomRight);
        painter.LineTo(bottomLeft);
        painter.LineTo(topLeft);
        painter.Stroke();
    }
}
