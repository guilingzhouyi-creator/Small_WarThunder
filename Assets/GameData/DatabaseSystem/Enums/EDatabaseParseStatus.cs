using UnityEngine;

/// <summary>
/// 数据库解析状态枚举，描述重载过程中对原始记录的解析阶段。
/// </summary>
public enum EDatabaseParseStatus
{
    /// <summary>空闲状态，无解析操作进行中</summary>
    Idle = 0,

    /// <summary>解析中，正在将原始记录转换为运行时格式</summary>
    Parsing = 1,

    /// <summary>解析成功，所有记录已转换为可消费结构</summary>
    Success = 2,

    /// <summary>解析失败，存在无法转换的记录</summary>
    Failed = 3
}
