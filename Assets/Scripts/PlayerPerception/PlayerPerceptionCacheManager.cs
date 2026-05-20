using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家感知缓存管理器。
/// 管理 PlayerPerceptionCacheSO 中最后快照缓存的运行时读写。
/// 当目标衰减完毕时插入最后可信位置快照，采用 LRU 淘汰策略。
/// 缓存仅保留最后可信位置，不追踪实时目标，不负责地图投影。
/// </summary>
public class PlayerPerceptionCacheManager
{
    private readonly PlayerPerceptionCacheSO _cache;
    private readonly PlayerPerceptionConfigSO _config;
    private readonly Dictionary<string, int> _uidToIndex = new Dictionary<string, int>();

    /// <summary>运行时可变长度缓存列表。</summary>
    private readonly List<PlayerPerceptionCacheSO.LastSeenCacheEntry> _cacheList =
        new List<PlayerPerceptionCacheSO.LastSeenCacheEntry>();

    /// <summary>LRU 时间戳，用于淘汰最久未访问条目。</summary>
    private readonly Dictionary<string, float> _lruAccessTime = new Dictionary<string, float>();

    public int CacheCount => _cacheList.Count;

    public PlayerPerceptionCacheManager(PlayerPerceptionCacheSO cache, PlayerPerceptionConfigSO config)
    {
        _cache = cache;
        _config = config;
        SyncFromSO();
    }

    /// <summary>
    /// 尝试插入或更新最后快照缓存条目。
    /// 若 uid 已存在则更新为最新快照；若缓存满则触发 LRU 淘汰。
    /// </summary>
    /// <param name="uid">单位 UID</param>
    /// <param name="faction">阵营</param>
    /// <param name="position">最后可信世界位置</param>
    /// <param name="yaw">最后可信朝向（度）</param>
    /// <param name="strength">丢失时的感知强度</param>
    /// <param name="currentTime">当前时间</param>
    public void TryInsertLastSeen(string uid, EUnitFaction faction, Vector3 position, float yaw, float strength, float currentTime)
    {
        if (_uidToIndex.TryGetValue(uid, out int idx))
        {
            // 更新已有快照
            PlayerPerceptionCacheSO.LastSeenCacheEntry existing = _cacheList[idx];
            existing.lastSeenPosition = position;
            existing.lastSeenYaw = yaw;
            existing.lastSeenTime = currentTime;
            existing.lostAwarenessStrength = strength;
            _cacheList[idx] = existing;
            _lruAccessTime[uid] = currentTime;
            return;
        }

        // 新快照 → 容量检查
        int maxCapacity = _cache.maxCacheCapacity;
        if (_cacheList.Count >= maxCapacity)
        {
            EvictLRU(currentTime);
        }

        // 插入
        PlayerPerceptionCacheSO.LastSeenCacheEntry entry = new PlayerPerceptionCacheSO.LastSeenCacheEntry
        {
            uid = uid,
            faction = faction,
            lastSeenPosition = position,
            lastSeenYaw = yaw,
            lastSeenTime = currentTime,
            lostAwarenessStrength = strength
        };
        _cacheList.Add(entry);
        _uidToIndex[uid] = _cacheList.Count - 1;
        _lruAccessTime[uid] = currentTime;

        Debug.Log($"{PlayerPerceptionConstants.DebugTagCache} 插入最后快照: uid={uid}, pos={position}, time={currentTime}");
    }

    /// <summary>
    /// 查询某单位的最后快照。返回 null 表示不存在。
    /// </summary>
    public PlayerPerceptionCacheSO.LastSeenCacheEntry? TryGetLastSeen(string uid, float currentTime)
    {
        if (_uidToIndex.TryGetValue(uid, out int idx))
        {
            _lruAccessTime[uid] = currentTime;
            return _cacheList[idx];
        }
        return null;
    }

