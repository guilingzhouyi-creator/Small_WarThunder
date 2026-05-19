using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 跟踪事件数据结构，封装单个追踪事件的完整信息。
/// </summary>
[System.Serializable]
public struct GlobalUnitTrackingEventData
{
    [Tooltip("相关单位UID")]
    public string unitUid;

    [Tooltip("事件类型")]
    public EUnitTrackingEventType eventType;

    [Tooltip("事件发生时的世界坐标")]
    public Vector3 worldPosition;

    [Tooltip("事件发生时的朝向")]
    public float yaw;

    [Tooltip("事件发生时间戳（GameTime.time）")]
    public float timestamp;

    [Tooltip("单位阵营")]
    public EUnitFaction faction;

    [Tooltip("事件负载键值对，用于附加数据（如生命状态变更时的前后状态）")]
    public Dictionary<string, string> payload;

    public static GlobalUnitTrackingEventData CreateDefault(
        string uid,
        EUnitTrackingEventType type,
        Vector3 pos,
        float yaw,
        EUnitFaction faction)
    {
        return new GlobalUnitTrackingEventData
        {
            unitUid = uid,
            eventType = type,
            worldPosition = pos,
            yaw = yaw,
            timestamp = Time.time,
            faction = faction,
            payload = new Dictionary<string, string>()
        };
    }
}

/// <summary>
/// 全局单位跟踪事件 ScriptableObject。
/// 定义生成、销毁、侦测、丢失、更新等事件类型广播配置。
/// 广播只负责通知状态变化，不直接完成地图绘制和高亮样式选择。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → TrackingEvent
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/跟踪事件")]
public class GlobalUnitTrackingEventSO : ScriptableObject
{
    [Header("事件版本")]
    [Tooltip("事件系统版本号。")]
    public string eventVersion = "1.0.0";

    [Header("事件队列")]
    [Tooltip("最大同时缓存的事件数量。超过则丢弃最旧事件。")]
    [Range(10, 500)]
    public int maxEventQueueSize = 100;

    [Tooltip("事件广播间隔（秒），用于批量广播。")]
    [Range(0f, 0.5f)]
    public float eventBroadcastInterval = 0.05f;

    [Header("事件类型过滤")]
    [Tooltip("是否广播单位生成事件。")]
    public bool broadcastUnitSpawned = true;

    [Tooltip("是否广播单位销毁事件。")]
    public bool broadcastUnitDestroyed = true;

    [Tooltip("是否广播进入侦测事件。")]
    public bool broadcastEnterDetection = true;

    [Tooltip("是否广播退出侦测事件。")]
    public bool broadcastExitDetection = true;

    [Tooltip("是否广播位置更新事件。")]
    public bool broadcastPositionUpdated = true;

    [Tooltip("是否广播生命状态变更事件。")]
    public bool broadcastLifeStatusChanged = true;

    [Tooltip("是否广播高亮状态变更事件。")]
    public bool broadcastHighlightChanged = true;

    [Tooltip("是否广播最后快照更新事件。")]
    public bool broadcastLastSeenUpdated = true;

    [Header("位置更新节流")]
    [Tooltip("同一单位位置更新的最小间隔（秒），低于此间隔的更新将被丢弃。")]
    [Range(0.01f, 1f)]
    public float positionUpdateThrottle = 0.1f;
}
