/// <summary>
/// 玩家侦查与感知系统所有字符串常量。
/// 包括Debug标签、事件键名和默认数值常量。
/// </summary>
public static class PlayerPerceptionConstants
{
    // ─── 调试标签 ───
    public const string DebugTag = "[PlayerPerception]";
    public const string DebugTagEngine = "[PlayerPerception_Engine]";
    public const string DebugTagState = "[PlayerPerception_State]";
    public const string DebugTagCache = "[PlayerPerception_Cache]";

    // ─── 默认容量 ───
    public const int DefaultMaxTargets = 32;
    public const int DefaultMaxDecaying = 16;
    public const int DefaultOcclusionSamples = 3;

    // ─── 档位阈值 ───
    public const float CriticalThreshold = 0.90f;
    public const float HighThreshold = 0.75f;
    public const float ElevatedThreshold = 0.50f;
    public const float GuardedThreshold = 0.25f;
}
