using UnityEngine;
using NTaskSystem.Event;

/// <summary>
/// 任务目标标记组件 — 挂载到策划拖入 TaskConfigSO 的 targetPrefab 预制体上。
/// 运行时被摧毁时，校验层通过 GetComponent<TaskTargetMarker>() 获取 missionKey + eventType，
/// 匹配任务配置并推进进度。
/// </summary>
public class TaskTargetMarker : MonoBehaviour
{
    [Header("任务标识")]
    [Tooltip("关联的任务键")]
    public string missionKey;

    [Header("事件类型")]
    [Tooltip("触发时的事件类型")]
    public ETaskEventType eventType = ETaskEventType.Destroy;
}
