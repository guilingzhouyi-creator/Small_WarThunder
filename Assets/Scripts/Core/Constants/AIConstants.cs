/// <summary>
/// AI系统使用的所有字符串常量（FSM状态名、黑板键、行为树节点名等）
/// 所有AI相关代码通过此类引用字符串，禁止硬编码。
/// </summary>
public static class AIConstants
{
    // ─── FSM 两层七态状态名称 ───
    // 常规层
    public const string StateWatch = "Watch";           // 监视态
    public const string StateWatchBuffer = "WatchBuffer";     // 监视缓冲态（过渡）
    public const string StateSuspicious = "Suspicious";      // 移动可疑态
    public const string StateLockBuffer = "LockBuffer";      // 锁定缓冲态
    public const string StateRandomAttack = "RandomAttack";    // 随机攻击态
    public const string StateDead = "Dead";            // 死亡损毁态
    // 追加层
    public const string StateSpecial = "Special";         // 特殊态（防御/撤退/支援）

    // ─── AI_Blackboard 键名 ───
    public const string BbKeyCurrentState = "CurrentState";
    public const string BbKeyTargetEnemy = "TargetEnemy";
    public const string BbKeyCurrentAwareness = "CurrentAwareness";    // 0-1
    public const string BbKeyIsAwarenessLocked = "IsAwarenessLocked";
    public const string BbKeyLastKnownPosition = "LastKnownPosition";
    public const string BbKeyHealth = "Health";
    public const string BbKeySuspensionOffset = "SuspensionOffset";
    public const string BbKeySuspensionVelocity = "SuspensionVelocity";
    public const string BbKeySuspiciousTimer = "SuspiciousTimer";     // 可疑态剩余秒
    public const string BbKeyLockBufferDone = "LockBufferDone";      // 锁定准备完成
    public const string BbKeyFireCommand = "FireCommand";         // bool：行为树触发开火

    // ─── 行为树节点名 ───
    public const string BtNodeCheckEnemy = "CheckEnemy";
    public const string BtNodeMoveTo = "MoveTo";
    public const string BtNodeAttack = "Attack";
    public const string BtNodeWait = "Wait";

    // ─── 感知系统常量 ───
    public const string PerceptionLayerName = "AI_Perception";
    public const int MaxPerceptionResults = 32;
    public const int PerceptionSliceCount = 4;
    public const float DetectionRangeFactor = 1.15f;   // 1.15倍范围

    // ─── 调试标签 ───
    public const string DebugTagAI = "[AI_System]";
    public const string DebugTagFSM = "[AI_FSM]";
    public const string DebugTagBlackboard = "[AI_Blackboard]";
    public const string DebugTagPerception = "[AI_Perception]";
    public const string DebugTagBehaviorTree = "[AI_BehaviorTree]";
    public const string DebugTagMove = "[AI_Move]";
    public const string DebugTagWeapon = "[AI_Weapon]";
    public const string DebugTagTurret = "[AI_Turret]";
    public const string DebugTagSuspension = "[AI_Suspension]";
    public const string DebugTagFire = "[AI_Fire]";
}
