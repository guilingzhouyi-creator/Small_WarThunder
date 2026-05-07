using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 指示渲染器：负责将指示对象渲染到屏幕上。
/// 采用对象池预分配 RectTransform，支持屏幕边缘钳制箭头和屏幕内图标显示。
/// 通过 IndicatorManager 每帧 LateUpdate 驱动。
/// </summary>
public class IndicatorRenderer : MonoBehaviour, IIndicatorRenderer
{
    [Header("对象池配置")]
    [SerializeField] private int _poolCapacity = 20;

    [Header("屏幕边缘偏移 (像素)")]
    [SerializeField] private float _edgeMargin = 60f;

    [Header("距离文本样式")]
    [SerializeField] private float _distanceFontSize = 14f;

    private IndicatorCentralRegistry _registry;

    // 对象池
    private List<RectTransform> _arrowPool = new List<RectTransform>();
    private List<RectTransform> _iconPool = new List<RectTransform>();
    private List<TextMeshProUGUI> _distanceTextPool = new List<TextMeshProUGUI>();

    // 活跃使用中的元素索引映射
    private Dictionary<int, RectTransform> _activeArrows = new Dictionary<int, RectTransform>();
    private Dictionary<int, RectTransform> _activeIcons = new Dictionary<int, RectTransform>();
    private Dictionary<int, TextMeshProUGUI> _activeDistanceTexts = new Dictionary<int, TextMeshProUGUI>();

    // Canvas 组件引用
    private Canvas _canvas;
    private RectTransform _canvasRect;

    /// <summary>
    /// 设置配置引用
    /// </summary>
    public void SetRegistry(IndicatorCentralRegistry registry)
    {
        _registry = registry;
    }

    private void Awake()
    {
        InitializeCanvas();
        InitializePool();
    }

    private void OnDestroy()
    {
        ClearAllVisuals();
    }

