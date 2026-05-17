using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTaskSystem.Event
{
    /// <summary>
    /// 标准化任务事件类型枚举 — 对应PR中击毁、收集、到达区域、交互、条件触发
    /// </summary>
    public enum ETaskEventType
    {
        Destroy = 0,
        Collect = 1,
        ReachArea = 2,
        Interact = 3,
        Conditional = 4,
    }

    /// <summary>
    /// 任务事件参数包装结构 — 用于标准化传递外部事件数据到校验层
    /// </summary>
    [Serializable]
    public struct TaskEventParams
    {
        [Tooltip("事件类型")]
        public ETaskEventType eventType;

        [Tooltip("触发单位UID")]
        public int unitUid;

        [Tooltip("关联标签")]
        public string relatedTag;

        [Tooltip("数量（如本次击杀数）")]
        public int count;

        [Tooltip("扩展参数（JSON或自定义）")]
        public string extraParam;

        public TaskEventParams(ETaskEventType type, int uid, string tag, int count = 1, string extra = null)
        {
            eventType = type;
            unitUid = uid;
            relatedTag = tag;
            this.count = count;
            extraParam = extra;
        }

        public override string ToString()
        {
            return $"[TaskEvent] type={eventType}, uid={unitUid}, tag={relatedTag}, count={count}";
        }
    }

    /// <summary>
    /// 单条任务事件模板条目 — 描述一种标准化事件的类型和参数模板
    /// </summary>
    [Serializable]
    public class TaskEventTemplateEntry
    {
        [Tooltip("事件类型")]
        public ETaskEventType eventType;

        [Tooltip("命中标签匹配规则（如'Enemy_Tank'）")]
        public string tagFilter;

        [Tooltip("触发阈值（如击杀数达到多少才触发一次事件）")]
        public int triggerThreshold = 1;

        [Tooltip("事件冷却时间(秒)，0表示无冷却")]
        public float cooldownSeconds;
    }

    /// <summary>
    /// 任务事件SO — 只描述触发任务所需的事件类型和参数，不直接判断任务是否完成。
    /// 职责：描述"怎么被触发"，作为外部事件输入与任务校验层之间的适配层。
    /// </summary>
    [CreateAssetMenu(fileName = "TaskEventSO", menuName = "TaskSystem/TaskEventSO")]
    public class TaskEventSO : ScriptableObject
    {
        [Header("事件模板列表")]
        [SerializeField]
        private List<TaskEventTemplateEntry> _eventTemplates = new List<TaskEventTemplateEntry>();

        /// <summary>运行时只读事件模板集合</summary>
        public IReadOnlyList<TaskEventTemplateEntry> EventTemplates => _eventTemplates;

        /// <summary>
        /// 将外部原始参数标准化为TaskEventParams
        /// </summary>
        public TaskEventParams Normalize(ETaskEventType type, int unitUid, string tag, int count = 1, string extra = null)
        {
            return new TaskEventParams(type, unitUid, tag, count, extra);
        }
    }
}
