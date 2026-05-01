using SmallWar.Data;
using UnityEngine;

public enum MissionNarrativeTriggerMode
{
    OnceOnEnter = 0,
    RepeatWhileInside = 1,
}

/// <summary>
/// 区域执行子总控。
/// 挂载在超大地图子区域预制体上，负责该区域内：
///   1. 持有 MissionRegistrySystem，按玩家进入/停留任务区时生成 SubtitlePackage
///   2. 维护当前区域的叙事运行时状态，交给 MissionNarrativeRuntime + MissionPannelUIController + GlobalSubtitleEngine 渲染
///   3. （未来）驱动敌人逻辑、提交情报包/数据包到 GameManager
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

    private GameObject _trackedPlayerTank;
    private SubtitlePackage _runtimeNarrativePackage;
    private string _activeRegionId;
    private bool _isPlayerInside;
    private bool _hasTriggeredOnce;
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
        TryDispatchNarrative(true);
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
        TryDispatchNarrative(false);
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
        _hasTriggeredOnce = false;
        _lastDispatchTime = float.NegativeInfinity;
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

        if (_runtimeNarrativePackage != null && !_runtimeNarrativePackage.HasFinished)
        {
            MissionNarrativeRuntime.PublishNarrative(this, ActiveRegionId, _runtimeNarrativePackage);
            return;
        }

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
        MissionNarrativeRuntime.PublishNarrative(this, ActiveRegionId, _runtimeNarrativePackage);
        Debug.Log($"[GameLevelManager] 玩家进入任务区域 {ActiveRegionId}，触发关卡 {_levelIndex} 的叙事包。", this);
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
