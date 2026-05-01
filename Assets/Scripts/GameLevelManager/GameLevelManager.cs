using SmallWar.Data;
using UnityEngine;

/// <summary>
/// 区域执行子总控。
/// 挂载在超大地图子区域预制体上，负责该区域内：
///   1. 持有 MissionRegistrySystem，生成 SubtitlePackage 传递给 UI 层
///   2. （未来）驱动敌人逻辑、提交情报包/数据包到 GameManager
/// LevelStreamingEngine 加载地图时通过 LevelMissionMarker 触发注册到 GameManager。
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

    private bool _hasPreparedLevelStartNarrative;

    public int LevelIndex => _levelIndex;

    private void OnEnable()
    {
        GameManager.Instance?.RegisterRuntimeGameLevel(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterRuntimeGameLevel(this);
        }
    }

    /// <summary>
    /// 准备关卡开场字幕：
    /// 从 MissionRegistrySystem 获取 SubtitlePackage，传递给 MissionPannelUIController，
    /// 由 GlobalSubtitleEngine 渲染。
    /// </summary>
    public void PrepareLevelStartNarrative()
    {
        if (_hasPreparedLevelStartNarrative)
        {
            return;
        }

        if (_missionRegistry == null)
        {
            Debug.LogWarning($"[GameLevelManager] 关卡 {_levelIndex} 未配置 MissionRegistrySystem");
            _hasPreparedLevelStartNarrative = true;
            return;
        }

        SubtitlePackage package = _missionRegistry.GetPackageSequence(
            _narrativeCategory, _narrativeStartId, _narrativeEndId, _narrativeChannel
        );

        if (package == null)
        {
            Debug.LogWarning($"[GameLevelManager] 关卡 {_levelIndex} 未能生成字幕包");
            _hasPreparedLevelStartNarrative = true;
            return;
        }

        var missionUI = FindFirstObjectByType<MissionPannelUIController>(FindObjectsInactive.Include);
        if (missionUI != null)
        {
            missionUI.PlayNarrative(package);
            _hasPreparedLevelStartNarrative = true;
            Debug.Log($"[GameLevelManager] 触发关卡 {_levelIndex} 的开始叙事。");
        }
        else
        {
            Debug.LogWarning("[GameLevelManager] 在场景中找不到 MissionPannelUIController 组件，无法触发关卡开始叙事。");
        }
    }
}
