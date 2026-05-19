using UnityEngine;

/// <summary>
/// 数据库记录状态枚举，描述单条持久化记录的当前生命周期状态。
/// </summary>
public enum EDatabaseRecordStatus
{
    /// <summary>活跃状态，记录有效且可读取</summary>
    Active = 0,

    /// <summary>脏状态，记录已被修改但未提交</summary>
    Dirty = 1,

    /// <summary>归档状态，记录已被逻辑删除但物理数据保留</summary>
    Archived = 2,

    /// <summary>损坏状态，记录校验失败或数据不可读</summary>
    Corrupted = 3
}
