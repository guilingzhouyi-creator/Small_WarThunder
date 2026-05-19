using UnityEngine;

/// <summary>
/// 数据库兼容策略枚举，描述解析器在遇到不同版本记录时的处理方式。
/// </summary>
public enum EDatabaseCompatibilityStrategy
{
    /// <summary>严格模式，版本不匹配则记录不可用，拒绝解析</summary>
    Strict = 0,

    /// <summary>兼容模式，版本不匹配时尝试字段映射和默认值补全</summary>
    Compatible = 1,

    /// <summary>忽略版本，跳过版本检查直接解析，适用于临时调试</summary>
    IgnoreVersion = 2,

    /// <summary>默认值模式，版本不匹配时丢弃旧数据使用完整默认值</summary>
    DefaultFill = 3
}
