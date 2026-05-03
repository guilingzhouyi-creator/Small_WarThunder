using System;
using SmallWar.Data;
using UnityEngine;

public enum MissionNarrativeTriggerMode
{
    OnceOnEnter = 0,
    RepeatWhileInside = 1,
}

/// <summary>
/// 触发策略：控制字幕/CG 是一次性还是可重复触发。
/// </summary>
public enum TriggerPolicy
{
    /// <summary>一次性触发，标记后不再触发。</summary>
    OnceOnly = 0,
    /// <summary>每次进入区域都触发。</summary>
    Repeatable = 1,
}

/// <summary>
/// 区域执行子总控。
/// 挂载在超大地图子区域预制体上，负责该区域内：
///   1. 持有 MissionRegistrySystem，按玩家进入/停留任务区时生成 SubtitlePackage
///   2. 持有可选 CgClip，在字幕前播放一次性 CG
///   3. 维护当前区域的叙事运行时状态，交给 MissionNarrativeRuntime + MissionPannelUIController + GlobalSubtitleEngine 渲染
///   4. （未来）驱动敌人逻辑、提交情报包/数据包到 GameManager
/// </summary>
public class GameLevelManager : MonoBehaviour
{
    [Header("关卡标识")]
    [SerializeField] private int _levelIndex;

    [Header("字幕中央注册表（ScriptableObject 资产）")]
    [SerializeField] private MissionRegistrySystem _missionRegistry;

    [Header("开场叙事配置")]
    [SerializeField] private MissionCategory _narrativeCategory = MissionCategory.Training;
    [SerializeField] private int _narrativeStartId = 100;
    [SerializeField] private int _narrativeEndId = 105;
    [SerializeField] private SubtitleChannel _narrativeChannel = SubtitleChannel.Dialogue;

    [Header("运行时触发配置")]
    [SerializeField] private MissionNarrativeTriggerMode _triggerMode = MissionNarrativeTriggerMode.RepeatWhileInside;
    [SerializeField] private float _repeatInterval = 6f;
    [SerializeField] private float _repeatAfterFinishDelay = 2f;

    [Header("字幕触发策略")]
    [Tooltip("OnceOnly：只触发一次，退出再进入不重播；Repeatable：每次进入都重播。")]
    [SerializeField] private TriggerPolicy _narrativePolicy = TriggerPolicy.Repeatable;

    [Header("CG 播放配置")]
    [Tooltip("CG 片段（可选），配置后将在首次字幕前播放。")]
    [SerializeField] private CgClip _cgClip;

    [Tooltip("CG 触发策略（固定为 OnceOnly，CG 只播一次）。")]
    [SerializeField] private TriggerPolicy _cgPolicy = TriggerPolicy.OnceOnly;

    private GameObject _trackedPlayerTank;
    private SubtitlePackage _runtimeNarrativePackage;
    private string _activeRegionId;
    private bool _isPlayerInside;
    private bool _hasTriggeredOnce;
    private bool _cgTriggered;
    private bool _narrativeFinishCallbackWired;
    private float _narrativeFinishedTime = float.NegativeInfinity;
    private float _lastDispatchTime = float.NegativeInfinity;

    public int LevelIndex => _levelIndex;
    public string ActiveRegionId => string.IsNullOrWhiteSpace(_activeRegionId) ? gameObject.name : _activeRegionId;

    private void OnDisable()
    {
        _trackedPlayerTank = null;
        _isPlayerInside = false;
        MissionNarrativeRuntime.DetachOwner(this);
    }

    public void NotifyPlayerEnteredRegion(GameObject playerTank, string regionId)
    {
        if (!CanTrackPlayer(playerTank))
        {
            return;
        }

        _trackedPlayerTank = playerTank;
        _activeRegionId = ResolveRegionId(regionId);
        _isPlayerInside = true;
        TryDispatchEntrySequence(true);
    }

    public void NotifyPlayerStayedRegion(GameObject playerTank, string regionId)
    {
        if (!CanTrackPlayer(playerTank))
        {
            return;
        }

        _trackedPlayerTank = playerTank;
        _activeRegionId = ResolveRegionId(regionId);
        _isPlayerInside = true;
        TryDispatchEntrySequence(false);
    }

    public void NotifyPlayerExitedRegion(GameObject playerTank, string regionId)
    {
        if (_trackedPlayerTank != null && playerTank != null && _trackedPlayerTank != playerTank)
        {
            return;
        }

        _activeRegionId = ResolveRegionId(regionId);
        _trackedPlayerTank = null;
        _isPlayerInside = false;
        _runtimeNarrativePackage = null;
        _narrativeFinishCallbackWired = false;
        _narrativeFinishedTime = float.NegativeInfinity;

        // CG 标记永不重置（一次性）
        // 字幕根据策略决定是否重置
        if (_narrativePolicy == TriggerPolicy.Repeatable)
        {
            _hasTriggeredOnce = false;
            _lastDispatchTime = float.NegativeInfinity;
        }

        MissionNarrativeRuntime.StopNarrative(this);
    }

