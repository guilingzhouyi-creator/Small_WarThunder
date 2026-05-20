using UnityEngine;

/// <summary>
/// 敌方高亮状态 ScriptableObject。
/// 持有当前所有追踪敌方目标的运行时快照，包括位置、阶段、时间戳。
/// 不负责渲染，仅作为高亮系统的数据容器。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 高亮状态
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/高亮状态")]
public class EnemyHighlightStateSO : ScriptableObject
{
    [Header("运行时状态列表")]
    [Tooltip("当前高亮中的敌方目标UID列表（运行时填充）。")]
    public string[] highlightedUids;

    [Tooltip("当前衰减中的敌方目标UID列表（运行时填充）。")]
    public string[] decayingUids;

    [Tooltip("当前仅保留最后快照的敌方目标UID列表（运行时填充）。")]
    public string[] lastSeenOnlyUids;

    [Header("统计信息")]
    [Tooltip("当前高亮中的目标数量。")]
    public int highlightedCount;

    [Tooltip("当前衰减中的目标数量。")]
    public int decayingCount;

    [Tooltip("当前最后快照目标数量。")]
    public int lastSeenCount;

    [Tooltip("历史累计进入过高亮状态的总目标数。")]
    public int totalTrackedCount;
}
