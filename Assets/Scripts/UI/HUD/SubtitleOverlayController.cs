using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using NNewUIFramework;

public class SubtitleOverlayController : UIToolkitViewAdapter<object>
{
    public override EUIIdentity identity => EUIIdentity.SubtitleOverlay;

    public static SubtitleOverlayController Instance { get; private set; }

    private VisualElement _subtitleRoot;
    private Label _subtitleText;

    // 状态
    private bool _isManuallyVisible;   // ★ 仅 Map 模式下 Tab 按键切换
    private bool _isNarrativeActive;   // 由 MissionPannelUIController 设置：当前是否有叙事包在播放
    private bool _textBridgeBound;
    private int _initializedSceneHandle = -1;

    // 缓存最近一次接收到的文本，用于切换显示时立即恢复
    private string _cachedText = string.Empty;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    protected override void OnOpened(object data)
    {
        InitializeUI();
    }

    protected override void OnClosing()
    {
        ResetForSceneExit();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeUI()
    {
        ResolveUIDocument();

        if (_uiDocument == null)
        {
            Debug.LogError("[SubtitleOverlayController] 未找到 UIDocument 组件，请在同一 GameObject 上添加 UIDocument。", this);
            return;
        }

        // 确保字幕面板始终渲染在所有其他 UIDocument（如地图 MapUI）之上
        _uiDocument.sortingOrder = 100;

        var root = _uiDocument.rootVisualElement;
        if (root == null)
        {
            Invoke(nameof(InitializeUI), 0.05f); // 重试：rootVisualElement 可能尚未就绪
            return;
        }

        // 该覆盖层自行管理内部 subtitle-root 的显隐，不依赖 UIToolkitViewAdapter.Open()
        // 因此需要主动解除基类在 Awake() 中对整个 rootVisualElement 的隐藏。
        root.style.display = DisplayStyle.Flex;
        root.style.opacity = 1f;

        // 清空 root，确保干净状态（UIDocument 没有绑定 UXML 时通常为空）
        root.Clear();

        // 加载 USS 样式（从 Resources 文件夹）
        StyleSheet styleSheet = Resources.Load<StyleSheet>(SceneAssetPaths.UI.SubtitleHUD);
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }
        else
        {
            Debug.LogWarning($"[SubtitleOverlayController] 未能在 Resources 中找到 USS: {SceneAssetPaths.UI.SubtitleHUD}", this);
        }

        // 动态构建 UI 层级
        _subtitleRoot = new VisualElement();
        _subtitleRoot.AddToClassList(UIStyleClassNames.SubtitleRoot);

        _subtitleText = new Label();
        _subtitleText.enableRichText = true;
        _subtitleText.AddToClassList(UIStyleClassNames.SubtitleText);

        _subtitleRoot.Add(_subtitleText);
        root.Add(_subtitleRoot);

        _subtitleRoot.style.display = DisplayStyle.None;

        BindTextBridge();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForScene(scene);
    }

    private void ResetForSceneExit()
    {
        UnbindTextBridge();

        ClearOverlay();
        _initializedSceneHandle = -1;
    }

