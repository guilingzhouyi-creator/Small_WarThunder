namespace NNewUIFramework
{
    /// <summary>
    /// UI 面板全局唯一标识枚举
    /// 框架层预定义，使用者按需在末尾追加新成员扩展
    /// </summary>
    public enum EUIIdentity
    {
        None = 0,

        // Permanent 层
        SubtitlePanel,
        FpsPanel,

        // Gameplay 层
        TankAimPanel,
        TankStatePanel,
        FcsHudPanel,

        // Overlay 层
        MapPanel,
        PausePanel,
        MissionPanel,
        SubtitleOverlay,

        // System 层
        SettingsPanel,
        KeyBindingPanel
    }
}
