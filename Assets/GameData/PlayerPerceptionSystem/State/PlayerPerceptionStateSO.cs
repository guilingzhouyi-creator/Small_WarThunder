using UnityEngine;

/// <summary>
/// 玩家感知状态 ScriptableObject。
/// 记录当前感知中的目标、感知强度、最后确认时间和衰减状态。
/// 存储序列化结构体 PerceivingTargetEntry，供运行时状态管理器读写。
/// 创建路径：右键 → SmallWarThunder → PlayerPerception → 感知状态
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/PlayerPerception/感知状态")]
public class PlayerPerceptionStateSO : ScriptableObject
{
    /// <summary>
    /// 感知中目标条目，记录一个目标在玩家感知系统中的完整状态。
    /// </summary>
    [System.Serializable]
    public struct PerceivingTargetEntry
    {
        /// <summary>单位 UID，来自全局单位跟踪系统。</summary>
        public string uid;

        /// <summary>当前感知状态。</summary>
        public EPlayerAwareness awareness;

        /// <summary>感知强度（0~1），由距离、角度、遮挡等合成。</summary>
        [Range(0f, 1f)]
        public float awarenessStrength;

        /// <summary>最后一次确认的时间（Time.time）。</summary>
        public float lastConfirmedTime;

        /// <summary>进入衰减的时间（Time.time），0 表示未进入衰减。</summary>
        public float decayStartTime;

        /// <summary>最后一次可信的世界位置。</summary>
        public Vector3 lastKnownPosition;

        /// <summary>距离玩家的实时距离（米）。</summary>
        public float distanceToPlayer;

        /// <summary>是否当前被遮挡。</summary>
        public bool isOccluded;
    }

    [Header("运行时数据")]
    [Tooltip("当前所有感知中的目标条目列表。")]
    public PerceivingTargetEntry[] activeTargets = new PerceivingTargetEntry[0];

    [Tooltip("当前实时确认目标数（status==Confirmed）。")]
    public int confirmedCount;

    [Tooltip("当前衰减中目标数（status==Decaying）。")]
    public int decayingCount;

    [Tooltip("最后更新帧号，用于去重/掉帧检测。")]
    public int lastUpdateFrame;
}
