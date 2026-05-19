using UnityEngine;

/// <summary>
/// 数据库运行状态SO，记录当前数据库的总体健康度、最后批次信息和空间占用。
/// 持久化储存系统和重载系统共同引用此SO来同步整体状态。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseState", menuName = "SmallWarThunder/数据库/状态/运行快照")]
public class DatabaseStateSO : ScriptableObject
{
    [Header("总体状态")]
    [Tooltip("当前数据库是否处于可用状态")]
    public bool isOperational = true;

    [Tooltip("当前总记录数量")]
    public int totalRecordCount;

    [Tooltip("当前总占用字节数")]
    public long totalBytesUsed;

    [Header("最近批次")]
    [Tooltip("最近一次成功写入的批次号")]
    public string lastBatchId;

    [Tooltip("最近一次写入的时间戳")]
    public long lastWriteTimestamp;

    [Tooltip("最近一次写入的状态")]
    public EDatabaseWriteStatus lastWriteStatus;

    [Header("最近重载")]
    [Tooltip("最近一次重载的批次号")]
    public string lastReloadBatchId;

    [Tooltip("最近一次重载的时间戳")]
    public long lastReloadTimestamp;

    [Tooltip("最近一次重载的解析状态")]
    public EDatabaseParseStatus lastParseStatus;
}
