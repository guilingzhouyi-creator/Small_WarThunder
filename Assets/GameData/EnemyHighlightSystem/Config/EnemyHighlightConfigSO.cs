using UnityEngine;

/// <summary>
/// 敌方高亮配置 ScriptableObject。
/// 定义敌方高亮显示的基本属性、条件、生命周期参数和输出开关。
/// 不包含地图样式参数，地图样式由 EnemyHighlightMapStyleSO 单独管理。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 高亮配置
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/高亮配置")]
public class EnemyHighlightConfigSO : ScriptableObject
{
    [Header("容量")]
    [Tooltip("最大同时高亮的敌方目标数。超过此数量时按距离优先级裁剪。")]
    [Range(1, 50)]
    public int maxHighlightTargets = 20;

    [Tooltip("最大同时衰减中的目标数。")]
    [Range(1, 50)]
    public int maxDecayingTargets = 30;

    [Tooltip("最大最后快照缓存目标数。")]
    [Range(1, 100)]
    public int maxLastSeenCacheTargets = 50;

    [Header("高亮生命周期")]
    [Tooltip("高亮持续时间（秒）：目标进入高亮阶段后的默认存活时长。超时后进入衰减阶段。")]
    [Range(0.5f, 60f)]
    public float highlightDuration = 15f;

    [Tooltip("衰减持续时间（秒）：目标失去侦测后渐隐阶段的时长。")]
    [Range(1f, 30f)]
    public float decayDuration = 5f;

    [Tooltip("最后快照保留时间（秒）：衰减结束后仅保留最后快照标记的最大时长。")]
    [Range(5f, 120f)]
    public float lastSeenRetentionTime = 30f;

    [Header("刷新策略")]
    [Tooltip("状态刷新间隔（秒），高亮管理器 Tick 周期。")]
    [Range(0.01f, 0.5f)]
    public float refreshInterval = 0.1f;

    [Tooltip("是否启用事件驱动更新。true=事件驱动+定时修正，false=纯轮询。")]
    public bool useEventDrivenUpdate = true;

    [Tooltip("定时修正间隔（秒），补偿丢帧和事件丢失。")]
    [Range(0.5f, 5f)]
    public float periodicCorrectionInterval = 2f;

    [Header("输出开关")]
    [Tooltip("是否输出世界空间高亮（轮廓、图标、标签等 TPS 视角表现）。")]
    public bool enableWorldSpaceHighlight = true;

    [Tooltip("是否输出地图投影标记数据（供小地图和大地图消费）。")]
    public bool enableMapProjection = true;

    [Header("世界空间高亮")]
    [Tooltip("高亮轮廓强度系数（0~1），控制世界空间轮廓 Alpha。")]
    [Range(0f, 1f)]
    public float worldSpaceOutlineStrength = 0.9f;

    [Tooltip("高亮图标强度系数（0~1），控制图标 Sprite 可见度。")]
    [Range(0f, 1f)]
    public float worldSpaceIconStrength = 0.85f;

    [Tooltip("高亮标签最大显示距离（米），超过此距离不绘制文字标签。")]
    [Range(10f, 500f)]
    public float labelMaxDisplayDistance = 200f;

    [Tooltip("高亮标签字体大小基础值。")]
    [Range(8, 24)]
    public int labelFontSize = 12;

    [Header("优先级与裁剪")]
    [Tooltip("高亮目标的裁剪优先级策略。true=按距离玩家由近到远优先，false=按进入时间优先。")]
    public bool prioritizeByDistance = true;

    [Tooltip("当目标数超出容量时，优先裁剪该阶段的目标。")]
    public EEnemyHighlightPhase trimPolicyPriority = EEnemyHighlightPhase.LastSeenOnly;
}
