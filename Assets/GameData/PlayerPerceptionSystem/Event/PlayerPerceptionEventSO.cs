using System;
using UnityEngine;

/// <summary>
/// 玩家感知事件 ScriptableObject。
/// 定义感知事件的类型归属和事件承载结构体 PerceptionEventData。
/// 事件数据包含 UID、事件类型、时间戳、位置和感知强度信息。
/// 创建路径：右键 → SmallWarThunder → PlayerPerception → 感知事件
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/PlayerPerception/感知事件")]
public class PlayerPerceptionEventSO : ScriptableObject
{
    /// <summary>
    /// 感知事件数据，承载一次感知事件的完整信息。
    /// </summary>
    [Serializable]
    public struct PerceptionEventData
    {
        /// <summary>单位 UID。</summary>
        public string uid;

        /// <summary>事件类型。</summary>
        public EPlayerPerceptionEventType eventType;

        /// <summary>事件发生时间（Time.time）。</summary>
        public float timestamp;

        /// <summary>事件发生时的世界位置。</summary>
        public Vector3 position;

        /// <summary>感知强度（0~1）。</summary>
        [Range(0f, 1f)]
        public float awarenessStrength;
    }

    [Header("事件缓冲区")]
    [Tooltip("待广播的事件队列（运行时使用，序列化仅为调试）。")]
    public PerceptionEventData[] pendingEvents = new PerceptionEventData[0];
}
