using UnityEngine;

/// <summary>
/// 地图渲染配置 ScriptableObject。
/// 控制小地图/大地图的时间间隔、颜色、网格、标记样式。
/// 创建路径：右键 → SmallWarThunder → Map → MapConfig
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/Map/MapConfig")]
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
    public float MiniMapPlayerMarkerRadius = 4f;
    public Color MiniMapPlayerColor = Color.cyan;

    [Header("大地图叠加层")]
    public bool ShowGridOnFullMap = true;
    public Color FullMapGridColor = new Color(1f, 1f, 1f, 0.2f);
    [Tooltip("大地图网格在世界坐标中的间距（米）。")]
    public float FullMapGridSpacing = 100f;
    public float FullMapPlayerMarkerRadius = 6f;
    public Color FullMapPlayerColor = Color.cyan;

    [Header("敌友标记")]
    public Color AllyColor = Color.green;
    public Color EnemyColor = Color.red;
    public float MarkerRadius = 5f;
    [Tooltip("是否在地图标记上显示文字标签。")]
    public bool ShowLabels = false;
}
