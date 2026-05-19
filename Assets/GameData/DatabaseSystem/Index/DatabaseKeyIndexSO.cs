using System;
using UnityEngine;

/// <summary>
/// 数据库键索引SO，维护"记录唯一键 → 记录元数据"的映射。
/// 持久化储存系统在写入时登记，重载系统在读取时查询。
/// 每条记录的键由 {实体类型}-{实体ID}-{状态类型} 组成。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseKeyIndex", menuName = "SmallWarThunder/数据库/索引/键映射表")]
public class DatabaseKeyIndexSO : ScriptableObject
{
    [Header("键条目列表")]
    [Tooltip("当前数据库中所有记录的键及其元数据")]
    public KeyIndexEntry[] entries;

    /// <summary>
    /// 通过记录键查找对应的键字条目。
    /// </summary>
    public KeyIndexEntry FindEntry(string recordKey)
    {
        if (entries == null) return null;
        int index = Array.FindIndex(entries, e => e != null && e.recordKey == recordKey);
        return index >= 0 ? entries[index] : null;
    }
}

/// <summary>
/// 键索引单条映射，描述一条记录在静态数据库中的定位信息。
/// </summary>
[System.Serializable]
public class KeyIndexEntry
{
    [Tooltip("记录唯一键：格式为 {实体类型}-{实体ID}-{状态类型}")]
    public string recordKey;

    [Tooltip("所属批次号")]
    public string batchId;

    [Tooltip("记录在物理存储中的相对路径或偏移")]
    public string storagePath;

    [Tooltip("记录写入时的时间戳")]
    public long writeTimestamp;

    [Tooltip("记录版本号")]
    public string recordVersion;

    [Tooltip("记录状态")]
    public EDatabaseRecordStatus status;
}
