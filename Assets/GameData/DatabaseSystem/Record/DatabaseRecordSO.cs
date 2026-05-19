using UnityEngine;

/// <summary>
/// 数据库记录SO，持久化层中最小的可保存数据单元。
/// 持久化储存系统在记录构建层创建，重载系统在解析层填充。
/// 每条记录必须有唯一键、类型标识和版本信息。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseRecord", menuName = "SmallWarThunder/数据库/记录/记录实体")]
public class DatabaseRecordSO : ScriptableObject
{
    [Header("记录标识")]
    [Tooltip("记录唯一键：格式为 {实体类型}-{实体ID}-{状态类型}")]
    public string recordKey;

    [Tooltip("记录类型标识，对应业务实体类型")]
    public string entityType;

    [Tooltip("记录版本号，用于兼容性判断")]
    public string recordVersion;

    [Header("批次归属")]
    [Tooltip("所属批次号")]
    public string batchId;

    [Header("时间戳")]
    [Tooltip("记录创建时间戳")]
    public long createTimestamp;

    [Tooltip("记录最后修改时间戳")]
    public long modifyTimestamp;

    [Header("负载数据")]
    [Tooltip("序列化后的业务数据负载（JSON或二进制）")]
    public string payload;

    [Tooltip("负载数据哈希值，用于完整性校验")]
    public string payloadHash;

    [Header("记录状态")]
    [Tooltip("记录当前状态")]
    public EDatabaseRecordStatus status;
}
