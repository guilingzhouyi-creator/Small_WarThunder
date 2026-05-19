using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最近可见缓存层运行时管理器。
/// 保存敌方单位失去侦测后的最后快照位置、朝向和时间。
/// 只保留"最后一次可信位置"，不输出实时高亮状态，不负责持续追踪。
/// </summary>
public class GlobalUnitLastSeenCacheManager
{
    /// <summary>最后快照记录结构体</summary>
    [System.Serializable]
    public struct LastSeenRecord
    {
        public string uid;
        public Vector3 lastPosition;
        public float lastYaw;
        public float lastSeenTime;
        public EUnitFaction faction;
    }

    private readonly Dictionary<string, LastSeenRecord> _cache;
    private readonly GlobalUnitLastSeenCacheSO _cacheSO;

    /// <summary>缓存保留秒数（从 SO 读取）</summary>
    public float CacheRetentionSeconds => _cacheSO != null ? _cacheSO.cacheRetentionSeconds : 30f;

    public int Count => _cache.Count;

    public GlobalUnitLastSeenCacheManager(GlobalUnitLastSeenCacheSO cacheSO)
    {
        _cacheSO = cacheSO;
        _cache = new Dictionary<string, LastSeenRecord>();
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_LAST_SEEN} 最近可见缓存层初始化完成，保留时长 {CacheRetentionSeconds}s。");
    }

    /// <summary>记录或更新最后快照位置</summary>
    public void RecordLastSeen(string uid, Vector3 position, float yaw, EUnitFaction faction, float timestamp)
    {
        _cache[uid] = new LastSeenRecord
        {
            uid = uid,
            lastPosition = position,
            lastYaw = yaw,
            lastSeenTime = timestamp,
            faction = faction
        };
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_LAST_SEEN} 缓存最后快照 UID={uid} 位置={position} 时间={timestamp:F2}。");
    }

    /// <summary>移除缓存记录（单位重新进入侦测或已销毁）</summary>
    public bool RemoveRecord(string uid)
    {
        if (_cache.Remove(uid))
        {
            Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_LAST_SEEN} 已移除缓存记录 UID={uid}。");
            return true;
        }
        return false;
    }

    /// <summary>按 UID 查询最后快照</summary>
    public bool TryGetRecord(string uid, out LastSeenRecord record)
    {
        return _cache.TryGetValue(uid, out record);
    }

    /// <summary>获取所有缓存记录</summary>
    public IEnumerable<LastSeenRecord> GetAllRecords() => _cache.Values;

    /// <summary>获取所有敌方最后快照标记（用于地图遗留提示）</summary>
    public List<LastSeenRecord> GetEnemyLastSeenRecords()
    {
        var result = new List<LastSeenRecord>();
        foreach (var kv in _cache)
        {
            if (kv.Value.faction == EUnitFaction.Enemy)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>清理过期缓存记录（超过保留时长）</summary>
    public void CleanExpired(float currentTime)
    {
        var expiredUids = new List<string>();
        float threshold = currentTime - CacheRetentionSeconds;
        foreach (var kv in _cache)
        {
            if (kv.Value.lastSeenTime < threshold)
                expiredUids.Add(kv.Key);
        }
        foreach (var uid in expiredUids)
        {
            _cache.Remove(uid);
            Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_LAST_SEEN} 过期缓存已清理 UID={uid}。");
        }
    }

    /// <summary>清空所有缓存</summary>
    public void Clear()
    {
        _cache.Clear();
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_LAST_SEEN} 最近可见缓存已清空。");
    }
}
