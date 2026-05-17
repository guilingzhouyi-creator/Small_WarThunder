using System;
using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

namespace NTaskSystem.Event
{
    /// <summary>
    /// 事件到任务条件的映射规则条目
    /// </summary>
    [Serializable]
    public class EventRuleEntry
    {
        [Tooltip("事件类型")]
        public ETaskEventType eventType;

        [Tooltip("碰撞标签过滤（如'Enemy_Tank'）")]
        public string tagFilter;

        [Tooltip("映射到的目标任务键")]
        public MissionKey targetMissionKey;

        [Tooltip("每次事件对进度的增量（默认1）")]
        public int progressIncrement = 1;

        [Tooltip("自定义条件判定表达式（可选，高级用法）")]
        public string customConditionExpression;

        [Tooltip("该规则是否启用")]
        public bool isEnabled = true;
    }

    /// <summary>
    /// 任务事件规则SO — 定义事件到任务条件的一一映射关系。
    /// 职责：连接"怎么被触发"与"哪个任务受影响"，不直接判断任务是否完成。
    /// 作为Event层与Config层之间的桥接，确保事件标准化后能准确命中目标任务。
    /// </summary>
    [CreateAssetMenu(fileName = "TaskEventRuleSO", menuName = "TaskSystem/TaskEventRuleSO")]
    public class TaskEventRuleSO : ScriptableObject
    {
        [Header("事件规则列表")]
        [SerializeField]
        private List<EventRuleEntry> _eventRules = new List<EventRuleEntry>();

        /// <summary>运行时只读规则集合</summary>
        public IReadOnlyList<EventRuleEntry> EventRules => _eventRules;

        private static bool IsUninitializedMissionKey(MissionKey key)
        {
            return EqualityComparer<MissionKey>.Default.Equals(key, default(MissionKey));
        }

        /// <summary>
        /// 根据事件类型和标签查找所有匹配的规则
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="tag">碰撞标签</param>
        /// <returns>匹配的规则列表（可能多条规则命中同一个任务的不同条件）</returns>
        public List<EventRuleEntry> FindMatchingRules(ETaskEventType eventType, string tag)
        {
            var results = new List<EventRuleEntry>();
            for (int i = 0; i < _eventRules.Count; i++)
            {
                var rule = _eventRules[i];
                if (rule == null || !rule.isEnabled) continue;
                if (rule.eventType != eventType) continue;
                if (!string.IsNullOrEmpty(rule.tagFilter) && rule.tagFilter != tag) continue;

                results.Add(rule);
            }
            return results;
        }

        /// <summary>
        /// 根据任务键获取其所有关联的事件规则
        /// </summary>
        public List<EventRuleEntry> GetRulesForMission(MissionKey missionKey)
        {
            var results = new List<EventRuleEntry>();
            if (IsUninitializedMissionKey(missionKey)) return results;

            for (int i = 0; i < _eventRules.Count; i++)
            {
                var rule = _eventRules[i];
                if (rule == null) continue;
                if (!IsUninitializedMissionKey(rule.targetMissionKey) &&
                    rule.targetMissionKey.category == missionKey.category &&
                    rule.targetMissionKey.subID == missionKey.subID)
                {
                    results.Add(rule);
                }
            }
            return results;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_eventRules == null) return;

            for (int i = 0; i < _eventRules.Count; i++)
            {
                var rule = _eventRules[i];
                if (rule == null) continue;

                if (rule.progressIncrement <= 0)
                {
                    Debug.LogWarning($"[TaskEventRuleSO] 规则 {i} 的 progressIncrement 必须大于0", this);
                    rule.progressIncrement = 1;
                }
            }
        }
#endif
    }
}
