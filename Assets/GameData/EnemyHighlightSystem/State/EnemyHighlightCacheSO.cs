using System;
using UnityEngine;

/// <summary>
/// 敌方高亮缓存 ScriptableObject（最近可见缓存）。
/// 保存敌方目标失去侦测/高亮后的最后快照位置、朝向和时间。
/// 仅保留最后一次可信状态，不承担持续追踪职责。
/// 创建路径：右键 → SmallWarThunder → EnemyHighlight → 高亮缓存
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/EnemyHighlight/高亮缓存")]
public class EnemyHighlightCacheSO : ScriptableObject
{
    [Header("缓存条目")]
    [Tooltip("缓存的敌方目标UID列表。")]
    public string[] cachedUids;

    [Tooltip("缓存的世界坐标列表（与cachedUids一一对应）。")]
    public Vector3[] cachedPositions;

    [Tooltip("缓存的朝向列表（与cachedUids一一对应）。")]
    public Vector3[] cachedForwardDirections;

    [Tooltip("缓存记录时间列表（Unix时间戳毫秒，与cachedUids一一对应）。")]
    public long[] cachedTimestampsMs;

    [Header("统计")]
    [Tooltip("当前已缓存的条目数量。")]
    public int cacheCount;

    [Tooltip("缓存有效时间（秒）。超过此时间的缓存条目视为过期。")]
    public float cacheLifetimeSeconds = 30f;

    /// <summary>
    /// 获取缓存中第一条过期时间（Unix毫秒），若无条目则返回0。
    /// </summary>
    public long GetOldestTimestampMs()
    {
        if (cachedTimestampsMs == null || cachedTimestampsMs.Length == 0) return 0L;
        long oldest = long.MaxValue;
        foreach (long t in cachedTimestampsMs)
        {
            if (t < oldest) oldest = t;
        }
        return oldest == long.MaxValue ? 0L : oldest;
    }
}
