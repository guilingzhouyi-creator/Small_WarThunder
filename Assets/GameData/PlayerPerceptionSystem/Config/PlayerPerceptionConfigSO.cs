using UnityEngine;

/// <summary>
/// 玩家侦查感知配置 ScriptableObject。
/// 定义感知范围、确认阈值、衰减时间、遮挡规则和采样策略。
/// 创建路径：右键 → SmallWarThunder → PlayerPerception → 感知配置
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/PlayerPerception/感知配置")]
public class PlayerPerceptionConfigSO : ScriptableObject
{
    [Header("感知范围")]
    [Tooltip("最大感知距离（米），超过此距离不会被感知。")]
    [Range(50f, 5000f)]
    public float maxPerceptionDistance = 2000f;

    [Tooltip("视锥角度（度），半角。相机前向±此角度内才可被感知。")]
    [Range(10f, 90f)]
    public float visionConeHalfAngle = 45f;

    [Tooltip("边缘感知距离（米），超过此距离但仍在范围内的单位感知强度将按距离衰减。")]
    [Range(0f, 5000f)]
    public float fringeDistanceThreshold = 1500f;

    [Header("确认窗口")]
    [Tooltip("确认时间窗（秒）：单位进入感知范围后需持续可见此时间才转为已确认。")]
    [Range(0f, 5f)]
    public float confirmationWindow = 0.3f;

    [Tooltip("丢失容忍窗（秒）：已确认目标短暂离开视野后在此时间内可维持确认状态。")]
    [Range(0f, 5f)]
    public float loseToleranceWindow = 0.5f;

    [Header("衰减")]
    [Tooltip("衰减持续时间（秒）：单位失去确认后保留弱提示的阶段时长。")]
    [Range(1f, 30f)]
    public float decayDuration = 5f;

    [Tooltip("最后快照保留时间（秒）：衰减结束后仅保留最后快照标记的最大时长。")]
    [Range(5f, 120f)]
    public float lastSeenRetentionTime = 30f;

    [Header("遮挡检测")]
    [Tooltip("是否启用物理遮挡检测（基于Raycast）。")]
    public bool enableOcclusionCheck = true;

    [Tooltip("遮挡检测的LayerMask。")]
    public LayerMask occlusionLayerMask = ~0;

    [Tooltip("遮挡检测采样间隔（秒），不低于采样间隔。")]
    [Range(0.05f, 1f)]
    public float occlusionCheckInterval = 0.2f;

    [Header("采样策略")]
    [Tooltip("感知采样间隔（秒）。")]
    [Range(0.01f, 0.5f)]
    public float sampleInterval = 0.1f;

    [Tooltip("是否启用事件驱动更新。true=事件驱动+定时修正，false=纯轮询。")]
    public bool useEventDrivenUpdate = true;

    [Tooltip("定时修正间隔（秒），补偿丢帧和事件丢失。")]
    [Range(0.2f, 5f)]
    public float periodicCorrectionInterval = 1f;

    [Header("容量")]
    [Tooltip("最大实时感知目标数。")]
    [Range(1, 50)]
    public int maxActiveTargets = 20;

    [Tooltip("最大衰减中目标数。")]
    [Range(1, 50)]
    public int maxDecayingTargets = 30;
}
