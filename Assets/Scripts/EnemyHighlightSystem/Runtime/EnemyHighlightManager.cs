using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌方高亮管理器（MonoBehaviour）。
/// 核心职责：驱动高亮生命周期状态机，负责创建、刷新、过期与移除高亮目标。
/// 通过 EnemyHighlightTrackerBridge 订阅 GlobalUnitEventBus 获取真源数据，
/// 输出标准化的 MapMarkerData 列表和世界空间高亮事件。
/// </summary>
[DisallowMultipleComponent]
public class EnemyHighlightManager : MonoBehaviour
{
    [Header("配置引用")]
    [SerializeField] private EnemyHighlightConfigSO _config;
    [SerializeField] private EnemyHighlightMapStyleSO _mapStyle;
    [SerializeField] private EnemyHighlightMapProjectionSO _mapProjection;
    [SerializeField] private EnemyHighlightDecaySO _decay;
    [SerializeField] private EnemyHighlightStateSO _state;
    [SerializeField] private EnemyHighlightCacheSO _cache;

    [Header("玩家引用")]
    [SerializeField] private Transform _playerTransform;

    // —— 运行时状态 ——
    private readonly Dictionary<string, HighlightEntry> _entries = new Dictionary<string, HighlightEntry>(64);
    private readonly List<string> _removalBuffer = new List<string>(32);
    private readonly List<MapMarkerData> _markerOutputBuffer = new List<MapMarkerData>(32);
    private EnemyHighlightTrackerBridge _bridge;
    private float _lastRefreshTime;
    private float _lastCorrectionTime;

    // —— 事件输出 ——
    public event Action<List<MapMarkerData>> onMarkerDataUpdated;
    public event Action<string, Vector3, EEnemyHighlightPhase> onHighlightPhaseChanged;
    public event Action<string, Vector3> onWorldSpaceHighlightRefreshed;
    public event Action<string, float> onHighlightAlphaUpdated;

