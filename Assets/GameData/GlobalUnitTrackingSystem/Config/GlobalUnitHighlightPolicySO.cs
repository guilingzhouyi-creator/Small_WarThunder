using UnityEngine;

/// <summary>
/// 全局单位高亮策略 ScriptableObject。
/// 定义哪些单位可以进入高亮与地图投影的规则。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → HighlightPolicy
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/高亮策略")]
public class GlobalUnitHighlightPolicySO : ScriptableObject
{
    [Header("阵营高亮开关")]
    [Tooltip("是否高亮敌方单位。")]
    public bool highlightEnemy = true;

    [Tooltip("是否高亮友军单位。")]
    public bool highlightAlly = true;

    [Tooltip("是否高亮中立单位。")]
    public bool highlightNeutral = false;

    [Header("高亮条件")]
    [Tooltip("仅高亮处于侦测范围内的单位。关闭则可能显示已知位置的友军（全局视野）。")]
    public bool onlyHighlightDetectedUnits = true;

    [Tooltip("单位死亡后是否仍显示最后快照标记（地图遗留点），但不再实时高亮。")]
    public bool showLastSeenOnDeath = true;

    [Tooltip("是否在失去侦测后仍保留高亮一小段时间（渐隐），0=立即关闭。")]
    [Range(0f, 10f)]
    public float highlightFadeAfterLoss = 1.5f;

    [Header("地图投影策略")]
    [Tooltip("小地图模式下显示的最大敌方标记数量。")]
    [Range(1, 50)]
    public int miniMapMaxEnemyMarkers = 10;

    [Tooltip("大地图模式下显示的最大敌方标记数量。")]
    [Range(1, 100)]
    public int fullMapMaxEnemyMarkers = 50;

    [Tooltip("小地图是否显示友军标记。")]
    public bool miniMapShowAllies = false;

    [Tooltip("大地图是否显示友军标记。")]
    public bool fullMapShowAllies = true;

    [Tooltip("是否在TPS视角中显示敌方轮廓/图标。")]
    public bool showWorldHighlight = true;

    [Tooltip("世界高亮的最大同时显示数量。")]
    [Range(1, 20)]
    public int maxWorldHighlightCount = 8;
}
