using UnityEngine;

/// <summary>
/// 标记样式 ScriptableObject。
/// 定义敌友颜色、半径、标签和衰减策略。
/// 小地图和大地图共享同一份样式配置，但允许不同的渲染参数。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → MarkerStyle
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/标记样式")]
public class GlobalUnitMarkerStyleSO : ScriptableObject
{
    [Header("敌方标记样式")]
    [Tooltip("敌方实时高亮颜色。")]
    public Color enemyLiveColor = Color.red;

    [Tooltip("敌方最后快照标记颜色（衰减后趋于此色）。")]
    public Color enemySnapshotColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);

    [Tooltip("敌方标记半径（像素）。")]
    [Range(2f, 20f)]
    public float enemyMarkerRadius = 6f;

    [Header("友军标记样式")]
    [Tooltip("友军实时高亮颜色。")]
    public Color allyLiveColor = Color.blue;

    [Tooltip("友军最后快照标记颜色。")]
    public Color allySnapshotColor = new Color(0.3f, 0.5f, 0.8f, 0.4f);

    [Tooltip("友军标记半径（像素）。")]
    [Range(2f, 20f)]
    public float allyMarkerRadius = 5f;

    [Header("中立标记样式")]
    [Tooltip("中立单位颜色。")]
    public Color neutralColor = Color.gray;

    [Tooltip("中立标记半径（像素）。")]
    [Range(2f, 20f)]
    public float neutralMarkerRadius = 4f;

    [Header("标签")]
    [Tooltip("是否在标记旁显示单位类型标签。")]
    public bool showLabels = true;

    [Tooltip("小地图标签字号。")]
    [Range(6, 24)]
    public int miniMapLabelFontSize = 8;

    [Tooltip("大地图标签字号。")]
    [Range(8, 32)]
    public int fullMapLabelFontSize = 12;

    [Tooltip("仅在大地图显示标签。")]
    public bool labelsOnlyOnFullMap = true;

    [Header("衰减策略")]
    [Tooltip("最后快照标记不透明度衰减曲线：控制标记从最新到最旧的透明度变化。")]
    public AnimationCurve snapshotFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

    [Tooltip("最后快照标记半径衰减系数，随时间缩小。")]
    public AnimationCurve snapshotRadiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

    [Header("小地图/大地图差异化")]
    [Tooltip("小地图标记半径缩放系数。")]
    [Range(0.3f, 1.5f)]
    public float miniMapRadiusScale = 0.6f;

    [Tooltip("大地图标记半径缩放系数。")]
    [Range(0.5f, 2f)]
    public float fullMapRadiusScale = 1.2f;
}
