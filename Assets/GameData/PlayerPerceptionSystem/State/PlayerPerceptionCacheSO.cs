using UnityEngine;

/// <summary>
/// 玩家感知缓存 ScriptableObject。
/// 记录最近一次可信目标、最后快照位置与短时记忆。
/// 创建路径：右键 → SmallWarThunder → PlayerPerception → 感知缓存
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/PlayerPerception/感知缓存")]
public class PlayerPerceptionCacheSO : ScriptableObject
{
    /// <summary>
    /// 最后快照缓存条目，保存已丢失单位的最后一次可信信息。
    /// </summary>
    [System.Serializable]
    public struct LastSeenCacheEntry
    {
        /// <summary>单位 UID。</summary>
        public string uid;

        /// <summary>单位阵营。</summary>
        public EUnitFaction faction;

        /// <summary>最后可信的世界位置。</summary>
        public Vector3 lastSeenPosition;

        /// <summary>最后可信的世界朝向（度）。</summary>
        public float lastSeenYaw;

        /// <summary>最后一次确认的时间（Time.time）。</summary>
        public float lastSeenTime;

        /// <summary>丢失时的感知强度。</summary>
        [Range(0f, 1f)]
        public float lostAwarenessStrength;
    }

    [Header("缓存数据")]
    [Tooltip("最后快照缓存列表，保存已丢失但有遗留位置的单位。")]
    public LastSeenCacheEntry[] lastSeenCache = new LastSeenCacheEntry[0];

    [Tooltip("当前缓存条目数量。")]
    public int cacheCount;

    [Tooltip("缓存最大容量，由 ConfigSO.maxDecayingTargets 决定。")]
    [Range(1, 100)]
    public int maxCacheCapacity = 30;
}
