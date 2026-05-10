namespace NNewUIFramework
{
    /// <summary>
    /// 视图适配器接口，抹平 UGUI / UITK 差异
    /// 每个 IUIController 持有其对应的 IUIViewAdapter 实现
    /// </summary>
    public interface IUIViewAdapter
    {
        /// <summary>设置根视图的可见性</summary>
        void SetVisible(bool visible);

        /// <summary>设置渲染排序层级（用于解决 UI 叠加顺序问题）</summary>
        void SetSortingOrder(int order);

        /// <summary>获取当前可见状态</summary>
        bool isVisible { get; }

        /// <summary>适配器绑定的根 Transform（UGUI）或 VisualElement（UITK），用于调试</summary>
        object rootObject { get; }
    }
}
