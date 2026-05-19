using UnityEngine;

/// <summary>
/// 数据库清单SO，记录一次保存批次包含的所有记录键、版本和类型信息。
/// 持久化储存系统在写入完成后更新清单，重载系统在读取前通过清单
/// 确定读取范围、增量更新和兼容分支。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseManifest", menuName = "SmallWarThunder/数据库/清单/清单文件")]
public class DatabaseManifestSO : ScriptableObject
{
    [Header("清单标识")]
    [Tooltip("清单唯一标识符，通常与批次号保持一致")]
    public string manifestId;

    [Tooltip("清单名称")]
    public string manifestName;

    [Header("版本信息")]
    [Tooltip("数据库版本号")]
    public string databaseVersion;

    [Tooltip("清单创建时间戳")]
    public long createTimestamp;

    [Header("记录列表")]
    [Tooltip("本批次包含的全部记录键")]
    public string[] recordKeys;

    [Header("类型统计")]
    [Tooltip("按实体类型分组的记录数量统计")]
    public TypeCountEntry[] typeCounts;

    [Header("完整性校验")]
    [Tooltip("清单内容哈希值，用于防篡改校验")]
    public string manifestHash;
}

/// <summary>
/// 类型计数条目，表示一种实体类型在批次中的记录数量。
/// </summary>
[System.Serializable]
public class TypeCountEntry
{
    [Tooltip("实体类型标识")]
    public string entityType;

    [Tooltip("该实体类型的记录数量")]
    public int recordCount;
}
