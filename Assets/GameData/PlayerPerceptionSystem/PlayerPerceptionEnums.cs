/// <summary>
/// 玩家侦查与感知系统枚举定义。
/// 包含感知状态枚举和感知事件类型枚举。
/// </summary>

/// <summary>
/// 玩家感知状态枚举，标识单位在玩家感知系统中的生命周期阶段。
/// </summary>
public enum EPlayerAwareness
{
    /// <summary>不可见：未进入玩家感知范围。</summary>
    None,
    /// <summary>确认中：已进入视野/感知范围，但尚未通过确认时间窗。</summary>
    Confirming,
    /// <summary>已确认：通过确认窗，当前处于实时感知状态。</summary>
    Confirmed,
    /// <summary>衰减中：失去确认后进入短时记忆阶段，保留最后快照。</summary>
    Decaying,
    /// <summary>已丢失：衰减结束，仅保留最后快照标记供地图显示。</summary>
    Lost
}

/// <summary>
/// 玩家感知事件类型枚举，用于事件路由层广播。
/// </summary>
public enum EPlayerPerceptionEventType
{
    /// <summary>发现新目标：单位进入确认中状态。</summary>
    Discovered,
    /// <summary>感知刷新：已确认目标的位置或状态更新。</summary>
    Refreshed,
    /// <summary>确认完成：确认中目标通过时间窗，转为实时感知。</summary>
    Confirmed,
    /// <summary>失去确认：已确认目标进入衰减阶段。</summary>
    Lost,
    /// <summary>衰减变化：衰减中目标的感知强度或时间更新。</summary>
    Decaying,
    /// <summary>恢复确认：衰减中目标被重新确认，回到实时感知。</summary>
    Recovered,
    /// <summary>彻底移除：目标从感知系统完全移除。</summary>
    Removed
}
