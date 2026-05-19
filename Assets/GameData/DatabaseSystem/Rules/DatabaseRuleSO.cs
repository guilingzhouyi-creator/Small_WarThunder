using UnityEngine;

/// <summary>
/// 数据库写入规则SO，声明持久化写入时的约束：键冲突策略、覆盖策略、数据上限和压缩策略。
/// 由持久化储存系统的PersistenceStorePolicySO引用。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseRule", menuName = "SmallWarThunder/数据库/规则/写入规则表")]
public class DatabaseRuleSO : ScriptableObject
{
    [Header("键冲突处理")]
    [Tooltip("冲突时是否覆盖已有记录")]
    public bool overwriteOnConflict = true;

    [Tooltip("是否允许同批次重复键（同一实体多次写入视为覆盖）")]
    public bool allowDuplicateKeyInBatch = false;

    [Header("覆盖策略")]
    [Tooltip("是否保留覆盖前的旧版本作为归档")]
    public bool archivePreviousVersion = false;

    [Header("数据上限")]
    [Tooltip("静态数据库最大总字节数，超过则拒绝写入")]
    public long maxStorageBytes = 104857600;

    [Header("压缩策略")]
    [Tooltip("写入前是否压缩记录负载")]
    public bool compressPayload = false;

    [Tooltip("压缩阈值（字节），小于此值不压缩")]
    public int compressThresholdBytes = 1024;
}
