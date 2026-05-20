using UnityEngine;

namespace NNewUIFramework
{
    /// <summary>
    /// UGUI Canvas-based UI 适配器抽象基类。
    /// 子类只需继承并实现 <see cref="IUIController{TData}"/> 即可接入框架。
    /// </summary>
    public abstract class UGUIViewAdapter : MonoBehaviour, IUIController<object>, IUIViewAdapter
    {
        [Header("ViewAdapter")]
        [SerializeField] protected Canvas _canvas;

        public abstract EUIIdentity identity { get; }

        public bool isActive => gameObject.activeSelf;

        public object rootObject => _canvas != null ? (object)_canvas.transform : (object)gameObject;

        public bool isVisible => gameObject.activeInHierarchy;

        public virtual void Open(object data)
        {
            if ((UnityEngine.Object)this == null)
            {
                return;
            }

            gameObject.SetActive(true);
            OnOpened(data);
        }

        public virtual void Close()
        {
            if ((UnityEngine.Object)this == null)
            {
                return;
            }

            OnClosing();
            gameObject.SetActive(false);
        }

        public virtual void Suspend()
        {
            if ((UnityEngine.Object)this == null)
            {
                return;
            }

            // UGUI: 保持 GameObject Active 但设置 Canvas 不可交互/不渲染
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        public virtual void Resume()
        {
            if ((UnityEngine.Object)this == null)
            {
                return;
            }

            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
        }

        public virtual void OnCovered()
        {
            // 视觉层: 被高层栈遮盖时可选处理（如 Canvas.enabled = false）
        }

        public virtual void OnRevealed()
        {
            // 视觉层: 遮盖解除
        }

        /// <summary>打开回调（子类重写以处理传入数据）</summary>
        protected virtual void OnOpened(object data) { }

        /// <summary>关闭回调（子类重写以清理状态）</summary>
        protected virtual void OnClosing() { }

        public void SetVisible(bool visible)
        {
            if ((UnityEngine.Object)this == null)
            {
                return;
            }

            gameObject.SetActive(visible);
        }

        public void SetSortingOrder(int order)
        {
            // 只影响渲染顺序；Canvas 可能未绑定时不报错
            if (_canvas == null)
                return;

            _canvas.sortingOrder = order;
        }

        protected virtual void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>(true);
                }
            }

            // 默认隐藏（由框架在 Open 时显示）
            gameObject.SetActive(false);
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
        }
    }
}
