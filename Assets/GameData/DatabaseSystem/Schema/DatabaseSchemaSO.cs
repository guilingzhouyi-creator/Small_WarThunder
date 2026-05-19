using UnityEngine;

/// <summary>
/// 数据库模式定义SO，声明哪些业务实体类型被允许进入持久化层。
/// 由持久化储存系统的PersistenceSchemaSO引用，重载系统通过它判断可恢复范围。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseSchema", menuName = "SmallWarThunder/数据库/结构/数据库结构表")]
public class DatabaseSchemaSO : ScriptableObject
{
    [Header("实体类型白名单")]
    [Tooltip("允许持久化的业务实体类型标识列表")]
    public string[] allowedEntityTypes;

    [Header("数据范围约束")]
    [Tooltip("每批次最大记录数")]
    public int maxRecordsPerBatch = 512;

    [Tooltip("单条记录最大字节数")]
    public int maxBytesPerRecord = 65536;

    [Header("版本策略")]
    [Tooltip("当前数据库版本号")]
    public string databaseVersion = "1.0";
}
