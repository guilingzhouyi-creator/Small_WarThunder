using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 最后快照缓存条目，记录单位失去侦测后保留的可信位置和时间。
/// </summary>
[System.Serializable]
public struct GlobalUnitLastSeenEntry
{
    [Tooltip("单位UID")]
    public string uid;

    [Tooltip("最后可信世界坐标位置")]
    public Vector3 lastSeenPosition;

    [Tooltip("最后可信朝向角度（度）")]
    public float lastSeenYaw;

    [Tooltip("最后一次被侦测的时间（GameTime.time）")]
    public float lastSeenTime;

    [Tooltip("单位阵营")]
    public EUnitFaction faction;

    [Tooltip("单位生命状态")]
    public EUnitLifeStatus lifeStatus;

    public static GlobalUnitLastSeenEntry CreateDefault(string uid, Vector3 position, float yaw, float time, EUnitFaction faction, EUnitLifeStatus lifeStatus)
    {
        return new GlobalUnitLastSeenEntry
        {
            uid = uid,
            lastSeenPosition = position,
            lastSeenYaw = yaw,
            lastSeenTime = time,
            faction = faction,
            lifeStatus = lifeStatus
        };
    }
}

/// <summary>
/// 最近可见缓存 ScriptableObject。
/// 保存敌方单位失去侦测后的最后快照位置、最后快照朝向和最后一次更新时间。
/// 只保留最后快照标记所需的可信位置，不输出实时高亮状态。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → LastSeenCache
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/最后快照缓存")]
public class GlobalUnitLastSeenCacheSO : ScriptableObject
{
    [Header("缓存元数据")]
    [Tooltip("缓存版本号。")]
    public string cacheVersion = "1.0.0";

    [Tooltip("最后快照缓存保留时长（秒）。超过该时长的记录会被清理。")]
    [Min(0f)]
    public float cacheRetentionSeconds = 30f;

    [Tooltip("缓存最大容量。超过则移除最旧的条目。")]
    [Range(10, 200)]
    public int maxCacheSize = 100;

    [Header("最后快照数据")]
    [Tooltip("所有已失去侦测单位的最后可信快照列表。")]
    public List<GlobalUnitLastSeenEntry> lastSeenEntries = new List<GlobalUnitLastSeenEntry>();
}
