/// <summary>
/// 敌方高亮显示系统枚举定义。
/// 包含高亮生命周期阶段枚举、衰减曲线类型枚举。
/// </summary>

/// <summary>
/// 敌方高亮生命周期阶段，标识一个被感知敌方目标在高亮系统中的状态。
/// </summary>
public enum EEnemyHighlightPhase
{
    /// <summary>未知：默认状态，未初始化或无有效高亮。</summary>
    Unknown,
    /// <summary>高亮中：目标当前处于实时侦测范围内，TPS 视角全显 + 地图实时 Marker。</summary>
    Highlighting,
    /// <summary>衰减中：目标失去侦测后进入短时衰减，TPS 轮廓渐隐 + 地图衰减 Marker。</summary>
    Decaying,
    /// <summary>仅快照：TPS 轮廓已隐藏，仅保留最后快照 Marker 供地图弱提示。</summary>
    LastSeenOnly,
    /// <summary>已驱逐：完全清除，不再出现在高亮或地图中。</summary>
    Evicted
}

/// <summary>
/// 衰减曲线类型枚举，定义透明度从初始值下降到下限的数学曲线。
/// </summary>
public enum EDecayCurve
{
    /// <summary>线性衰减：透明度均匀下降。</summary>
    Linear,
    /// <summary>指数衰减：前期下降快，后期慢。</summary>
    Exponential,
    /// <summary>平滑步进：匀速后急停，S 形曲线。</summary>
    SmoothStep
}
