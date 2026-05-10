namespace NNewUIFramework
{
    /// <summary>
    /// UI 上下文类型枚举，定义栈的层级（优先级从高到低）
    /// System > Overlay > Gameplay > Permanent
    /// 高优先级栈非空时，自动隐藏所有低优先级栈的活跃 UI
    /// </summary>
    public enum EUIContextType
    {
        /// <summary>底层常驻 UI，如 FPS 显示、通知条</summary>
        Permanent = 0,
        /// <summary>Gameplay HUD，如坦克瞄准、状态面板</summary>
        Gameplay = 1,
        /// <summary>中层覆盖 UI，如地图、暂停、任务面板</summary>
        Overlay = 2,
        /// <summary>系统级 UI，如设置面板（最高优先级，遮挡一切）</summary>
        System = 3
    }
}
