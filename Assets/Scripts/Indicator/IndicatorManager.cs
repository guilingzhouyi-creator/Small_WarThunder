using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指示管理器：MonoBehaviour 单例，负责所有指示对象的生命周期管理。
/// 提供 CreateIndicator / UpdateIndicator / DestroyIndicator / ClearAllIndicators 接口。
/// 每帧在 LateUpdate 中驱动渲染器进行屏幕空间更新。
/// </summary>
public class IndicatorManager : MonoBehaviour, IIndicatorManager
{
    public static IndicatorManager Instance { get; private set; }

    [Header("配置引用")]
    [SerializeField] private IndicatorCentralRegistry _registry;
    [SerializeField] private IndicatorRenderer _renderer;

    [Header("对象池容量")]
    [SerializeField] private int _maxActiveIndicators = 20;

    private List<IndicatorObject> _activeIndicators = new List<IndicatorObject>();
    private int _nextId = 1;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_registry == null)
        {
            Debug.LogError("[IndicatorManager] IndicatorCentralRegistry 未赋值");
        }

        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<IndicatorRenderer>(true);
        }

        _renderer?.SetRegistry(_registry);
    }

    private void LateUpdate()
    {
        if (_renderer == null || _mainCamera == null)
        {
            return;
        }

        RemoveExpiredIndicators();
        _renderer.RenderIndicators(_activeIndicators, _mainCamera);
    }

    /// <summary>
    /// 创建指示对象
    /// </summary>
    /// <param name="type">指示类型</param>
    /// <param name="worldPosition">世界坐标</param>
    /// <param name="customDuration">自定义时长，-1 使用配置默认值</param>
    /// <param name="customPriority">自定义优先级，-1 使用配置默认值</param>
    /// <returns>指示唯一 ID</returns>
    public int CreateIndicator(EIndicatorType type, Vector3 worldPosition, float customDuration = -1f, int customPriority = -1)
    {
        if (_activeIndicators.Count >= _maxActiveIndicators)
        {
            Debug.LogWarning($"[IndicatorManager] 活跃指示已达上限 {_maxActiveIndicators}，拒绝创建新指示");
            return -1;
        }

        IndicatorProperties props = _registry != null
            ? _registry.GetProperties(type)
            : DefaultProperties(type);

        float duration = customDuration >= 0f ? customDuration : props.defaultDuration;
        int priority = customPriority >= 0 ? customPriority : props.defaultPriority;

        var indicator = new IndicatorObject(_nextId, type, worldPosition, duration, priority);
        _nextId++;

        _activeIndicators.Add(indicator);

        Debug.Log($"[IndicatorManager] 创建指示 id={indicator.id} type={type} pos={worldPosition}");
        return indicator.id;
    }

    /// <summary>
    /// 更新已有指示的世界坐标
    /// </summary>
    public void UpdateIndicator(int indicatorId, Vector3 newWorldPosition)
    {
        var indicator = FindById(indicatorId);
        if (indicator == null)
        {
            Debug.LogWarning($"[IndicatorManager] 未找到指示 id={indicatorId}，无法更新位置");
            return;
        }

        indicator.worldPosition = newWorldPosition;
    }

    /// <summary>
    /// 销毁指定指示
    /// </summary>
    public void DestroyIndicator(int indicatorId)
    {
        var indicator = FindById(indicatorId);
        if (indicator == null)
        {
            return;
        }

        indicator.isExpired = true;
        Debug.Log($"[IndicatorManager] 销毁指示 id={indicatorId}");
    }

    /// <summary>
    /// 获取指示对象
    /// </summary>
    public IndicatorObject GetIndicator(int indicatorId)
    {
        return FindById(indicatorId);
    }

    /// <summary>
    /// 清除所有活跃指示
    /// </summary>
    public void ClearAllIndicators()
    {
        foreach (var indicator in _activeIndicators)
        {
            indicator.isExpired = true;
        }

        Debug.Log("[IndicatorManager] 清除所有指示");
    }

    /// <summary>
    /// 移除所有已过期的指示对象
    /// </summary>
    private void RemoveExpiredIndicators()
    {
        for (int i = _activeIndicators.Count - 1; i >= 0; i--)
        {
            if (_activeIndicators[i].CheckExpired())
            {
                _activeIndicators.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 根据 ID 查找指示对象
    /// </summary>
    private IndicatorObject FindById(int id)
    {
        foreach (var indicator in _activeIndicators)
        {
            if (indicator.id == id)
            {
                return indicator;
            }
        }

        return null;
    }

    /// <summary>
    /// 兜底默认属性（当 registry 未设置时使用）
    /// </summary>
    private IndicatorProperties DefaultProperties(EIndicatorType type)
    {
        return new IndicatorProperties
        {
            mainColor = Color.white,
            edgeColor = Color.black,
            iconSize = 32f,
            arrowSize = 24f,
            iconSprite = null,
            showDistance = true,
            defaultPriority = 0,
            defaultDuration = 0f,
        };
    }
}
