using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家感知状态管理器。
/// 管理 PlayerPerceptionStateSO 的运行时读写，负责目标状态机转换和事件广播。
/// 自身不执行感知计算（由 PlayerPerceptionEngine 负责），仅消费评估结果并更新状态。
/// </summary>
public class PlayerPerceptionStateManager
{
    private readonly PlayerPerceptionStateSO _state;
    private readonly PlayerPerceptionConfigSO _config;
    private readonly Dictionary<string, int> _uidToIndex = new Dictionary<string, int>();

    /// <summary>活跃目标列表（运行时可变长度，SO中为固定大小序列化槽）。</summary>
    private readonly List<PlayerPerceptionStateSO.PerceivingTargetEntry> _activeList =
        new List<PlayerPerceptionStateSO.PerceivingTargetEntry>();

    public int ConfirmedCount { get; private set; }
    public int DecayingCount { get; private set; }

    public PlayerPerceptionStateManager(PlayerPerceptionStateSO state, PlayerPerceptionConfigSO config)
    {
        _state = state;
        _config = config;
        SyncFromSO();
    }

    /// <summary>
    /// 批量更新感知状态。
    /// 外部逐帧调用，传入本帧所有评估结果。
    /// </summary>
    /// <param name="results">uid → PerceptionResult 映射</param>
    /// <param name="currentTime">Time.time</param>
    public void UpdatePerception(Dictionary<string, PlayerPerceptionEngine.PerceptionResult> results, float currentTime)
    {
        RebuildIndex();
        ProcessNewResults(results, currentTime);
        ProcessDecay(currentTime);
        UpdateCounters();
        FlushToSO();
    }

