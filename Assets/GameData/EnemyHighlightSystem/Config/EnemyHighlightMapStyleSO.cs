using UnityEngine;

/// <summary>
/// 地图高亮样式 ScriptableObject。
/// 定义小地图和大地图上敌方高亮标记的颜色、半径、透明度和标签策略。
/// 该 SO 专属于敌方高亮系统的地图投影输出样式，不与 MapConfigSO 的通用敌友颜色混淆。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 地图样式
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/地图样式")]
public class EnemyHighlightMapStyleSO : ScriptableObject
{
    [Header("实时高亮 — 标记颜色")]
    [Tooltip("实时高亮敌方标记颜色（小地图/大地图共用基色）。")]
    public Color liveMarkerColor = Color.red;

    [Tooltip("衰减中敌方标记颜色。")]
    public Color decayingMarkerColor = new Color(0.9f, 0.3f, 0.1f, 0.8f);

    [Tooltip("最后快照敌方标记颜色（弱提示）。")]
    public Color lastSeenMarkerColor = new Color(0.4f, 0.2f, 0.2f, 0.45f);

    [Header("小地图 — 标记半径（像素）")]
    [Tooltip("实时高亮标记半径。")]
    [Range(2f, 15f)]
    public float minimapLiveRadius = 5f;

    [Tooltip("衰减中标记半径。")]
    [Range(1f, 10f)]
    public float minimapDecayingRadius = 3.5f;

    [Tooltip("最后快照标记半径。")]
    [Range(1f, 8f)]
    public float minimapLastSeenRadius = 2.5f;

    [Header("大地图 — 标记半径（像素）")]
    [Tooltip("实时高亮标记半径。")]
    [Range(4f, 25f)]
    public float bigmapLiveRadius = 10f;

    [Tooltip("衰减中标记半径。")]
    [Range(3f, 18f)]
    public float bigmapDecayingRadius = 7f;

    [Tooltip("最后快照标记半径。")]
    [Range(2f, 12f)]
    public float bigmapLastSeenRadius = 4f;

    [Header("标签策略")]
    [Tooltip("小地图是否显示单位文本标签（如\"T - 80\"）。")]
    public bool minimapShowLabels = false;

    [Tooltip("大地图是否显示单位文本标签。")]
    public bool bigmapShowLabels = true;

    [Tooltip("标签字体大小（大地图）。")]
    [Range(8, 24)]
    public int labelFontSize = 11;

    [Header("透明度策略")]
    [Tooltip("实时高亮标记透明度（0~1）。")]
    [Range(0.3f, 1f)]
    public float liveMarkerOpacity = 0.95f;

    [Tooltip("衰减中标记透明度。")]
    [Range(0.1f, 0.8f)]
    public float decayingMarkerOpacity = 0.6f;

    [Tooltip("最后快照标记透明度。")]
    [Range(0.05f, 0.5f)]
    public float lastSeenMarkerOpacity = 0.3f;

    [Header("小地图/大地图差异化")]
    [Tooltip("小地图标记缩放到大地图的系数（半径 × multiplier）。")]
    [Range(0.5f, 3f)]
    public float bigmapScaleMultiplier = 2f;

    [Tooltip("小地图标记是否使用简化形状（实心圆 vs 带轮廓圆）。")]
    public bool minimapUseSimplifiedShape = true;

    [Tooltip("大地图标记是否使用轮廓外环。")]
    public bool bigmapShowOutlineRing = true;
}
