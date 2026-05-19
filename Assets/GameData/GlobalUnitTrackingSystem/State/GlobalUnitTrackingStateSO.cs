using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单位追踪快照数据结构，记录单个单位在追踪状态层的当前状态。
/// </summary>
[System.Serializable]
public struct GlobalUnitSnapshot
{
    [Tooltip("单位UID")]
    public string uid;

    [Tooltip("世界坐标位置")]
    public Vector3 worldPosition;

    [Tooltip("朝向角度（度）")]
    public float yaw;

    [Tooltip("快照时间戳（GameTime.time）")]
    public float timestamp;

    [Tooltip("单位阵营")]
    public EUnitFaction faction;

    [Tooltip("是否处于侦测范围内")]
    public bool isInDetectionRange;

    [Tooltip("跟踪状态")]
    public EUnitTrackStatus trackStatus;

    public static GlobalUnitSnapshot CreateDefault(string uid, Vector3 position, float yaw, float time, EUnitFaction faction, bool inRange)
    {
        return new GlobalUnitSnapshot
        {
            uid = uid,
            worldPosition = position,
            yaw = yaw,
            timestamp = time,
            faction = faction,
            isInDetectionRange = inRange,
            trackStatus = inRange ? EUnitTrackStatus.Tracking : EUnitTrackStatus.NotTracked
        };
    }
}

/// <summary>
/// 全局单位跟踪状态 ScriptableObject。
/// 持有当前追踪单位的运行时快照，包括位置、最后快照位置、更新时间、阵营和标记状态。
/// 提供对外查询接口（按UID/阵营/可见性/最近侦测时间筛选）。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → TrackingState
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/跟踪状态")]
public class GlobalUnitTrackingStateSO : ScriptableObject
{
    [Header("状态版本")]
    [Tooltip("状态快照版本号。")]
    public string stateVersion = "1.0.0";

    [Header("当前追踪快照")]
    [Tooltip("当前所有追踪单位的运行时快照列表。")]
    public List<GlobalUnitSnapshot> currentSnapshots = new List<GlobalUnitSnapshot>();

    [Header("查询配置")]
    [Tooltip("按最近侦测时间筛选时使用的默认时间窗口（秒）。")]
    [Range(1f, 60f)]
    public float defaultDetectionTimeWindow = 10f;
}
