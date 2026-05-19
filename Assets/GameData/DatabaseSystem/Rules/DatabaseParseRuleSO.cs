using UnityEngine;

/// <summary>
/// 数据库解析规则SO，声明重载系统在解析原始记录时的行为：兼容策略、重试次数、缺失字段默认值和超时。
/// 由持久化数据重载系统的PersistenceReloadRuleSO引用。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseParseRule", menuName = "SmallWarThunder/数据库/规则/解析规则")]
public class DatabaseParseRuleSO : ScriptableObject
{
    [Header("版本兼容")]
    [Tooltip("版本不匹配时的兼容策略")]
    public EDatabaseCompatibilityStrategy compatibilityStrategy = EDatabaseCompatibilityStrategy.Compatible;

    [Header("解析重试")]
    [Tooltip("单条记录解析失败时的最大重试次数")]
    public int maxParseRetries = 1;

    [Header("缺失字段处理")]
    [Tooltip("记录中缺少字段时是否填充默认值")]
    public bool fillMissingWithDefault = true;

    [Header("超时控制")]
    [Tooltip("整批解析超时时间（秒），超过则中断")]
    public float parseTimeoutSeconds = 10f;
}
