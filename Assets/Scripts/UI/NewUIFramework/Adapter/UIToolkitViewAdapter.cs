using UnityEngine;
using UnityEngine.UIElements;

namespace NNewUIFramework
{
    /// <summary>
    /// UI Toolkit UIDocument-based 适配器抽象基类。
    /// 封装 UIDocument.rootVisualElement 的 display/opacity 控制，
    /// 子类继承后实现 <see cref="IUIController{TData}"/> 即可接入框架。
    /// </summary>
    /// <typeparam name="TData">打开时传入的数据类型</typeparam>
    public abstract class UIToolkitViewAdapter<TData> : MonoBehaviour, IUIController<TData>, IUIViewAdapter
    {
        [Header("UIDocument (optional)")]
        [SerializeField] protected UIDocument _uiDocument;

        /// <summary>UI Toolkit VisualElement 根节点</summary>
        protected VisualElement _rootVisual;

        public abstract EUIIdentity identity { get; }

        public bool isActive => _rootVisual != null
                                 && _rootVisual.style.display != DisplayStyle.None
                                 && gameObject.activeInHierarchy;

        /// <summary>当前视觉可见状态（不等同于 isActive：被遮盖时 display=Flex 但应当隐藏）</summary>
        protected bool _isVisualVisible;

        public virtual void Open(TData data)
        {
            // 必须在 ResolveUIDocumentIfNeeded 之前激活 GameObject，
            // 确保 UIDocument.OnEnable() 已创建 rootVisualElement。
            // 否则 Close() 后再 Open() 会拿到失效的旧 _rootVisual 引用。
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ResolveUIDocumentIfNeeded();
            if (_rootVisual == null)
            {
                Debug.LogError($"[{GetType().Name}] Open 失败：_rootVisual 为 null", this);
                return;
            }

            _rootVisual.style.display = DisplayStyle.Flex;
            _isVisualVisible = true;
            OnOpened(data);
        }

        public virtual void Close()
        {
            OnClosing();
            if (_rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.None;
                _rootVisual = null;
            }
            _isVisualVisible = false;
            gameObject.SetActive(false);
        }

        public virtual void Suspend()
        {
            // UI Toolkit: 隐藏视觉但不影响底层数据
            if (_rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.None;
            }
        }

        public virtual void Resume()
        {
            if (_isVisualVisible && _rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.Flex;
            }
        }

        public virtual void OnCovered()
        {
            if (_rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.None;
            }
        }

        public virtual void OnRevealed()
        {
            if (_isVisualVisible && _rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>打开回调（子类重写以处理传入数据）</summary>
        protected virtual void OnOpened(TData data) { }

        /// <summary>关闭回调（子类重写以清理状态）</summary>
        protected virtual void OnClosing() { }

        /// <summary>懒加载解析 UIDocument 引用和 rootVisualElement</summary>
        protected void ResolveUIDocumentIfNeeded()
        {
            if (_rootVisual != null)
            {
                return;
            }

            // 优先使用 Inspector 赋值
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    _uiDocument = GetComponentInChildren<UIDocument>(true);
                }
                if (_uiDocument == null)
                {
                    _uiDocument = GetComponentInParent<UIDocument>(true);
                }
            }

            if (_uiDocument != null)
            {
                _rootVisual = _uiDocument.rootVisualElement;
            }

            if (_rootVisual == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 未找到 UIDocument 或 rootVisualElement 尚未就绪", this);
            }
        }

        public void SetVisible(bool visible)
        {
            ResolveUIDocumentIfNeeded();
            if (_rootVisual == null)
                return;

            _rootVisual.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _isVisualVisible = visible;
        }

        public void SetSortingOrder(int order)
        {
            if (_uiDocument == null)
                return;

            // UIDocument.sortingOrder 影响 Panel 在渲染层级的排序（通过 UI Toolkit 渲染管线）
            _uiDocument.sortingOrder = order;
        }

        public bool isVisible => _rootVisual != null
                                   && _rootVisual.style.display != DisplayStyle.None
                                   && gameObject.activeInHierarchy;

        public object rootObject => (object)_uiDocument;

        protected virtual void Awake()
        {
            ResolveUIDocumentIfNeeded();
            if (_rootVisual != null)
            {
                _rootVisual.style.display = DisplayStyle.None;
                _isVisualVisible = false;
            }
        }
    }
}
