using System.Collections;
using UnityEngine;


/// <summary>
/// 游戏关卡管理器，负责管理当前关卡的相关数据和状态，例如关卡索引、玩家出生点、摄像机初始位置等。该组件可以在每个关卡场景中放置一个实例，并通过 Inspector 设置不同的参数，以便在游戏运行时根据当前关卡加载相应的数据和配置。通过将关卡相关的数据和逻辑封装在一个独立的组件中，可以使代码更加清晰和模块化，便于维护和扩展。
/// </summary>
public class GameLevelManager : MonoBehaviour
{
    [SerializeField] private int levelIndex; // 关卡索引，表示这是第几关，初始值为0，表示第一关
    private Coroutine _prepareNarrativeRoutine;
    private bool _hasPreparedLevelStartNarrative;

    public int LevelIndex => levelIndex;
    public int GetLevelIndex() => levelIndex;

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

        if (_prepareNarrativeRoutine != null)
        {
            StopCoroutine(_prepareNarrativeRoutine);
            _prepareNarrativeRoutine = null;
        }
    }

    public void PrepareLevelStartNarrative()
    {
        if (_hasPreparedLevelStartNarrative || _prepareNarrativeRoutine != null)
        {
            return;
        }

        _prepareNarrativeRoutine = StartCoroutine(PrepareLevelStartNarrativeWhenReady());
    }

    private IEnumerator PrepareLevelStartNarrativeWhenReady()
    {
        yield return null;

        if (UIManager.Instance != null)
        {
            var missionUI = FindFirstObjectByType<MissionPannelUIController>(FindObjectsInactive.Include);
            if (missionUI != null)
            {
                missionUI.TriggerLevelStartNarrative();
                _hasPreparedLevelStartNarrative = true;
                Debug.Log($"[GameLevelManager] 触发关卡 {levelIndex} 的开始叙事。");
            }
            else
            {
                Debug.LogWarning("[GameLevelManager] 在场景中找不到 MissionPannelUIController 组件，无法触发关卡开始叙事。请确保场景中有一个对象挂载了 MissionPannelUIController 组件。");
            }
        }

        _prepareNarrativeRoutine = null;
    }

}