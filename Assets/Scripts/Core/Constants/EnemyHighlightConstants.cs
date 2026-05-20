/// <summary>
/// 敌方高亮系统字符串常量库。
/// 统一管理该系统内所有硬编码字符串引用，包括日志标签、事件名、资源路径等。
/// </summary>
public static class EnemyHighlightConstants
{
    // —— 日志标签 ——
    public const string LOG_TAG_MANAGER = "[EnemyHighlightManager]";
    public const string LOG_TAG_BRIDGE = "[EnemyHighlightTrackerBridge]";
    public const string LOG_TAG_MAP_ADAPTER = "[EnemyHighlightMapAdapter]";
    public const string LOG_TAG_VIEW_ADAPTER = "[EnemyHighlightViewAdapter]";
    public const string LOG_TAG_DECAY = "[EnemyHighlightDecay]";

    // —— 事件名 ——
    public const string EVENT_HIGHLIGHT_STARTED = "HighlightStarted";
    public const string EVENT_HIGHLIGHT_REFRESHED = "HighlightRefreshed";
    public const string EVENT_HIGHLIGHT_ENDED = "HighlightEnded";
    public const string EVENT_POSITION_UPDATED = "HighlightPositionUpdated";
    public const string EVENT_DECAY_STARTED = "DecayStarted";
    public const string EVENT_DECAY_ENDED = "DecayEnded";
    public const string EVENT_LAST_SEEN_RETAINED = "LastSeenRetained";
    public const string EVENT_LAST_SEEN_EVICTED = "LastSeenEvicted";

    // —— 地图标记类型标签 ——
    public const string MARKER_TYPE_REAL_TIME = "RealTimeHighlight";
    public const string MARKER_TYPE_LAST_SEEN = "LastSeenSnapshot";

    // —— SO路径 ——
    public const string SO_PATH_CONFIG = "EnemyHighlightSystem/Config/EnemyHighlightConfigSO";
    public const string SO_PATH_MAP_STYLE = "EnemyHighlightSystem/Config/EnemyHighlightMapStyleSO";
    public const string SO_PATH_MAP_PROJECTION = "EnemyHighlightSystem/Config/EnemyHighlightMapProjectionSO";
    public const string SO_PATH_DECAY = "EnemyHighlightSystem/Config/EnemyHighlightDecaySO";
    public const string SO_PATH_STATE = "EnemyHighlightSystem/State/EnemyHighlightStateSO";
    public const string SO_PATH_CACHE = "EnemyHighlightSystem/State/EnemyHighlightCacheSO";
}
