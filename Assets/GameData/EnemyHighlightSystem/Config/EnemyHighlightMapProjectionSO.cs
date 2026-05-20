using UnityEngine;

/// <summary>
/// 敌方高亮地图投影配置 ScriptableObject。
/// 定义小地图/大地图的投影规则与落点策略，不负责绘制。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 地图投影
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/地图投影")]
public class EnemyHighlightMapProjectionSO : ScriptableObject
{
    [Header("投影范围")]
    [Tooltip("小地图投影最大距离（世界单位）。超过此距离的标记不显示在小地图。")]
    public float minimapMaxDistance = 1500f;

    [Tooltip("大地图投影最大距离（世界单位）。超过此距离的标记不显示在大地图。")]
    public float bigmapMaxDistance = 8000f;

    [Header("落点策略")]
    [Tooltip("最后快照落点是否始终显示（即使超出投影距离）。")]
    public bool alwaysShowLastSeen = true;

    [Tooltip("小地图上是否显示最后快照标记。")]
    public bool minimapShowLastSeen = true;

    [Tooltip("大地图上是否显示最后快照标记。")]
    public bool bigmapShowLastSeen = true;

    [Header("聚合/压缩")]
    [Tooltip("小地图是否启用邻近标记聚合。")]
    public bool minimapEnableClustering = true;

    [Tooltip("小地图标记聚合距离阈值（像素）。")]
    [Range(5f, 60f)]
    public float minimapClusterThreshold = 20f;

    [Tooltip("大地图是否启用邻近标记聚合。")]
    public bool bigmapEnableClustering = false;

    [Tooltip("大地图标记聚合距离阈值（像素）。")]
    [Range(8f, 80f)]
    public float bigmapClusterThreshold = 30f;

    [Header("Z轴/高度处理")]
    [Tooltip("投影时是否忽略Y轴高度差。")]
    public bool ignoreAltitude = true;

    [Tooltip("高度修正偏移量（仅在ignoreAltitude=false时生效）。")]
    public float altitudeOffset = 0f;
}
