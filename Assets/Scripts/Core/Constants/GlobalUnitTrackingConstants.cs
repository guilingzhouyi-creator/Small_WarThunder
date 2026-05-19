/// <summary>
/// 全局单位跟踪系统字符串常量库。
/// 统一管理该系统内所有硬编码字符串引用，包括日志标签、事件名、预制体路径等。
/// </summary>
public static class GlobalUnitTrackingConstants
{
    // —— 日志标签 ——
    public const string LOG_TAG_REGISTRY = "[GlobalUnitRegistry]";
    public const string LOG_TAG_TRACKING = "[GlobalUnitTrackingSystem]";
    public const string LOG_TAG_LAST_SEEN = "[GlobalUnitLastSeenCache]";
    public const string LOG_TAG_PROJECTION = "[GlobalUnitMapProjection]";
    public const string LOG_TAG_EVENT_BUS = "[GlobalUnitEventBus]";
    public const string LOG_TAG_DETECTION_ADAPTER = "[DetectionInputAdapter]";

    // —— 事件类型标识 ——
    public const string EVENT_UNIT_SPAWNED = "UnitSpawned";
    public const string EVENT_UNIT_DESTROYED = "UnitDestroyed";
    public const string EVENT_UNIT_DETECTED = "UnitDetected";
    public const string EVENT_UNIT_LOST = "UnitLost";
    public const string EVENT_UNIT_POSITION_UPDATED = "UnitPositionUpdated";
    public const string EVENT_UNIT_STATE_CHANGED = "UnitStateChanged";

    // —— 地图标记标签 ——
    public const string MARKER_TAG_REAL_TIME = "RealTimeHighlight";
    public const string MARKER_TAG_LAST_SEEN = "LastSeenSnapshot";

    // —— 预制体/资源路径 ——
    public const string SO_PATH_CONFIG = "GlobalUnitTrackingSystem/Config/GlobalUnitTrackingConfigSO";
    public const string SO_PATH_HIGHLIGHT_POLICY = "GlobalUnitTrackingSystem/Config/GlobalUnitHighlightPolicySO";
    public const string SO_PATH_REGISTRY = "GlobalUnitTrackingSystem/Registry/GlobalUnitRegistrySO";
    public const string SO_PATH_TRACKING_STATE = "GlobalUnitTrackingSystem/State/GlobalUnitTrackingStateSO";
    public const string SO_PATH_LAST_SEEN_CACHE = "GlobalUnitTrackingSystem/State/GlobalUnitLastSeenCacheSO";
    public const string SO_PATH_MAP_PROJECTION = "GlobalUnitTrackingSystem/Projection/GlobalUnitMapProjectionSO";
    public const string SO_PATH_MARKER_STYLE = "GlobalUnitTrackingSystem/Projection/GlobalUnitMarkerStyleSO";
    public const string SO_PATH_EVENT = "GlobalUnitTrackingSystem/Event/GlobalUnitTrackingEventSO";
}
