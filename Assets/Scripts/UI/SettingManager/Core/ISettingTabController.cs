namespace NSettingSystem
{
    /// <summary>
    /// 设置面板 Tab 子控制器接口。
    /// 每个 Tab 实现此接口，由 SettingManager 统一管理生命周期与按钮路由。
    /// </summary>
    public interface ISettingTabController
    {
        /// <summary>Tab 唯一标识 Key。</summary>
        string tabKey { get; }

        /// <summary>Tab 从隐藏切换到显示。</summary>
        void OnTabOpened();

        /// <summary>Tab 从显示切换到隐藏。</summary>
        void OnTabClosed();

        /// <summary>用户点击返回按钮，返回 true 表示控制器已处理（如保存数据）。</summary>
        bool OnBackRequested();

        /// <summary>用户点击应用按钮。</summary>
        void OnApplyRequested();

        /// <summary>用户点击取消按钮。</summary>
        void OnCancelRequested();
    }
}
