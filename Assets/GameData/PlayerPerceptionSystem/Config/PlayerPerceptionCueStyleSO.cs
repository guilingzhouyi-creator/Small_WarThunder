using UnityEngine;

/// <summary>
/// 感知提示样式 ScriptableObject。
/// 定义世界空间提示、UI 提示和地图投影的颜色、半径与层级策略。
/// 创建路径：右键 → SmallWarThunder → PlayerPerception → 提示样式
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/PlayerPerception/提示样式")]
public class PlayerPerceptionCueStyleSO : ScriptableObject
{
    [Header("实时感知 — 标记颜色")]
    [Tooltip("实时感知敌方标记颜色。")]
    public Color activeEnemyColor = Color.red;

    [Tooltip("实时感知友军标记颜色。")]
    public Color activeAllyColor = Color.green;

    [Tooltip("实时感知中立标记颜色。")]
    public Color activeNeutralColor = Color.yellow;

    [Header("衰减中 — 标记颜色")]
    [Tooltip("衰减中敌方标记颜色（比实时暗）。")]
    public Color decayingEnemyColor = new Color(0.6f, 0.1f, 0.1f, 0.7f);

    [Tooltip("衰减中友军标记颜色。")]
    public Color decayingAllyColor = new Color(0.1f, 0.5f, 0.1f, 0.7f);

    [Tooltip("衰减中中立标记颜色。")]
    public Color decayingNeutralColor = new Color(0.5f, 0.5f, 0.1f, 0.7f);

    [Header("最后快照 — 标记颜色")]
    [Tooltip("最后快照敌方标记颜色（弱提示）。")]
    public Color lastSeenEnemyColor = new Color(0.4f, 0.2f, 0.2f, 0.4f);

    [Tooltip("最后快照友军标记颜色。")]
    public Color lastSeenAllyColor = new Color(0.2f, 0.3f, 0.2f, 0.4f);

    [Tooltip("最后快照中立标记颜色。")]
    public Color lastSeenNeutralColor = new Color(0.3f, 0.3f, 0.1f, 0.4f);

    [Header("小地图 — 标记半径")]
    [Tooltip("实时感知小地图标记半径（像素）。")]
    [Range(2f, 20f)]
    public float minimapActiveRadius = 6f;

    [Tooltip("衰减中小地图标记半径（像素）。")]
    [Range(1f, 15f)]
    public float minimapDecayingRadius = 4f;

    [Tooltip("最后快照小地图标记半径（像素）。")]
    [Range(1f, 10f)]
    public float minimapLastSeenRadius = 3f;

    [Header("大地图 — 标记半径")]
    [Tooltip("实时感知大地图标记半径（像素）。")]
    [Range(3f, 30f)]
    public float bigmapActiveRadius = 12f;

    [Tooltip("衰减中大地图标记半径（像素）。")]
    [Range(2f, 20f)]
    public float bigmapDecayingRadius = 8f;

    [Tooltip("最后快照大地图标记半径（像素）。")]
    [Range(1f, 15f)]
    public float bigmapLastSeenRadius = 5f;

    [Header("标签策略")]
    [Tooltip("小地图是否显示单位类型标签。")]
    public bool minimapShowLabels = false;

    [Tooltip("大地图是否显示单位类型标签。")]
    public bool bigmapShowLabels = true;

    [Tooltip("标签字体大小（大地图）。")]
    [Range(8, 24)]
    public int labelFontSize = 12;

    [Header("衰减动画")]
    [Tooltip("衰减标记是否启用透明度动画（由强到弱）。")]
    public bool decayingAlphaAnimation = true;

    [Tooltip("衰减标记是否启用缩放动画（逐渐缩小）。")]
    public bool decayingScaleAnimation = true;
}