    /// <summary>
    /// 获取当前所有已确认（Confirmed）的目标条目。
    /// </summary>
    public List<PlayerPerceptionStateSO.PerceivingTargetEntry> GetConfirmedTargets()
    {
        List<PlayerPerceptionStateSO.PerceivingTargetEntry> result =
            new List<PlayerPerceptionStateSO.PerceivingTargetEntry>();
        for (int i = 0; i < _activeList.Count; i++)
        {
            if (_activeList[i].awareness == EPlayerAwareness.Confirmed)
            {
                result.Add(_activeList[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有衰减中（Decaying）的目标条目。
    /// </summary>
    public List<PlayerPerceptionStateSO.PerceivingTargetEntry> GetDecayingTargets()
    {
        List<PlayerPerceptionStateSO.PerceivingTargetEntry> result =
            new List<PlayerPerceptionStateSO.PerceivingTargetEntry>();
        for (int i = 0; i < _activeList.Count; i++)
        {
            if (_activeList[i].awareness == EPlayerAwareness.Decaying)
            {
                result.Add(_activeList[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有活跃目标条目（含Confirming/Confirmed/Decaying）。
    /// </summary>
    public List<PlayerPerceptionStateSO.PerceivingTargetEntry> GetAllActiveTargets()
    {
        return new List<PlayerPerceptionStateSO.PerceivingTargetEntry>(_activeList);
    }

    private void ProcessNewResults(Dictionary<string, PlayerPerceptionEngine.PerceptionResult> results, float currentTime)
    {
        foreach (KeyValuePair<string, PlayerPerceptionEngine.PerceptionResult> kvp in results)
        {
            string uid = kvp.Key;
            PlayerPerceptionEngine.PerceptionResult result = kvp.Value;

            if (_uidToIndex.TryGetValue(uid, out int idx))
            {
                // 已有条目 → 更新
                PlayerPerceptionStateSO.PerceivingTargetEntry entry = _activeList[idx];
                UpdateExistingEntry(ref entry, result, currentTime);
                _activeList[idx] = entry;
            }
            else
            {
                // 新目标 → 创建条目，进入Confirming
                PlayerPerceptionStateSO.PerceivingTargetEntry newEntry = CreateEntry(uid, result, currentTime);
                _activeList.Add(newEntry);
                EnqueueEvent(uid, EPlayerPerceptionEventType.Discovered, result.worldPosition, result.strength, currentTime);
            }
        }
    }

    private void UpdateExistingEntry(
        ref PlayerPerceptionStateSO.PerceivingTargetEntry entry,
        PlayerPerceptionEngine.PerceptionResult result,
        float currentTime)
    {
        float previousStrength = entry.awarenessStrength;
        entry.lastKnownPosition = result.worldPosition;
        entry.distanceToPlayer = result.distance;
        entry.isOccluded = result.isOccluded;
        entry.awarenessStrength = result.strength;

        EPlayerAwareness previousState = entry.awareness;

        switch (entry.awareness)
        {
            case EPlayerAwareness.Confirming:
                float elapsed = currentTime - entry.lastConfirmedTime;
                if (elapsed >= _config.confirmationWindow)
                {
                    entry.awareness = EPlayerAwareness.Confirmed;
                    entry.lastConfirmedTime = currentTime;
                    EnqueueEvent(entry.uid, EPlayerPerceptionEventType.Confirmed, result.worldPosition, result.strength, currentTime);
                }
                break;

            case EPlayerAwareness.Confirmed:
                entry.lastConfirmedTime = currentTime;
                // 强度变化超过阈值才发更新事件，减少事件洪水
                if (Mathf.Abs(previousStrength - result.strength) > 0.05f)
                {
                    EnqueueEvent(entry.uid, EPlayerPerceptionEventType.Refreshed, result.worldPosition, result.strength, currentTime);
                }
                break;

            case EPlayerAwareness.Decaying:
                // 衰减中重新获得信号 → 回切至Confirming，重置确认计时
                entry.awareness = EPlayerAwareness.Confirming;
                entry.lastConfirmedTime = currentTime;
                entry.decayStartTime = 0f;
                EnqueueEvent(entry.uid, EPlayerPerceptionEventType.Recovered, result.worldPosition, result.strength, currentTime);
                break;
        }
    }

    private void ProcessDecay(float currentTime)
    {
        for (int i = _activeList.Count - 1; i >= 0; i--)
        {
            PlayerPerceptionStateSO.PerceivingTargetEntry entry = _activeList[i];

            switch (entry.awareness)
            {
                case EPlayerAwareness.Confirmed:
                    // 失去信号超时 → 进入衰减
                    float lostDuration = currentTime - entry.lastConfirmedTime;
                    if (lostDuration >= _config.loseToleranceWindow)
                    {
                        entry.awareness = EPlayerAwareness.Decaying;
                        entry.decayStartTime = currentTime;
                        _activeList[i] = entry;
                        EnqueueEvent(entry.uid, EPlayerPerceptionEventType.Lost, entry.lastKnownPosition, entry.awarenessStrength, currentTime);
                    }
                    break;

                case EPlayerAwareness.Decaying:
                    float decayDuration = currentTime - entry.decayStartTime;
                    if (decayDuration >= _config.decayDuration)
                    {
                        // 衰减完毕 → 移除
                        EnqueueEvent(entry.uid, EPlayerPerceptionEventType.Removed, entry.lastKnownPosition, 0f, currentTime);
                        _activeList.RemoveAt(i);
                    }
                    else
                    {
                        // 线性衰减强度
                        float decayFactor = 1f - (decayDuration / _config.decayDuration);
                        entry.awarenessStrength = entry.awarenessStrength * decayFactor;
                        _activeList[i] = entry;
                    }
                    break;
            }
        }

        // 容量控制：若活跃目标超上限，移除最弱的Decaying条目
        int maxTargets = _config.maxActiveTargets > 0 ? _config.maxActiveTargets : PlayerPerceptionConstants.DefaultMaxTargets;
        while (_activeList.Count > maxTargets)
        {
            int weakestIdx = -1;
            float weakestStrength = float.MaxValue;
            for (int i = 0; i < _activeList.Count; i++)
            {
                if (_activeList[i].awareness == EPlayerAwareness.Decaying &&
                    _activeList[i].awarenessStrength < weakestStrength)
                {
                    weakestStrength = _activeList[i].awarenessStrength;
                    weakestIdx = i;
                }
            }

            if (weakestIdx >= 0)
            {
                PlayerPerceptionStateSO.PerceivingTargetEntry removed = _activeList[weakestIdx];
                EnqueueEvent(removed.uid, EPlayerPerceptionEventType.Removed, removed.lastKnownPosition, 0f, currentTime);
                _activeList.RemoveAt(weakestIdx);
            }
            else
            {
                // 无Decaying条目可淘汰，强制移除最末尾
                PlayerPerceptionStateSO.PerceivingTargetEntry removed = _activeList[_activeList.Count - 1];
                _activeList.RemoveAt(_activeList.Count - 1);
                EnqueueEvent(removed.uid, EPlayerPerceptionEventType.Removed, removed.lastKnownPosition, 0f, currentTime);
            }
        }
    }

    private static PlayerPerceptionStateSO.PerceivingTargetEntry CreateEntry(
        string uid,
        PlayerPerceptionEngine.PerceptionResult result,
        float currentTime)
    {
        return new PlayerPerceptionStateSO.PerceivingTargetEntry
        {
            uid = uid,
            awareness = EPlayerAwareness.Confirming,
            awarenessStrength = result.strength,
            lastConfirmedTime = currentTime,
            decayStartTime = 0f,
            lastKnownPosition = result.worldPosition,
            distanceToPlayer = result.distance,
            isOccluded = result.isOccluded
        };
    }

    private static void EnqueueEvent(string uid, EPlayerPerceptionEventType eventType, Vector3 position, float strength, float time)
    {
        PlayerPerceptionEventBus.Enqueue(new PlayerPerceptionEventSO.PerceptionEventData
        {
            uid = uid,
            eventType = eventType,
            timestamp = time,
            position = position,
            awarenessStrength = strength
        });
    }

    private void RebuildIndex()
    {
        _uidToIndex.Clear();
        for (int i = 0; i < _activeList.Count; i++)
        {
            if (!string.IsNullOrEmpty(_activeList[i].uid))
            {
                _uidToIndex[_activeList[i].uid] = i;
            }
        }
    }

    private void UpdateCounters()
    {
        ConfirmedCount = 0;
        DecayingCount = 0;
        for (int i = 0; i < _activeList.Count; i++)
        {
            if (_activeList[i].awareness == EPlayerAwareness.Confirmed) ConfirmedCount++;
            if (_activeList[i].awareness == EPlayerAwareness.Decaying) DecayingCount++;
        }
    }

    private void SyncFromSO()
    {
        _activeList.Clear();
        if (_state.activeTargets != null)
        {
            _activeList.AddRange(_state.activeTargets);
        }
        UpdateCounters();
    }

    private void FlushToSO()
    {
        _state.activeTargets = _activeList.ToArray();
        _state.confirmedCount = ConfirmedCount;
        _state.decayingCount = DecayingCount;
        _state.lastUpdateFrame = Time.frameCount;
    }
}
