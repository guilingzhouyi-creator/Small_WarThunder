using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 敌方高亮世界空间视图适配层。
/// 负责在 TPS 视角下管理敌方高亮的轮廓、图标、标签等视觉表现。
/// 只处理"当前实时高亮"的敌人，不涉及地图遗留点（最后快照标记）。
/// 内部使用独立创建的视觉对象，不直接修改单位本体材质。
/// </summary>
public class EnemyHighlightViewAdapter
{
    private EnemyHighlightManager _manager;
    private EnemyHighlightConfigSO _config;
    private Camera _mainCamera;
    private Transform _viewRoot;

    // 对象池
    private readonly Dictionary<string, HighlightViewObject> _activeViews = new Dictionary<string, HighlightViewObject>(32);
    private readonly Queue<HighlightViewObject> _inactivePool = new Queue<HighlightViewObject>(32);
    private int _maxPooled = 32;

    public EnemyHighlightViewAdapter(EnemyHighlightManager manager, EnemyHighlightConfigSO config)
    {
        _manager = manager;
        _config = config;
        _mainCamera = Camera.main;
        _maxPooled = Mathf.Max(config.maxHighlightTargets, config.maxDecayingTargets);

        CreateViewRoot();

        if (_manager != null)
        {
            _manager.onHighlightPhaseChanged += OnPhaseChanged;
            _manager.onWorldSpaceHighlightRefreshed += OnPositionRefreshed;
            _manager.onHighlightAlphaUpdated += OnHighlightAlphaUpdated;
        }

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_VIEW_ADAPTER} 已初始化。");
    }

    public void Dispose()
    {
        if (_manager != null)
        {
            _manager.onHighlightPhaseChanged -= OnPhaseChanged;
            _manager.onWorldSpaceHighlightRefreshed -= OnPositionRefreshed;
            _manager.onHighlightAlphaUpdated -= OnHighlightAlphaUpdated;
        }

        ClearAllViews();

        if (_viewRoot != null)
        {
            Object.Destroy(_viewRoot.gameObject);
            _viewRoot = null;
        }

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_VIEW_ADAPTER} 已释放。");
    }

    /// <summary>
    /// 每帧由外部驱动（如 EnemyHighlightManager.Update 末端或独立组件调用），
    /// 更新所有活跃高亮的朝向（Billboard）和距离裁剪。
    /// </summary>
    public void TickUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        var camPos = _mainCamera.transform.position;
        var removal = new List<string>(4);

        foreach (var kvp in _activeViews)
        {
            var view = kvp.Value;
            if (view.go == null) continue;

            // Billboard
            Vector3 toCamera = _mainCamera.transform.position - view.go.transform.position;
            view.go.transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);

            // 标签距离裁剪
            if (view.label != null)
            {
                float dist = Vector3.Distance(view.go.transform.position, camPos);
                view.label.enabled = dist <= _config.labelMaxDisplayDistance;
            }
        }

        // 清理已销毁的视图
        foreach (var uid in removal)
        {
            DestroyView(uid);
        }
    }

    // ========== 事件回调 ==========

    private void OnPhaseChanged(string uid, Vector3 position, EEnemyHighlightPhase phase)
    {
        switch (phase)
        {
            case EEnemyHighlightPhase.Highlighting:
                // 如果存在旧的衰减视图，复用
                if (!_activeViews.TryGetValue(uid, out var existing))
                {
                    CreateView(uid, position);
                }
                SetViewAlpha(uid, _config.worldSpaceOutlineStrength);
                break;

            case EEnemyHighlightPhase.Decaying:
                // 衰减：保留视图，后续由 Manager 的 TickDecay 逐步降低 alpha
                if (!_activeViews.TryGetValue(uid, out var decayingView))
                {
                    CreateView(uid, position);
                }
                break;

            case EEnemyHighlightPhase.LastSeenOnly:
            case EEnemyHighlightPhase.Evicted:
                DestroyView(uid);
                break;
        }
    }

    private void OnPositionRefreshed(string uid, Vector3 position)
    {
        if (_activeViews.TryGetValue(uid, out var view))
        {
            if (view.go != null)
            {
                view.go.transform.position = position;
            }
        }
    }

    private void OnHighlightAlphaUpdated(string uid, float alpha)
    {
        SetViewAlpha(uid, alpha);
    }

    // ========== 视图生命周期 ==========

    private void CreateView(string uid, Vector3 worldPosition)
    {
        var view = GetOrCreateViewObject();
        view.uid = uid;
        view.go.name = $"{EnemyHighlightConstants.LOG_TAG_VIEW_ADAPTER}_View_{uid}";
        view.go.transform.position = worldPosition;
        view.go.transform.SetParent(_viewRoot);
        view.go.SetActive(true);
        _activeViews[uid] = view;
    }

    private void DestroyView(string uid)
    {
        if (!_activeViews.TryGetValue(uid, out var view)) return;

        if (view.go != null)
        {
            view.go.SetActive(false);
            view.go.transform.SetParent(_viewRoot);
            _inactivePool.Enqueue(view);
        }

        _activeViews.Remove(uid);
    }

    private void SetViewAlpha(string uid, float alpha)
    {
        if (!_activeViews.TryGetValue(uid, out var view)) return;

        if (view.iconRenderer != null)
        {
            Color c = view.iconRenderer.color;
            c.a = Mathf.Clamp01(alpha);
            view.iconRenderer.color = c;
        }

        if (view.outlineRenderer != null)
        {
            Color c = view.outlineRenderer.startColor;
            c.a = Mathf.Clamp01(alpha);
            view.outlineRenderer.startColor = c;
            view.outlineRenderer.endColor = c;
        }

        if (view.label != null)
        {
            Color c = view.label.color;
            c.a = Mathf.Clamp01(alpha);
            view.label.color = c;
        }
    }

    private void ClearAllViews()
    {
        foreach (var kvp in _activeViews)
        {
            if (kvp.Value.go != null)
            {
                Object.Destroy(kvp.Value.go);
            }
        }

        _activeViews.Clear();

        while (_inactivePool.Count > 0)
        {
            var v = _inactivePool.Dequeue();
            if (v.go != null) Object.Destroy(v.go);
        }
    }

    // ========== 对象池 ==========

    private HighlightViewObject GetOrCreateViewObject()
    {
        HighlightViewObject view;

        if (_inactivePool.Count > 0)
        {
            view = _inactivePool.Dequeue();
        }
        else
        {
            view = new HighlightViewObject();
            view.go = new GameObject("HighlightView");
            view.go.transform.SetParent(_viewRoot);

            // 轮廓（圆环使用 LineRenderer）
            var lrGo = new GameObject("Outline");
            lrGo.transform.SetParent(view.go.transform);
            lrGo.transform.localPosition = Vector3.zero;
            view.outlineRenderer = lrGo.AddComponent<LineRenderer>();
            view.outlineRenderer.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            view.outlineRenderer.startColor = new Color(1f, 0.2f, 0.1f, 0.8f);
            view.outlineRenderer.endColor = new Color(1f, 0.2f, 0.1f, 0.8f);
            view.outlineRenderer.startWidth = 0.08f;
            view.outlineRenderer.endWidth = 0.08f;
            view.outlineRenderer.loop = true;
            view.outlineRenderer.positionCount = 8;
            BuildOutlineRing(view.outlineRenderer, 1.5f);

            // 图标（Sprite）
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(view.go.transform);
            iconGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            view.iconRenderer = iconGo.AddComponent<SpriteRenderer>();
            view.iconRenderer.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            view.iconRenderer.color = new Color(1f, 0.15f, 0.1f, 0.85f);
            iconGo.transform.localScale = Vector3.one * 0.5f;

            // 标签（TextMeshPro）
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(view.go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            view.label = labelGo.AddComponent<TextMeshPro>();
            view.label.fontSize = _config.labelFontSize;
            view.label.alignment = TextAlignmentOptions.Center;
            view.label.color = Color.white;
        }

        return view;
    }

    private void CreateViewRoot()
    {
        _viewRoot = new GameObject("EnemyHighlightViews").transform;
        _viewRoot.SetParent(null);
        Object.DontDestroyOnLoad(_viewRoot.gameObject);
    }

    /// <summary>
    /// 为 LineRenderer 构建水平圆环顶点（8边形近似）。
    /// </summary>
    private static void BuildOutlineRing(LineRenderer lr, float radius)
    {
        int segments = 8;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    // ========== 内部数据结构 ==========

    private class HighlightViewObject
    {
        public string uid;
        public GameObject go;
        public LineRenderer outlineRenderer;
        public SpriteRenderer iconRenderer;
        public TextMeshPro label;
    }
}
