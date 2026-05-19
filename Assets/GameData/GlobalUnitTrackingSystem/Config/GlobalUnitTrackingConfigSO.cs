using UnityEngine;

/// <summary>
/// 全局单位跟踪系统配置 ScriptableObject。
/// 定义采样间隔、更新策略、阵营过滤和缓存规则等参数。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → TrackingConfig
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/跟踪配置")]
public class GlobalUnitTrackingConfigSO : ScriptableObject
{
    [Header("采样与更新")]
    [Tooltip("跟踪状态采样间隔（秒），越小越精确但性能开销越大。")]
    [Range(0.01f, 1f)]
    public float sampleInterval = 0.1f;

    [Tooltip("更新策略：true=事件驱动为主+周期校准，false=纯周期轮询。")]
    public bool useEventDrivenUpdate = true;

    [Tooltip("周期校准间隔（秒），仅在事件驱动模式下生效，用于补偿丢帧。")]
    [Range(0.1f, 5f)]
    public float periodicCalibrationInterval = 1f;

    [Header("阵营过滤")]
    [Tooltip("是否跟踪友军单位。")]
    public bool trackAllies = true;

    [Tooltip("是否跟踪敌方单位。")]
    public bool trackEnemies = true;

    [Tooltip("是否跟踪中立单位。")]
    public bool trackNeutrals = false;

    [Tooltip("是否跟踪玩家单位本身。")]
    public bool trackPlayer = false;

    [Header("侦测与丢失")]
    [Tooltip("单位失去侦测后保留最后快照的最大时间（秒），超时则移除。")]
    [Range(1f, 300f)]
    public float lastSeenRetentionTime = 30f;

    [Tooltip("侦测去重窗口（秒），同一单位在此时间窗口内重复进入侦测将不触发 Exit→Enter 事件序列。")]
    [Range(0.1f, 5f)]
    public float detectionDedupWindow = 0.5f;

    [Header("缓存规则")]
    [Tooltip("运行时追踪状态快照的最大缓存数量。")]
    [Range(10, 500)]
    public int maxTrackingSnapshotCache = 200;

    [Tooltip("最后快照缓存的最大数量。")]
    [Range(10, 200)]
    public int maxLastSeenCache = 100;

    [Header("性能")]
    [Tooltip("是否启用对象池复用快照数据结构。")]
    public bool enableSnapshotObjectPool = true;

    [Tooltip("对象池初始容量。")]
    [Range(10, 200)]
    public int snapshotPoolInitialCapacity = 50;
}