    /// <summary>
    /// 初始化 Canvas 组件
    /// </summary>
    private void InitializeCanvas()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
        }

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        // 确保有 CanvasScaler 和 GraphicRaycaster (但不用于交互)
        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        _canvasRect = _canvas.GetComponent<RectTransform>();
    }

    /// <summary>
    /// 初始化对象池：预分配箭头和图标的 RectTransform
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < _poolCapacity; i++)
        {
            CreatePooledArrow(i);
            CreatePooledIcon(i);
            CreatePooledDistanceText(i);
        }
    }

    private void CreatePooledArrow(int index)
    {
        GameObject arrowGo = new GameObject($"IndicatorArrow_{index}");
        arrowGo.transform.SetParent(transform, false);

        RectTransform rt = arrowGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(24f, 24f);
        rt.anchoredPosition = Vector2.zero;

        Image img = arrowGo.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        arrowGo.SetActive(false);
        _arrowPool.Add(rt);
    }

    private void CreatePooledIcon(int index)
    {
        GameObject iconGo = new GameObject($"IndicatorIcon_{index}");
        iconGo.transform.SetParent(transform, false);

        RectTransform rt = iconGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(32f, 32f);
        rt.anchoredPosition = Vector2.zero;

        Image img = iconGo.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        iconGo.SetActive(false);
        _iconPool.Add(rt);
    }

    private void CreatePooledDistanceText(int index)
    {
        GameObject textGo = new GameObject($"IndicatorDistance_{index}");
        textGo.transform.SetParent(transform, false);

        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 30f);
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = _distanceFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        textGo.SetActive(false);
        _distanceTextPool.Add(text);
    }

    /// <summary>
    /// 渲染所有活跃指示到屏幕空间
    /// </summary>
    public void RenderIndicators(List<IndicatorObject> activeIndicators, Camera renderCamera)
    {
        if (activeIndicators == null || renderCamera == null || _registry == null)
        {
            return;
        }

        // 释放上一帧使用的池元素
        ReleaseAllActiveVisuals();

        int arrowIndex = 0;
        int iconIndex = 0;
        int textIndex = 0;

        // 按优先级排序 (值越小优先级越高)
        activeIndicators.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (var indicator in activeIndicators)
        {
            if (indicator == null || indicator.isExpired)
            {
                continue;
            }

            IndicatorProperties props = _registry.GetProperties(indicator.type);
            RenderSingleIndicator(indicator, props, renderCamera, ref arrowIndex, ref iconIndex, ref textIndex);
        }
    }

    /// <summary>
    /// 渲染单个指示到屏幕
    /// </summary>
    private void RenderSingleIndicator(IndicatorObject indicator, IndicatorProperties props, Camera renderCamera,
        ref int arrowIndex, ref int iconIndex, ref int textIndex)
    {
        Vector3 screenPoint = renderCamera.WorldToScreenPoint(indicator.worldPosition);

        bool isBehindCamera = screenPoint.z < 0f;
        bool isOnScreen = IsOnScreen(screenPoint) && !isBehindCamera;

        if (isOnScreen)
        {
            // 屏幕内：显示图标
            if (iconIndex < _iconPool.Count)
            {
                RectTransform iconRt = _iconPool[iconIndex];
                ApplyIconVisual(iconRt, props, screenPoint);
                _activeIcons[indicator.id] = iconRt;
                iconIndex++;

                // 距离文本
                if (props.showDistance && textIndex < _distanceTextPool.Count)
                {
                    TextMeshProUGUI distText = _distanceTextPool[textIndex];
                    ApplyDistanceText(distText, indicator, props, screenPoint, renderCamera);
                    _activeDistanceTexts[indicator.id] = distText;
                    textIndex++;
                }
            }
        }
        else
        {
            // 屏幕外：显示边缘钳制箭头
            if (arrowIndex < _arrowPool.Count)
            {
                RectTransform arrowRt = _arrowPool[arrowIndex];
                ApplyArrowVisual(arrowRt, props, screenPoint, isBehindCamera);
                _activeArrows[indicator.id] = arrowRt;
                arrowIndex++;

                // 距离文本
                if (props.showDistance && textIndex < _distanceTextPool.Count)
                {
                    TextMeshProUGUI distText = _distanceTextPool[textIndex];
                    Vector3 clampedScreenPoint = ClampToScreenEdge(screenPoint, isBehindCamera);
                    ApplyDistanceTextAtPosition(distText, indicator, props, clampedScreenPoint, renderCamera);
                    _activeDistanceTexts[indicator.id] = distText;
                    textIndex++;
                }
            }
        }
    }

    /// <summary>
    /// 配置图标在屏幕内的视觉效果
    /// </summary>
    private void ApplyIconVisual(RectTransform rt, IndicatorProperties props, Vector3 screenPoint)
    {
        rt.gameObject.SetActive(true);

        // 位置
        Vector2 anchoredPos = ScreenToCanvasPosition(screenPoint);
        rt.anchoredPosition = anchoredPos;

        // 颜色
        Image img = rt.GetComponent<Image>();
        if (img != null)
        {
            img.color = props.mainColor;
            if (props.iconSprite != null)
            {
                img.sprite = props.iconSprite;
            }
            else
            {
                // 无 Sprite 时显示纯色方块
                img.sprite = null;
            }
        }

        rt.sizeDelta = new Vector2(props.iconSize, props.iconSize);
    }

    /// <summary>
    /// 配置箭头在屏幕边缘的视觉效果
    /// </summary>
    private void ApplyArrowVisual(RectTransform rt, IndicatorProperties props, Vector3 screenPoint, bool isBehindCamera)
    {
        rt.gameObject.SetActive(true);

        // 位置：钳制到屏幕边缘
        Vector3 clampedPoint = ClampToScreenEdge(screenPoint, isBehindCamera);
        Vector2 anchoredPos = ScreenToCanvasPosition(clampedPoint);
        rt.anchoredPosition = anchoredPos;

        // 颜色
        Image img = rt.GetComponent<Image>();
        if (img != null)
        {
            img.color = props.mainColor;
            if (props.iconSprite != null)
            {
                img.sprite = props.iconSprite;
            }
        }

        rt.sizeDelta = new Vector2(props.arrowSize, props.arrowSize);

        // 旋转箭头指向目标方向
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 worldScreenPoint = isBehindCamera
            ? screenCenter + (screenCenter - (Vector2)clampedPoint)
            : (Vector3)screenPoint;
        Vector3 direction = worldScreenPoint - (Vector3)screenCenter;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    /// <summary>
    /// 为图标附加距离文本
    /// </summary>
    private void ApplyDistanceText(TextMeshProUGUI distText, IndicatorObject indicator, IndicatorProperties props,
        Vector3 screenPoint, Camera renderCamera)
    {
        Vector2 anchoredPos = ScreenToCanvasPosition(screenPoint);
        distText.gameObject.SetActive(true);
        distText.rectTransform.anchoredPosition = anchoredPos + new Vector2(0f, props.iconSize * 0.5f + 16f);
        distText.color = props.mainColor;
        distText.text = CalculateDistance(indicator.worldPosition, renderCamera);
    }

    /// <summary>
    /// 为箭头附加距离文本
    /// </summary>
    private void ApplyDistanceTextAtPosition(TextMeshProUGUI distText, IndicatorObject indicator, IndicatorProperties props,
        Vector3 screenPos, Camera renderCamera)
    {
        Vector2 anchoredPos = ScreenToCanvasPosition(screenPos);
        distText.gameObject.SetActive(true);
        distText.rectTransform.anchoredPosition = anchoredPos + new Vector2(0f, props.arrowSize * 0.5f + 16f);
        distText.color = props.mainColor;
        distText.text = CalculateDistance(indicator.worldPosition, renderCamera);
    }

    /// <summary>
    /// 计算目标到摄像机的距离（文本显示）
    /// </summary>
    private string CalculateDistance(Vector3 worldPos, Camera cam)
    {
        float distance = Vector3.Distance(worldPos, cam.transform.position);
        if (distance >= 1000f)
        {
            return $"{distance / 1000f:F1}km";
        }
        return $"{distance:F0}m";
    }

    /// <summary>
    /// 判断屏幕坐标是否在屏幕范围内
    /// </summary>
    private bool IsOnScreen(Vector3 screenPoint)
    {
        return screenPoint.x >= _edgeMargin &&
               screenPoint.x <= Screen.width - _edgeMargin &&
               screenPoint.y >= _edgeMargin &&
               screenPoint.y <= Screen.height - _edgeMargin;
    }

    /// <summary>
    /// 将屏幕坐标钳制到屏幕边缘内
    /// </summary>
    private Vector3 ClampToScreenEdge(Vector3 screenPoint, bool isBehindCamera)
    {
        if (isBehindCamera)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = ((Vector2)screenPoint - screenCenter).normalized;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.up;
        }

        // 计算方向与屏幕边界的交点
        float tX = float.MaxValue;
        float tY = float.MaxValue;

        if (Mathf.Abs(direction.x) > 0.001f)
        {
            tX = direction.x > 0
                ? (Screen.width - _edgeMargin - screenCenter.x) / direction.x
                : (_edgeMargin - screenCenter.x) / direction.x;
        }

        if (Mathf.Abs(direction.y) > 0.001f)
        {
            tY = direction.y > 0
                ? (Screen.height - _edgeMargin - screenCenter.y) / direction.y
                : (_edgeMargin - screenCenter.y) / direction.y;
        }

        float t = Mathf.Min(tX, tY);
        Vector2 clampedPos = screenCenter + direction * t;

        return new Vector3(clampedPos.x, clampedPos.y, screenPoint.z);
    }

    /// <summary>
    /// 屏幕坐标转 Canvas 空间锚点坐标
    /// </summary>
    private Vector2 ScreenToCanvasPosition(Vector3 screenPoint)
    {
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPoint, null, out anchoredPos);
        return anchoredPos;
    }

    /// <summary>
    /// 释放所有活跃视觉效果回池
    /// </summary>
    private void ReleaseAllActiveVisuals()
    {
        foreach (var kvp in _activeArrows)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }

        foreach (var kvp in _activeIcons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }

        foreach (var kvp in _activeDistanceTexts)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }

        _activeArrows.Clear();
        _activeIcons.Clear();
        _activeDistanceTexts.Clear();
    }

    /// <summary>
    /// 清除所有视觉元素（销毁时使用）
    /// </summary>
    public void ClearAllVisuals()
    {
        ReleaseAllActiveVisuals();

        foreach (var rt in _arrowPool)
        {
            if (rt != null)
            {
                Destroy(rt.gameObject);
            }
        }

        foreach (var rt in _iconPool)
        {
            if (rt != null)
            {
                Destroy(rt.gameObject);
            }
        }

        foreach (var text in _distanceTextPool)
        {
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        _arrowPool.Clear();
        _iconPool.Clear();
        _distanceTextPool.Clear();
    }
}
