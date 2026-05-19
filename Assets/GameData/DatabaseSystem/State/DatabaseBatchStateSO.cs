using UnityEngine;

/// <summary>
/// 数据库批次状态SO，记录单次保存批次的写入进度、成功/失败记录列表和批次完整性。
/// 持久化储存系统写入层在批次落库过程中和完成后更新此SO。
/// 持久化数据重载系统的读取层通过此SO判断批次是否完整可用。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseBatchState", menuName = "SmallWarThunder/数据库/状态/批次快照")]
public class DatabaseBatchStateSO : ScriptableObject
{
    [Header("批次标识")]
    [Tooltip("批次唯一标识符")]
    public string batchId;

    [Tooltip("批次写入开始时间戳")]
    public long batchStartTimestamp;

    [Tooltip("批次写入结束时间戳")]
    public long batchEndTimestamp;

    [Header("批次进度")]
    [Tooltip("批次当前状态")]
    public EDatabaseBatchStatus batchStatus;

    [Tooltip("本批次总记录数")]
    public int totalRecordsInBatch;

    [Tooltip("本批次成功写入记录数")]
    public int successRecordCount;

    [Tooltip("本批次失败记录数")]
    public int failedRecordCount;

    [Header("失败详情")]
    [Tooltip("写入失败的记录键列表")]
    public string[] failedRecordKeys;

    [Tooltip("失败原因摘要")]
    public string failureReason;
}
