namespace NNewUIFramework
{
    /// <summary>
    /// UI 控制器泛型接口，定义框架调度的标准契约
    /// 所有业务 UI Controller 必须实现此接口
    /// </summary>
    /// <typeparam name="TData">打开该 UI 时传入的数据类型，无数据则用 object</typeparam>
    public interface IUIController<TData>
    {
        /// <summary>UI 面板标识</summary>
        EUIIdentity identity { get; }

        /// <summary>当前活跃状态</summary>
        bool isActive { get; }

        /// <summary>打开 / 显示 UI，可接收数据</summary>
        void Open(TData data);

        /// <summary>关闭 / 隐藏 UI</summary>
        void Close();

        /// <summary>暂停 UI（被高层栈 Exclusive 压入时调用）</summary>
        void Suspend();

        /// <summary>恢复 UI（从 Suspend 状态回到 Active）</summary>
        void Resume();

        /// <summary>被高层栈遮挡时调用（不改变栈状态，仅隐藏视觉）</summary>
        void OnCovered();

        /// <summary>遮挡解除后调用（恢复视觉显示）</summary>
        void OnRevealed();
    }
}
