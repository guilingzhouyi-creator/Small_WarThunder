using System;
using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

namespace NTaskSystem.Config
{
    /// <summary>
    /// 任务条件类型枚举
    /// </summary>
    public enum ETaskConditionType
    {
        Destroy = 0,
        Collect = 1,
        ReachArea = 2,
        Interact = 3,
        Conditional = 4,
    }

    /// <summary>
    /// 任务奖励类型枚举
    /// </summary>
    public enum ETaskRewardType
    {
        None = 0,
        Currency = 1,
        Item = 2,
        Unlock = 3,
        Reputation = 4,
    }

    /// <summary>
    /// 单条任务条件定义 — 静态配置，不含运行时进度
    /// </summary>
    [Serializable]
    public class TaskConditionDefinition
    {
        [Tooltip("触发类型")]
        public ETaskConditionType conditionType;

        [Tooltip("目标单位标签，如敌方坦克Tag")]
        public string targetTag;

        [Tooltip("目标数量")]
        public int targetCount;

        [Tooltip("前置任务键，可为空表示无前置")]
        public string prerequisiteTaskKey;
    }

    /// <summary>
    /// 单条任务奖励定义 — 静态配置
    /// </summary>
    [Serializable]
    public class TaskRewardDefinition
    {
        [Tooltip("奖励类型")]
        public ETaskRewardType rewardType;

        [Tooltip("奖励参数，如货币数量/物品ID")]
        public string rewardParam;

        [Tooltip("是否为一次性发放")]
        public bool isOneShot = true;
    }

    /// <summary>
    /// 任务展示模板 — 静态配置
    /// </summary>
    [Serializable]
    public class TaskDisplayTemplate
    {
        [Tooltip("任务标题")]
        public string title;

        [Tooltip("任务正文模板，{current}和{target}替换为进度")]
        public string bodyTemplate;

        [Tooltip("进度文案模板")]
        public string progressTemplate;

        [Tooltip("完成文案模板")]
        public string completeTemplate;

        [Tooltip("显示优先级，越小越靠前")]
        public int displayPriority;
    }

    /// <summary>
    /// 任务配置外壳 — 只保存任务的静态定义，不保存运行时进度。
    /// 职责：描述"是什么任务"，是策划可编辑的单一真源。
    /// 运行时只读，不反向写回。
    /// </summary>
    [CreateAssetMenu(fileName = "TaskConfigSO", menuName = "TaskSystem/TaskConfigSO")]
    public class TaskConfigSO : ScriptableObject
    {
        [Header("任务定义列表")]
        [SerializeField]
        private List<TaskDefinitionEntry> _taskDefinitions = new List<TaskDefinitionEntry>();

        /// <summary>运行时只读任务定义集合</summary>
        public IReadOnlyList<TaskDefinitionEntry> TaskDefinitions => _taskDefinitions;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下校验配置合法性
        /// </summary>
        private void OnValidate()
        {
            if (_taskDefinitions == null) return;

            var seenKeys = new HashSet<string>();
            for (int i = 0; i < _taskDefinitions.Count; i++)
            {
                var entry = _taskDefinitions[i];
                if (entry == null) continue;

                string key = $"{entry.missionKey.category}_{entry.missionKey.subID}";
                if (seenKeys.Contains(key))
                {
                    Debug.LogWarning($"[TaskConfigSO] 任务键重复: {key}，索引 {i}", this);
                }
                seenKeys.Add(key);

                if (entry.conditions == null || entry.conditions.Count == 0)
                {
                    Debug.LogWarning($"[TaskConfigSO] 任务 {key} 未配置条件", this);
                }
            }
        }
#endif
    }

    /// <summary>
    /// 单条任务定义条目 — 对应PR中任务配置内部的每一条定义。
    /// 包含基础标识、条件信息、奖励信息、展示信息和运行约束。
    /// </summary>
    [Serializable]
    public class TaskDefinitionEntry
    {
        [Header("基础标识")]
        [Tooltip("任务键，由类别+子ID组成")]
        public MissionKey missionKey;

        [Tooltip("任务名")]
        public string taskName;

        [Tooltip("任务描述")]
        [TextArea(3, 5)]
        public string taskDescription;

        [Tooltip("任务分类")]
        public MissionCategory category;

        [Header("条件信息")]
        [Tooltip("条件集合")]
        public List<TaskConditionDefinition> conditions = new List<TaskConditionDefinition>();

        [Tooltip("前置任务键，可为空")]
        public string prerequisiteTaskKey;

        [Tooltip("是否可重复完成")]
        public bool isRepeatable;

        [Header("奖励信息")]
        [Tooltip("奖励集合")]
        public List<TaskRewardDefinition> rewards = new List<TaskRewardDefinition>();

        [Header("展示信息")]
        [Tooltip("展示模板")]
        public TaskDisplayTemplate displayTemplate;

        [Header("运行约束")]
        [Tooltip("是否自动发布（场景加载时自动注册到校验层）")]
        public bool autoPublish;

        [Tooltip("是否需要校验层参与进度追踪")]
        public bool requireVerification = true;

        [Tooltip("是否允许跨场景保留进度")]
        public bool allowCrossScene;
    }
}
