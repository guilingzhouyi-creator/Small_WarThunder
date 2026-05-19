using System;
using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

namespace NTaskSystem.State
{
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum ETaskProgressState
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Claimed = 3,
        Failed = 4,
    }

    /// <summary>
    /// 单条任务的状态快照条目 — 运行时状态，不定义任务规则
    /// </summary>
    [Serializable]
    public class TaskStateEntry
    {
        [Tooltip("任务键")]
        public MissionKey missionKey;

        [Tooltip("当前状态")]
        public ETaskProgressState state = ETaskProgressState.NotStarted;

        [Tooltip("当前完成度（如已击杀数）")]
        public int currentProgress;

        [Tooltip("是否已领奖")]
        public bool hasClaimedReward;

        [Tooltip("最后更新时间")]
        public long lastUpdateTimestamp;

        [Tooltip("跨场景保留标记")]
        public bool preserveCrossScene;
    }

    /// <summary>
    /// 任务状态快照SO — 只保存任务的运行时状态，不定义任务规则。
    /// 职责：描述"当前到哪一步"。
    /// 用于存档、读档、场景切换和任务恢复。
    /// </summary>
    [CreateAssetMenu(fileName = "TaskStateSO", menuName = "SmallWarThunder/任务/状态/状态快照")]
    public class TaskStateSO : ScriptableObject
    {
        [Header("任务状态列表")]
        [SerializeField]
        private List<TaskStateEntry> _taskStates = new List<TaskStateEntry>();

        /// <summary>运行时只读状态集合</summary>
        public IReadOnlyList<TaskStateEntry> TaskStates => _taskStates;

        private static bool IsUninitializedMissionKey(MissionKey key)
        {
            return EqualityComparer<MissionKey>.Default.Equals(key, default(MissionKey));
        }

        /// <summary>
        /// 获取指定任务键的状态条目，未找到返回null
        /// </summary>
        public TaskStateEntry GetState(MissionKey key)
        {
            if (IsUninitializedMissionKey(key)) return null;
            for (int i = 0; i < _taskStates.Count; i++)
            {
                var entry = _taskStates[i];
                if (entry != null && !IsUninitializedMissionKey(entry.missionKey) &&
                    entry.missionKey.category == key.category &&
                    entry.missionKey.subID == key.subID)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// 更新或追加任务状态（运行时调用，内部操作）
        /// </summary>
        public void SetState(TaskStateEntry newState)
        {
            if (newState == null || IsUninitializedMissionKey(newState.missionKey))
            {
                Debug.LogWarning("[TaskStateSO] SetState 参数无效");
                return;
            }

            var existing = GetState(newState.missionKey);
            if (existing != null)
            {
                existing.state = newState.state;
                existing.currentProgress = newState.currentProgress;
                existing.hasClaimedReward = newState.hasClaimedReward;
                existing.lastUpdateTimestamp = newState.lastUpdateTimestamp;
                existing.preserveCrossScene = newState.preserveCrossScene;
            }
            else
            {
                _taskStates.Add(newState);
            }
            Debug.Log($"[TaskStateSO] 状态已更新: {newState.missionKey.category}_{newState.missionKey.subID} -> {newState.state}, 进度 {newState.currentProgress}");
        }

        /// <summary>
        /// 清空所有运行时状态（场景卸载时调用，保留跨场景标记的状态）
        /// </summary>
        public void ClearNonPersistentStates()
        {
            for (int i = _taskStates.Count - 1; i >= 0; i--)
            {
                if (!_taskStates[i].preserveCrossScene)
                {
                    _taskStates.RemoveAt(i);
                }
            }
            Debug.Log($"[TaskStateSO] 已清除非跨场景状态，保留 {_taskStates.Count} 条");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_taskStates == null) return;

            var seenKeys = new HashSet<string>();
            for (int i = 0; i < _taskStates.Count; i++)
            {
                var entry = _taskStates[i];
                if (entry == null || IsUninitializedMissionKey(entry.missionKey)) continue;

                string key = $"{entry.missionKey.category}_{entry.missionKey.subID}";
                if (seenKeys.Contains(key))
                {
                    Debug.LogWarning($"[TaskStateSO] 重复状态键: {key}", this);
                }
                seenKeys.Add(key);
            }
        }
#endif
    }
}
