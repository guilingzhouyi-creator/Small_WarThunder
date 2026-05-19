using System;
using UnityEngine;

/// <summary>
/// 数据库版本索引SO，维护"版本号 → 记录键列表"的映射。
/// 重载系统通过它按版本查询记录，配合兼容策略处理跨版本数据。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseVersionIndex", menuName = "SmallWarThunder/数据库/索引/版本索引")]
public class DatabaseVersionIndexSO : ScriptableObject
{
    [Header("当前数据库版本")]
    [Tooltip("静态数据库的最新版本号")]
    public string currentVersion = "1.0";

    [Header("版本索引条目")]
    [Tooltip("各版本号对应的记录键集合，用于兼容查询")]
    public VersionIndexGroup[] versionGroups;

    /// <summary>
    /// 按版本号获取该版本下的所有记录键。
    /// </summary>
    public string[] GetKeysByVersion(string version)
    {
        if (versionGroups == null) return Array.Empty<string>();
        for (int i = 0; i < versionGroups.Length; i++)
        {
            if (versionGroups[i] != null && versionGroups[i].version == version)
                return versionGroups[i].recordKeys ?? Array.Empty<string>();
        }
        return Array.Empty<string>();
    }
}

/// <summary>
/// 版本索引分组，表示一个版本号对应的全部记录键。
/// </summary>
[System.Serializable]
public class VersionIndexGroup
{
    [Tooltip("版本号")]
    public string version;

    [Tooltip("该版本下的所有记录键")]
    public string[] recordKeys;
}