    /// <summary>
    /// 清理超过保留时间的过期条目。
    /// </summary>
    /// <param name="retentionTime">保留时长（秒），来自 config.lastSeenRetentionTime</param>
    /// <param name="currentTime">当前时间</param>
    public void PurgeExpired(float retentionTime, float currentTime)
    {
        for (int i = _cacheList.Count - 1; i >= 0; i--)
        {
            PlayerPerceptionCacheSO.LastSeenCacheEntry entry = _cacheList[i];
            if (currentTime - entry.lastSeenTime > retentionTime)
            {
                Debug.Log($"{PlayerPerceptionConstants.DebugTagCache} 过期清理快照: uid={entry.uid}, age={currentTime - entry.lastSeenTime:F1}s");
                RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 获取指定阵营的所有快照（用于地图投影层按阵营筛选）。
    /// </summary>
    public List<PlayerPerceptionCacheSO.LastSeenCacheEntry> GetByFaction(EUnitFaction faction, float currentTime)
    {
        List<PlayerPerceptionCacheSO.LastSeenCacheEntry> result =
            new List<PlayerPerceptionCacheSO.LastSeenCacheEntry>();
        for (int i = 0; i < _cacheList.Count; i++)
        {
            if (_cacheList[i].faction == faction)
            {
                result.Add(_cacheList[i]);
                _lruAccessTime[_cacheList[i].uid] = currentTime;
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有非过期快照的拷贝。
    /// </summary>
    public List<PlayerPerceptionCacheSO.LastSeenCacheEntry> GetAllValid(float currentTime, float retentionTime)
    {
        List<PlayerPerceptionCacheSO.LastSeenCacheEntry> result =
            new List<PlayerPerceptionCacheSO.LastSeenCacheEntry>();
        for (int i = 0; i < _cacheList.Count; i++)
        {
            if (currentTime - _cacheList[i].lastSeenTime <= retentionTime)
            {
                result.Add(_cacheList[i]);
                _lruAccessTime[_cacheList[i].uid] = currentTime;
            }
        }
        return result;
    }

    /// <summary>
    /// 手动移除指定 uid 的快照（例如单位复活时）。
    /// </summary>
    public bool Remove(string uid)
    {
        if (_uidToIndex.TryGetValue(uid, out int idx))
        {
            RemoveAt(idx);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清空全部缓存。
    /// </summary>
    public void Clear()
    {
        _cacheList.Clear();
        _uidToIndex.Clear();
        _lruAccessTime.Clear();
        FlushToSO();
    }

    /// <summary>
    /// 将运行时状态写回 SO。
    /// </summary>
    public void FlushToSO()
    {
        _cache.lastSeenCache = _cacheList.ToArray();
        _cache.cacheCount = _cacheList.Count;
    }

    private void RemoveAt(int idx)
    {
        string removedUid = _cacheList[idx].uid;
        // 与末尾交换后移除（O(1) 删除）
        int lastIdx = _cacheList.Count - 1;
        if (idx != lastIdx)
        {
            _cacheList[idx] = _cacheList[lastIdx];
            _uidToIndex[_cacheList[idx].uid] = idx;
        }

        _cacheList.RemoveAt(lastIdx);
        _uidToIndex.Remove(removedUid);
        _lruAccessTime.Remove(removedUid);
    }

    /// <summary>
    /// LRU 淘汰：移除访问时间最久的条目。
    /// </summary>
    private void EvictLRU(float currentTime)
    {
        string oldestUid = null;
        float oldestTime = float.MaxValue;

        foreach (KeyValuePair<string, float> kvp in _lruAccessTime)
        {
            if (kvp.Value < oldestTime)
            {
                oldestTime = kvp.Value;
                oldestUid = kvp.Key;
            }
        }

        if (oldestUid != null && _uidToIndex.TryGetValue(oldestUid, out int idx))
        {
            Debug.Log($"{PlayerPerceptionConstants.DebugTagCache} LRU淘汰快照: uid={oldestUid}, age={currentTime - oldestTime:F1}s");
            RemoveAt(idx);
        }
    }

    private void SyncFromSO()
    {
        _cacheList.Clear();
        _uidToIndex.Clear();
        _lruAccessTime.Clear();
        if (_cache.lastSeenCache != null)
        {
            _cacheList.AddRange(_cache.lastSeenCache);
            for (int i = 0; i < _cacheList.Count; i++)
            {
                _uidToIndex[_cacheList[i].uid] = i;
                _lruAccessTime[_cacheList[i].uid] = _cacheList[i].lastSeenTime;
            }
        }
    }
}
