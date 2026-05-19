/// <summary>
/// 全局单位跟踪系统枚举定义。
/// 包含单位阵营、生命状态、跟踪状态和跟踪事件类型。
/// </summary>
public enum EUnitFaction
{
    /// <summary>玩家阵营</summary>
    Player,
    /// <summary>友军阵营</summary>
    Ally,
    /// <summary>敌方阵营</summary>
    Enemy,
    /// <summary>中立单位</summary>
    Neutral
}

/// <summary>
/// 单位生命状态枚举。
/// </summary>
public enum EUnitLifeStatus
{
    /// <summary>存活</summary>
    Alive,
    /// <summary>已死亡</summary>
    Dead,
    /// <summary>已摧毁（完全不可恢复）</summary>
    Destroyed
}

/// <summary>
/// 单位跟踪状态枚举。
/// </summary>
public enum EUnitTrackStatus
{
    /// <summary>未跟踪</summary>
    NotTracked,
    /// <summary>正在跟踪（处于侦测范围内）</summary>
    Tracking,
    /// <summary>已丢失（失去侦测，保留最后快照）</summary>
    Lost
}

/// <summary>
/// 跟踪事件类型枚举，用于事件广播层。
/// </summary>
public enum EUnitTrackingEventType
{
    /// <summary>单位生成并注册</summary>
    UnitSpawned,
    /// <summary>单位销毁并注销</summary>
    UnitDestroyed,
    /// <summary>单位进入侦测范围</summary>
    EnterDetection,
    /// <summary>单位退出侦测范围</summary>
    ExitDetection,
    /// <summary>单位位置更新</summary>
    PositionUpdated,
    /// <summary>单位生命状态变更</summary>
    LifeStatusChanged,
    /// <summary>单位高亮状态变更</summary>
    HighlightChanged,
    /// <summary>单位最后快照位置更新（失去侦测时记录）</summary>
    LastSeenUpdated
}