    private bool CanTrackPlayer(GameObject playerTank)
    {
        if (playerTank == null)
        {
            return false;
        }

        if (_trackedPlayerTank != null && _trackedPlayerTank != playerTank)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 统一的进入序列调度：CG → 字幕。
    /// </summary>
    private void TryDispatchEntrySequence(bool isInitialEntry)
    {
        if (!_isPlayerInside)
        {
            return;
        }

        // 第一步：检查是否需要播放 CG
        if (ShouldPlayCg())
        {
            PlayCgThenNarrative();
            return;
        }

        // 第二步：检查是否需要播放字幕
        TryDispatchNarrative(isInitialEntry);
    }

    private bool ShouldPlayCg()
    {
        if (_cgClip == null || _cgClip.videoClip == null)
        {
            return false;
        }

        if (_cgTriggered)
        {
            return false;
        }

        return true;
    }

    private void PlayCgThenNarrative()
    {
        if (CgPlaybackSystem.Instance == null)
        {
            Debug.LogWarning($"[GameLevelManager] 关卡 {_levelIndex} 需要播放 CG 但 CgPlaybackSystem 实例不存在。跳过 CG，直接播字幕。", this);
            _cgTriggered = true;
            TryDispatchNarrative(true);
            return;
        }

        _cgTriggered = true;
        _hasTriggeredOnce = true;
        _lastDispatchTime = Time.unscaledTime;

        CgPlaybackSystem.Instance.PlayCg(_cgClip, () =>
        {
            // CG 播完后继续播字幕
            if (_isPlayerInside)
            {
                TryDispatchNarrative(true);
            }
        });
    }

    private void TryDispatchNarrative(bool isInitialEntry)
    {
        if (!_isPlayerInside)
        {
            return;
        }

        if (_missionRegistry == null)
        {
            Debug.LogWarning($"[GameLevelManager] 关卡 {_levelIndex} 未配置 MissionRegistrySystem", this);
            return;
        }

        if (_runtimeNarrativePackage == null)
        {
            if (!CanCreateNextNarrativePackage(isInitialEntry))
            {
                return;
            }

            _runtimeNarrativePackage = BuildNarrativePackage();
            if (_runtimeNarrativePackage == null)
            {
                Debug.LogWarning($"[GameLevelManager] 关卡 {_levelIndex} 未能生成字幕包", this);
                return;
            }

            _lastDispatchTime = Time.unscaledTime;
            _hasTriggeredOnce = true;
            WireNarrativeFinishCallback(_runtimeNarrativePackage);
            MissionNarrativeRuntime.PublishNarrative(this, ActiveRegionId, _runtimeNarrativePackage);
            Debug.Log($"[GameLevelManager] 玩家进入任务区域 {ActiveRegionId}，触发关卡 {_levelIndex} 的叙事包。", this);
            return;
        }

        if (!_runtimeNarrativePackage.HasFinished)
        {
            return;
        }

        if (_triggerMode != MissionNarrativeTriggerMode.RepeatWhileInside)
        {
            return;
        }

        if (Time.unscaledTime - _narrativeFinishedTime < Mathf.Max(0.1f, _repeatAfterFinishDelay))
        {
            return;
        }

        _runtimeNarrativePackage.ResetProgress();
        _lastDispatchTime = Time.unscaledTime;
        MissionNarrativeRuntime.PublishNarrative(this, ActiveRegionId, _runtimeNarrativePackage);
    }

    private void WireNarrativeFinishCallback(SubtitlePackage package)
    {
        if (package == null || _narrativeFinishCallbackWired)
        {
            return;
        }

        package.OnFinished = () =>
        {
            _narrativeFinishedTime = Time.unscaledTime;
        };

        _narrativeFinishCallbackWired = true;
    }

    private bool CanCreateNextNarrativePackage(bool isInitialEntry)
    {
        if (!_hasTriggeredOnce)
        {
            return true;
        }

        if (_triggerMode == MissionNarrativeTriggerMode.OnceOnEnter)
        {
            return false;
        }

        if (isInitialEntry)
        {
            return true;
        }

        return Time.unscaledTime - _lastDispatchTime >= Mathf.Max(0.1f, _repeatInterval);
    }

    private SubtitlePackage BuildNarrativePackage()
    {
        return _missionRegistry.GetPackageSequence(
            _narrativeCategory,
            _narrativeStartId,
            _narrativeEndId,
            _narrativeChannel);
    }

    private string ResolveRegionId(string regionId)
    {
        return string.IsNullOrWhiteSpace(regionId) ? gameObject.name : regionId;
    }
}