    public bool InitializeForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        if (_initializedSceneHandle == scene.handle)
        {
            return true;
        }

        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            ResetForSceneExit();
            _initializedSceneHandle = scene.handle;
            return true;
        }

        InitializeUI();
        ApplyVisibility();
        _initializedSceneHandle = scene.handle;
        return true;
    }

    private void ResolveUIDocument()
    {
        // 优先使用 Inspector 拖入的引用
        if (_uiDocument != null)
        {
            return;
        }

        // 回退：自动查找同物体或子物体上的 UIDocument
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            _uiDocument = GetComponentInChildren<UIDocument>(true);
        }
    }

    /// <summary>
    /// 设置字幕文本（由叙事系统或 GlobalSubtitleEngine 打字机事件调用）。
    /// 如果文本已包含颜色标签（来自引擎打字机缓存），直接使用；否则通过 SubtitleColorRenderEngine 着色。
    /// </summary>
    public void SetText(string text)
    {
        _cachedText = text ?? string.Empty;

        if (_subtitleText == null)
        {
            InitializeUI();
        }

        if (_subtitleText != null)
        {
            // 已包含颜色标签（来自 GlobalSubtitleEngine 打字机缓存的 GetVisibleSubstring 输出），跳过重复着色
            if (_cachedText.Contains("<color="))
            {
                _subtitleText.text = _cachedText;
            }
            else
            {
                SubtitleChannel channel = GlobalSubtitleEngine.Instance?.CurrentPackage?.Channel ?? SubtitleChannel.System;
                string colored = SubtitleColorRenderEngine.Process(_cachedText, SubtitleRenderScope.Overlay, channel);
                _subtitleText.text = colored;
            }
        }
    }

    /// <summary>
    /// 标记叙事包是否活跃（由 MissionPannelUIController 调用）
    /// </summary>
    public void SetNarrativeActive(bool active)
    {
        if (_subtitleRoot == null)
        {
            InitializeUI();
        }

        if (_isNarrativeActive == active)
        {
            return;
        }

        _isNarrativeActive = active;
        ApplyVisibility();
    }

    public void ClearOverlay()
    {
        _isManuallyVisible = false;
        _isNarrativeActive = false;
        _cachedText = string.Empty;

        if (_subtitleText != null)
        {
            _subtitleText.text = string.Empty;
        }

        if (_subtitleRoot != null)
        {
            _subtitleRoot.style.display = DisplayStyle.None;
            _subtitleRoot.style.opacity = 0f;
            _subtitleRoot.RemoveFromClassList(UIStyleClassNames.SubtitleVisible);
        }
    }

    /// <summary>
    /// Map 模式下 Tab 按键切换手动显示状态
    /// 由 UIManager.HandleTabInput() 调用（仅当 _isMapShown == true 时）
    /// </summary>      
    /// <summary>字幕当前是否在屏幕上物理可见</summary>
    public bool IsSubtitleVisible
    {
        get
        {
            return _subtitleRoot != null
                && _subtitleRoot.style.display == DisplayStyle.Flex
                && _subtitleRoot.style.opacity.value > 0f;
        }
    }

    /// <summary>强制显示字幕</summary>
    public void ShowSubtitle()
    {
        _isManuallyVisible = true;
        ApplyVisibility();
    }

    /// <summary>强制隐藏字幕</summary>
    public void HideSubtitle()
    {
        _isManuallyVisible = false;
        ApplyVisibility();
    }

    public void ToggleManualVisibility()
    {
        if (NewUIManager.instance == null || !NewUIManager.instance.IsMapShown)
        {
            Debug.LogWarning($"[SubtitleOverlayController] ToggleManualVisibility 被调用但条件不满足：IsAvailable={NewUIManager.instance != null}, IsMapShown={NewUIManager.instance?.IsMapShown}", this);
            return;
        }

        _isManuallyVisible = !_isManuallyVisible;
        Debug.Log($"[SubtitleOverlayController] 手动字幕切换：{(_isManuallyVisible ? "显示" : "隐藏")}", this);
        ApplyVisibility();
    }

    /// <summary>
    /// 核心门禁引擎：根据所有条件决定最终显示/隐藏
    /// </summary>
    public void ApplyVisibility()
    {
        if (!SceneLoader.IsScene(SceneManager.GetActiveScene(), SceneLoader.Scene.GameScene))
        {
            return;
        }

        if (_subtitleRoot == null)
        {
            InitializeUI();
        }

        if (_subtitleRoot == null)
        {
            Debug.LogWarning("[SubtitleOverlayController] ApplyVisibility 被调用但 _subtitleRoot 为 null（UI 尚未初始化完成）。", this);
            return;
        }

        if (NewUIManager.instance == null)
        {
            Debug.LogWarning("[SubtitleOverlayController] ApplyVisibility 被调用但 NewUIManager.instance 为 null。", this);
            return;
        }

        // 门禁1：绝对隐藏层 — Tab / ESC暂停 / 设置 / CG播放
        bool isAbsoluteHidden = NewUIManager.instance.IsPaused
                             || NewUIManager.instance.IsSettingUIVisible
                             || NewUIManager.instance.IsTabed
                             || NewUIManager.instance.IsCgPlaying;

        if (isAbsoluteHidden)
        {
            SetUITVisibility(false);
            return;
        }

        // 门禁2：Map 大地图模式 — 听从手动 Tab 切换
        if (NewUIManager.instance.IsMapShown)
        {
            SetUITVisibility(_isManuallyVisible);
            return;
        }

        // 门禁3：TPS / AIM 主游戏 — 听从叙事包驱动
        SetUITVisibility(_isNarrativeActive);
    }

    private void SetUITVisibility(bool visible)
    {
        if (_subtitleRoot == null)
        {
            Debug.LogWarning("[SubtitleOverlayController] SetUITVisibility 被调用但 _subtitleRoot 为 null。", this);
            return;
        }

        if (visible)
        {
            _subtitleRoot.style.display = DisplayStyle.Flex;
            _subtitleRoot.style.opacity = 1f;
            _subtitleRoot.AddToClassList(UIStyleClassNames.SubtitleVisible);
        }
        else
        {
            _subtitleRoot.style.display = DisplayStyle.None;
            _subtitleRoot.style.opacity = 0f;
            _subtitleRoot.RemoveFromClassList(UIStyleClassNames.SubtitleVisible);
        }
    }

    private void BindTextBridge()
    {
        if (_textBridgeBound)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.OnOverlayTextChanged += HandleSubtitleTextChanged;
            _textBridgeBound = true;
            return;
        }

        // GlobalSubtitleEngine 尚未就绪，延迟重试（最多 30 次 = 3 秒）
        Debug.LogWarning("[SubtitleOverlayController] GlobalSubtitleEngine.Instance 尚未就绪，0.1 秒后重试绑定。", this);
        StartCoroutine(RetryBindTextBridge(0.1f, 30));
    }

    private IEnumerator RetryBindTextBridge(float interval, int maxAttempts)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            yield return new WaitForSeconds(interval);

            if (_textBridgeBound)
            {
                yield break;
            }

            if (GlobalSubtitleEngine.Instance != null)
            {
                GlobalSubtitleEngine.Instance.OnOverlayTextChanged += HandleSubtitleTextChanged;
                _textBridgeBound = true;
                Debug.Log("[SubtitleOverlayController] 延迟绑定成功。", this);
                yield break;
            }
        }

        Debug.LogError($"[SubtitleOverlayController] 绑定失败：经过 {maxAttempts} 次重试后 GlobalSubtitleEngine.Instance 仍为 null。字幕将无法从引擎直接接收文本（但 MissionPannelUIController 的桥接路径仍可用）。", this);
    }

    private void UnbindTextBridge()
    {
        if (!_textBridgeBound)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.OnOverlayTextChanged -= HandleSubtitleTextChanged;
        }

        _textBridgeBound = false;
    }

    private void HandleSubtitleTextChanged(string text)
    {
        SetText(text);
    }
}
