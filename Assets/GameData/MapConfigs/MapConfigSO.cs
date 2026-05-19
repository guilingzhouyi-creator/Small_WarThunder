using UnityEngine;

/// <summary>
/// 地图渲染配置 ScriptableObject。
/// 控制小地图/大地图的时间间隔、网格、玩家指示器和标记样式。
/// 创建路径：右键 → SmallWarThunder → Map → MapConfig
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/UI/地图/地图配置")]
public class MapConfigSO : ScriptableObject
{
    [Header("时间间隔")]
    [Tooltip("地图数据采集和绘制的时间间隔（秒），100ms 表示每秒更新 10 次。")]
    public float UpdateInterval = 0.1f;

    [Header("小地图叠加层")]
    public bool ShowGridOnMiniMap = true;
    public Color MiniMapGridColor = new Color(1f, 1f, 1f, 0.15f);
    [Tooltip("小地图网格在世界坐标中的间距（米）。")]
    public float MiniMapGridSpacing = 50f;
    public float MiniMapPlayerIndicatorRadius = 4f;
    public Color MiniMapPlayerIndicatorColor = Color.cyan;

    [Header("大地图叠加层")]
    public bool ShowGridOnFullMap = true;
    public Color FullMapGridColor = new Color(1f, 1f, 1f, 0.2f);
    [Tooltip("大地图网格在世界坐标中的间距（米）。")]
    public float FullMapGridSpacing = 100f;
    public float FullMapPlayerIndicatorRadius = 6f;
    public Color FullMapPlayerIndicatorColor = Color.cyan;

    [Header("敌友与通用标记样式")]
    public Color AllyMarkerColor = Color.green;
    public Color EnemyMarkerColor = Color.red;
    public float DefaultMarkerDisplayRadius = 5f;
    [Tooltip("是否在地图标记上显示文字标签。")]
    public bool ShowMarkerLabels = false;
}
