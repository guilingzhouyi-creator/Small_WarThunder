using UnityEngine;

/// <summary>
/// 数据库批次状态枚举，描述一次保存批次在持久化流程中的状态。
/// </summary>
public enum EDatabaseBatchStatus
{
    /// <summary>开放状态，批次正在接收记录写入</summary>
    Open = 0,

    /// <summary>已提交，批次所有记录已成功落库</summary>
    Committed = 1,

    /// <summary>已回滚，批次写入被撤销</summary>
    RolledBack = 2,

    /// <summary>部分失败，批次中部分记录写入失败但已落库的记录保留</summary>
    PartialFailure = 3
}
