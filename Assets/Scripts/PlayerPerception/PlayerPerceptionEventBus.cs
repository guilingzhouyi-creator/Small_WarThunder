using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家感知事件总线。
/// 负责接收感知状态变化并向外广播事件，提供给敌方高亮系统、地图系统等消费者订阅。
/// 内部维护事件队列，按帧批量派发，避免逐事件高频调用。
/// </summary>
public static class PlayerPerceptionEventBus
{
    private static readonly Queue<PlayerPerceptionEventSO.PerceptionEventData> eventQueue =
        new Queue<PlayerPerceptionEventSO.PerceptionEventData>();

    /// <summary>单个感知事件广播。</summary>
    public static event Action<PlayerPerceptionEventSO.PerceptionEventData> onPerceptionEvent;

    /// <summary>当帧所有事件批量派发（帧末）。</summary>
    public static event Action<List<PlayerPerceptionEventSO.PerceptionEventData>> onBatchDispatch;

    private static readonly List<PlayerPerceptionEventSO.PerceptionEventData> batchBuffer =
        new List<PlayerPerceptionEventSO.PerceptionEventData>();

    /// <summary>
    /// 入队一个感知事件，将在帧末统一派发。
    /// </summary>
    /// <param name="eventData">感知事件数据。</param>
    public static void Enqueue(PlayerPerceptionEventSO.PerceptionEventData eventData)
    {
        if (string.IsNullOrEmpty(eventData.uid))
        {
            Debug.LogWarning("[PlayerPerceptionEventBus] 入队失败：uid为空");
            return;
        }

        eventQueue.Enqueue(eventData);
    }

    /// <summary>
    /// 每帧末调用，批量派发所有队列事件。
    /// </summary>
    public static void DispatchBatch()
    {
        if (eventQueue.Count == 0)
        {
            return;
        }

        batchBuffer.Clear();
        while (eventQueue.Count > 0)
        {
            batchBuffer.Add(eventQueue.Dequeue());
        }

        for (int i = 0; i < batchBuffer.Count; i++)
        {
            onPerceptionEvent?.Invoke(batchBuffer[i]);
        }

        onBatchDispatch?.Invoke(batchBuffer);
    }

    /// <summary>
    /// 清空事件队列和所有订阅。
    /// </summary>
    public static void Clear()
    {
        eventQueue.Clear();
        batchBuffer.Clear();
        onPerceptionEvent = null;
        onBatchDispatch = null;
    }
}
