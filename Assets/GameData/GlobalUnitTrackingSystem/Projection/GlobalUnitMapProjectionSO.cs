using UnityEngine;

/// <summary>
/// 地图投影标记数据结构，用于从追踪状态转换为地图可用的标记数据。
/// 区分"实时高亮 Marker"和"最后快照 Marker"。
/// </summary>
[System.Serializable]
public struct GlobalUnitMarkerData
{
    [Tooltip("单位UID")]
    public string uid;

    [Tooltip("投影到地图平面的坐标（归一化或世界坐标，由投影策略决定）")]
    public Vector2 mapPosition;

    [Tooltip("单位朝向角度（度），用于态势表达")]
    public float yaw;

    [Tooltip("是否为实时高亮标记（true=实时目标，false=最后快照标记）")]
    public bool isLiveHighlight;

    [Tooltip("单位阵营")]
    public EUnitFaction faction;

    [Tooltip("标记生成时间戳")]
    public float timestamp;

    public static GlobalUnitMarkerData CreateLive(string uid, Vector2 pos, float yaw, EUnitFaction faction, float time)
    {
        return new GlobalUnitMarkerData
        {
            uid = uid,
            mapPosition = pos,
            yaw = yaw,
            isLiveHighlight = true,
            faction = faction,
            timestamp = time
        };
    }

    public static GlobalUnitMarkerData CreateSnapshot(string uid, Vector2 pos, float yaw, EUnitFaction faction, float time)
    {
        return new GlobalUnitMarkerData
        {
            uid = uid,
            mapPosition = pos,
            yaw = yaw,
            isLiveHighlight = false,
            faction = faction,
            timestamp = time
        };
    }
}

/// <summary>
/// 地图投影配置 ScriptableObject。
/// 定义小地图/大地图的投影规则与落点策略。
/// 不直接绘制 UI，只输出 MapSnapshot 兼容的数据。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → MapProjection
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/地图投影")]
public class GlobalUnitMapProjectionSO : ScriptableObject
{
    [Header("投影策略")]
    [Tooltip("投影坐标空间：true=归一化[0,1]，false=世界坐标。")]
    public bool useNormalizedCoordinates = false;

    [Tooltip("小地图投影缩放系数，控制标记密度。")]
    [Range(0.1f, 2f)]
    public float miniMapScaleFactor = 0.5f;

    [Tooltip("大地图投影缩放系数，控制标记密度。")]
    [Range(0.1f, 2f)]
    public float fullMapScaleFactor = 1f;

    [Tooltip("最后快照标记的衰减时间（秒），超出后标记透明度降至最低。")]
    [Range(1f, 60f)]
    public float lastSeenFadeDuration = 15f;

    [Header("距离裁剪")]
    [Tooltip("小地图最大显示距离（世界单位），超出则裁剪。")]
    [Range(10f, 5000f)]
    public float miniMapMaxDistance = 500f;

    [Tooltip("大地图最大显示距离（世界单位），超出则裁剪。0=无限制。")]
    [Range(0f, 20000f)]
    public float fullMapMaxDistance = 0f;

    [Header("地图边界")]
    [Tooltip("小地图边界范围（世界单位），标记超出此范围则裁剪。")]
    public Vector2 miniMapBounds = new Vector2(1000f, 1000f);

    [Tooltip("大地图边界范围（世界单位），标记超出此范围则裁剪。")]
    public Vector2 fullMapBounds = new Vector2(5000f, 5000f);
}
