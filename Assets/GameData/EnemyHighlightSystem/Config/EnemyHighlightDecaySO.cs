using UnityEngine;

/// <summary>
/// 敌方高亮衰减配置 ScriptableObject。
/// 定义高亮目标失去侦测后的衰减策略，包括持续时长、透明度曲线、颜色过渡等。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 衰减配置
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/衰减配置")]
public class EnemyHighlightDecaySO : ScriptableObject
{
    [Header("衰减时长")]
    [Tooltip("高亮衰减总时长（秒）。从失去侦测开始到完全降级为最后快照。")]
    [Range(0.5f, 15f)]
    public float decayDurationSeconds = 3f;

    [Tooltip("最后快照保留时长（秒）。超过此时长后缓存条目被清除。")]
    [Range(5f, 120f)]
    public float lastSeenRetentionSeconds = 30f;

    [Header("透明度衰减曲线")]
    [Tooltip("透明度从实时高亮最大值衰减到0的曲线（0=起始，1=结束）。")]
    public AnimationCurve opacityDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("颜色过渡")]
    [Tooltip("是否启用颜色过渡（从实时高亮色过渡到最后快照色）。")]
    public bool enableColorTransition = true;

    [Tooltip("颜色过渡曲线（0=实时色，1=快照色）。")]
    public AnimationCurve colorTransitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("半径衰减")]
    [Tooltip("标记半径从实时尺寸衰减到最后快照尺寸的曲线（0=实时尺寸，1=快照尺寸）。")]
    public AnimationCurve radiusDecayCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

    [Header("标签衰减")]
    [Tooltip("是否在衰减期间隐藏单位文本标签。")]
    public bool hideLabelDuringDecay = true;

    [Tooltip("最后快照标记是否显示标签。")]
    public bool showLabelOnLastSeen = false;
}
