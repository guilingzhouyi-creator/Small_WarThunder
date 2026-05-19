using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跟踪状态层运行时管理器。
/// 持有当前追踪单位的运行时快照，对外提供查询接口。
/// 只保存"当前有效状态"和"最后一次有效状态"的差异，不直接决定地图如何渲染。
/// </summary>
public class GlobalUnitTrackingStateManager
{
    /// <summary>单位跟踪状态结构体</summary>
    [System.Serializable]
    public struct UnitTrackingSnapshot
    {
        public string uid;
        public Vector3 currentPosition;
        public Vector3 lastSeenPosition;
        public float lastUpdateTime;
        public float visibleDuration;
        public EUnitFaction faction;
        public EUnitTrackStatus trackStatus;
        public bool isHighlighted;
    }

    private readonly Dictionary<string, UnitTrackingSnapshot> _snapshots;
    private readonly GlobalUnitTrackingStateSO _stateSO;

    public int Count => _snapshots.Count;

    public GlobalUnitTrackingStateManager(GlobalUnitTrackingStateSO stateSO)
    {
        _stateSO = stateSO;
        _snapshots = new Dictionary<string, UnitTrackingSnapshot>();
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_TRACKING} 跟踪状态层初始化完成。");
    }

    /// <summary>添加或更新跟踪快照</summary>
    public void UpsertSnapshot(string uid, Vector3 position, EUnitFaction faction,
        EUnitTrackStatus trackStatus, bool isHighlighted, float timestamp)
    {
        if (_snapshots.TryGetValue(uid, out var snap))
        {
            snap.currentPosition = position;
            snap.trackStatus = trackStatus;
            snap.isHighlighted = isHighlighted;
            snap.lastUpdateTime = timestamp;
            if (trackStatus == EUnitTrackStatus.Tracking)
            {
                snap.visibleDuration += (timestamp - snap.lastUpdateTime);
            }
            _snapshots[uid] = snap;
        }
        else
        {
            _snapshots[uid] = new UnitTrackingSnapshot
            {
                uid = uid,
                currentPosition = position,
                lastSeenPosition = position,
                lastUpdateTime = timestamp,
                visibleDuration = 0f,
                faction = faction,
                trackStatus = trackStatus,
                isHighlighted = isHighlighted
            };
        }
    }

    /// <summary>更新最后快照位置（单位丢失侦测时记录）</summary>
    public void UpdateLastSeen(string uid, Vector3 lastPosition, float timestamp)
    {
        if (_snapshots.TryGetValue(uid, out var snap))
        {
            snap.lastSeenPosition = lastPosition;
            snap.trackStatus = EUnitTrackStatus.Lost;
            snap.lastUpdateTime = timestamp;
            _snapshots[uid] = snap;
        }
    }

    /// <summary>移除跟踪快照</summary>
    public void RemoveSnapshot(string uid)
    {
        _snapshots.Remove(uid);
    }

    /// <summary>按 UID 查询</summary>
    public bool TryGetSnapshot(string uid, out UnitTrackingSnapshot snapshot)
    {
        return _snapshots.TryGetValue(uid, out snapshot);
    }

    /// <summary>获取所有快照</summary>
    public IEnumerable<UnitTrackingSnapshot> GetAllSnapshots() => _snapshots.Values;

    /// <summary>按阵营筛选</summary>
    public List<UnitTrackingSnapshot> GetByFaction(EUnitFaction faction)
    {
        var result = new List<UnitTrackingSnapshot>();
        foreach (var kv in _snapshots)
        {
            if (kv.Value.faction == faction)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>按是否高亮筛选</summary>
    public List<UnitTrackingSnapshot> GetHighlighted()
    {
        var result = new List<UnitTrackingSnapshot>();
        foreach (var kv in _snapshots)
        {
            if (kv.Value.isHighlighted)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>按跟踪状态筛选（Tracking 表示当前侦测内）</summary>
    public List<UnitTrackingSnapshot> GetByTrackStatus(EUnitTrackStatus status)
    {
        var result = new List<UnitTrackingSnapshot>();
        foreach (var kv in _snapshots)
        {
            if (kv.Value.trackStatus == status)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>获取最近更新快照（最近侦测时间 >= minTimestamp）</summary>
    public List<UnitTrackingSnapshot> GetByMinTime(float minTimestamp)
    {
        var result = new List<UnitTrackingSnapshot>();
        foreach (var kv in _snapshots)
        {
            if (kv.Value.lastUpdateTime >= minTimestamp)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>清空所有快照</summary>
    public void Clear()
    {
        _snapshots.Clear();
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_TRACKING} 跟踪状态层已清空。");
    }
}
