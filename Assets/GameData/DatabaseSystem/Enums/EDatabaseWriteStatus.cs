using UnityEngine;

/// <summary>
/// 数据库写入状态枚举，描述数据库引擎当前的写入阶段。
/// </summary>
public enum EDatabaseWriteStatus
{
    /// <summary>空闲状态，无写入操作进行中</summary>
    Idle = 0,

    /// <summary>写入中，正在将记录落入物理存储</summary>
    Writing = 1,

    /// <summary>写入成功，批次所有记录已落库</summary>
    Success = 2,

    /// <summary>写入失败，存在未落库的记录</summary>
    Failed = 3
}