    void Awake()
    {
        _bridge = new EnemyHighlightTrackerBridge(
            onDetected: HandleUnitDetected,
            onLost: HandleUnitLost,
            onLifeStatusChanged: HandleLifeStatusChanged,
            onPositionUpdated: HandleUnitPositionUpdated);

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_MANAGER} 已初始化。");
    }

    void OnDestroy()
    {
        _bridge?.Dispose();
        _entries.Clear();
        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_MANAGER} 已释放。");
    }

    void Update()
    {
        if (_config == null) return;

        float now = Time.time;

        // 定时刷新
        if (now - _lastRefreshTime >= _config.refreshInterval)
        {
            _lastRefreshTime = now;
            TickRefresh(now);
        }

        // 定时修正（补偿丢帧和事件丢失）
        if (_config.useEventDrivenUpdate && now - _lastCorrectionTime >= _config.periodicCorrectionInterval)
        {
            _lastCorrectionTime = now;
            TickCorrection(now);
        }

        // 每帧驱动衰减
        TickDecay(now);
    }

    // ========== GlobalUnitEventBus 回调 ==========

    private void HandleUnitDetected(string uid, Vector3 worldPosition)
    {
        if (_entries.TryGetValue(uid, out var entry))
        {
            // 已存在 → 刷新
            entry.CurrentPosition = worldPosition;
            entry.LastDetectedTime = Time.time;

            if (entry.Phase == EEnemyHighlightPhase.Decaying || entry.Phase == EEnemyHighlightPhase.LastSeenOnly)
            {
                TransitionTo(entry, EEnemyHighlightPhase.Highlighting);
            }
        }
        else
        {
            // 新建
            entry = new HighlightEntry
            {
                Uid = uid,
                CurrentPosition = worldPosition,
                EnteredTime = Time.time,
                LastDetectedTime = Time.time,
                Phase = EEnemyHighlightPhase.Highlighting
            };
            _entries[uid] = entry;

            if (_config.enableWorldSpaceHighlight)
            {
                onHighlightPhaseChanged?.Invoke(uid, worldPosition, EEnemyHighlightPhase.Highlighting);
            }
        }
    }

    private void HandleUnitLost(string uid, Vector3 lastKnownPosition)
    {
        if (!_entries.TryGetValue(uid, out var entry)) return;

        entry.CurrentPosition = lastKnownPosition;
        if (entry.Phase == EEnemyHighlightPhase.Highlighting)
        {
            TransitionTo(entry, EEnemyHighlightPhase.Decaying);
        }
    }

    private void HandleLifeStatusChanged(string uid, EUnitLifeStatus newLifeStatus)
    {
        if (!_entries.TryGetValue(uid, out var entry)) return;

        if (newLifeStatus == EUnitLifeStatus.Destroyed)
        {
            // 目标被消灭 → 可选保留最后快照
            if (_config != null && entry.Phase != EEnemyHighlightPhase.Evicted)
            {
                TransitionTo(entry, EEnemyHighlightPhase.LastSeenOnly);
            }
        }
    }

    private void HandleUnitPositionUpdated(string uid, Vector3 newPosition)
    {
        if (!_entries.TryGetValue(uid, out var entry)) return;

        entry.CurrentPosition = newPosition;
        if (entry.Phase == EEnemyHighlightPhase.Highlighting || entry.Phase == EEnemyHighlightPhase.Decaying)
        {
            onWorldSpaceHighlightRefreshed?.Invoke(uid, newPosition);
        }
    }

    // ========== Tick 驱动 ==========

    private void TickRefresh(float now)
    {
        if (_entries.Count == 0) return;

        _removalBuffer.Clear();
        _markerOutputBuffer.Clear();

        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            float elapsed = now - entry.LastDetectedTime;

            switch (entry.Phase)
            {
                case EEnemyHighlightPhase.Highlighting:
                    if (elapsed > _config.highlightDuration)
                    {
                        TransitionTo(entry, EEnemyHighlightPhase.Decaying);
                    }
                    else
                    {
                        AddRealTimeMarkerToBuffer(entry, 1f);
                    }
                    break;

                case EEnemyHighlightPhase.Decaying:
                    float decayProgress = elapsed / Mathf.Max(0.001f, _decay != null ? _decay.decayDurationSeconds : _config.decayDuration);
                    if (decayProgress >= 1f)
                    {
                        TransitionTo(entry, EEnemyHighlightPhase.LastSeenOnly);
                    }
                    else
                    {
                        float alpha = 1f - decayProgress;
                        AddRealTimeMarkerToBuffer(entry, alpha);
                    }
                    break;

                case EEnemyHighlightPhase.LastSeenOnly:
                    float retentionElapsed = now - entry.DecayEndTime;
                    if (retentionElapsed > (_config.lastSeenRetentionTime))
                    {
                        _removalBuffer.Add(kvp.Key);
                    }
                    else
                    {
                        AddLastSeenMarkerToBuffer(entry);
                    }
                    break;
            }
        }

        // 输出地图标记
        onMarkerDataUpdated?.Invoke(_markerOutputBuffer);

        // 移除过期条目
        for (int i = 0; i < _removalBuffer.Count; i++)
        {
            string uid = _removalBuffer[i];
            _entries.Remove(uid);
            UpdateStateCounts();
        }

        // 容量裁剪
        TrimIfNeeded();

        UpdateStateCounts();
    }

    private void TickCorrection(float now)
    {
        // 定时修正：对超过 highlightDuration 但仍然在 Highlighting 状态的目标强制刷新
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            float elapsed = now - entry.LastDetectedTime;
            if (entry.Phase == EEnemyHighlightPhase.Highlighting && elapsed > _config.highlightDuration)
            {
                TransitionTo(entry, EEnemyHighlightPhase.Decaying);
            }
        }
    }

    private void TickDecay(float now)
    {
        if (_decay == null) return;

        // 衰减 Tick：根据 SO 曲线更新世界空间透明度
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            if (entry.Phase != EEnemyHighlightPhase.Decaying) continue;

            float elapsed = now - entry.LastDetectedTime;
            float progress = elapsed / Mathf.Max(0.001f, _decay.decayDurationSeconds);
            float alpha = _decay.opacityDecayCurve != null ? _decay.opacityDecayCurve.Evaluate(progress) : (1f - progress);
            onHighlightAlphaUpdated?.Invoke(entry.Uid, alpha);
        }
    }

    // ========== 状态转换 ==========

    private void TransitionTo(HighlightEntry entry, EEnemyHighlightPhase newPhase)
    {
        EEnemyHighlightPhase oldPhase = entry.Phase;
        entry.Phase = newPhase;

        switch (newPhase)
        {
            case EEnemyHighlightPhase.Decaying:
                entry.DecayStartTime = Time.time;
                break;
            case EEnemyHighlightPhase.LastSeenOnly:
                entry.DecayEndTime = Time.time;
                break;
            case EEnemyHighlightPhase.Evicted:
                _removalBuffer.Add(entry.Uid);
                break;
        }

        if (_config.enableWorldSpaceHighlight)
        {
            onHighlightPhaseChanged?.Invoke(entry.Uid, entry.CurrentPosition, newPhase);
        }

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_MANAGER} 状态转换 [{entry.Uid}]: {oldPhase} → {newPhase}");
    }

    // ========== 标记生成 ==========

    private void AddRealTimeMarkerToBuffer(HighlightEntry entry, float alpha)
    {
        if (!_config.enableMapProjection || _mapStyle == null) return;

        MapMarkerData marker = new MapMarkerData
        {
            MapWorldPosition = entry.CurrentPosition,
            DisplayColor = ColorWithAlpha(_mapStyle.liveMarkerColor, _mapStyle.liveMarkerOpacity * alpha),
            DisplayLabel = null,
            DisplayRadius = _mapStyle.minimapLiveRadius
        };
        _markerOutputBuffer.Add(marker);
    }

    private void AddLastSeenMarkerToBuffer(HighlightEntry entry)
    {
        if (!_config.enableMapProjection || _mapStyle == null) return;

        MapMarkerData marker = new MapMarkerData
        {
            MapWorldPosition = entry.CurrentPosition,
            DisplayColor = ColorWithAlpha(_mapStyle.lastSeenMarkerColor, _mapStyle.lastSeenMarkerOpacity),
            DisplayLabel = null,
            DisplayRadius = _mapStyle.minimapLastSeenRadius
        };
        _markerOutputBuffer.Add(marker);
    }

    // ========== 工具方法 ==========

    private void TrimIfNeeded()
    {
        int maxLive = _config.maxHighlightTargets;
        int maxDecaying = _config.maxDecayingTargets;
        int maxLastSeen = _config.maxLastSeenCacheTargets;

        int liveCount = 0, decayCount = 0, lastSeenCount = 0;
        foreach (var kvp in _entries)
        {
            switch (kvp.Value.Phase)
            {
                case EEnemyHighlightPhase.Highlighting: liveCount++; break;
                case EEnemyHighlightPhase.Decaying: decayCount++; break;
                case EEnemyHighlightPhase.LastSeenOnly: lastSeenCount++; break;
            }
        }

        if (liveCount > maxLive) TrimPhase(EEnemyHighlightPhase.Highlighting, liveCount - maxLive);
        if (decayCount > maxDecaying) TrimPhase(EEnemyHighlightPhase.Decaying, decayCount - maxDecaying);
        if (lastSeenCount > maxLastSeen) TrimPhase(EEnemyHighlightPhase.LastSeenOnly, lastSeenCount - maxLastSeen);
    }

    private void TrimPhase(EEnemyHighlightPhase phase, int excess)
    {
        var candidates = new List<KeyValuePair<string, HighlightEntry>>();
        foreach (var kvp in _entries)
        {
            if (kvp.Value.Phase == phase) candidates.Add(kvp);
        }

        if (_config.prioritizeByDistance && _playerTransform != null)
        {
            candidates.Sort((a, b) =>
            {
                float distA = Vector3.Distance(a.Value.CurrentPosition, _playerTransform.position);
                float distB = Vector3.Distance(b.Value.CurrentPosition, _playerTransform.position);
                return distA.CompareTo(distB);
            });
        }
        else
        {
            candidates.Sort((a, b) => a.Value.EnteredTime.CompareTo(b.Value.EnteredTime));
        }

        for (int i = candidates.Count - 1; i >= 0 && excess > 0; i--, excess--)
        {
            _entries.Remove(candidates[i].Key);
        }
    }

    private void UpdateStateCounts()
    {
        if (_state == null) return;

        int liveCount = 0, decayCount = 0, lastSeenCount = 0;
        foreach (var kvp in _entries)
        {
            switch (kvp.Value.Phase)
            {
                case EEnemyHighlightPhase.Highlighting: liveCount++; break;
                case EEnemyHighlightPhase.Decaying: decayCount++; break;
                case EEnemyHighlightPhase.LastSeenOnly: lastSeenCount++; break;
            }
        }

        _state.highlightedCount = liveCount;
        _state.decayingCount = decayCount;
        _state.lastSeenCount = lastSeenCount;
    }

    private static Color ColorWithAlpha(Color baseColor, float alpha)
    {
        return new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
    }

    // ========== 内部数据结构 ==========

    private class HighlightEntry
    {
        public string Uid;
        public Vector3 CurrentPosition;
        public float EnteredTime;
        public float LastDetectedTime;
        public float DecayStartTime;
        public float DecayEndTime;
        public EEnemyHighlightPhase Phase = EEnemyHighlightPhase.Unknown;
    }
}
