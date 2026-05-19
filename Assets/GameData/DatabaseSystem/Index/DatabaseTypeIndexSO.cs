using System;
using UnityEngine;

/// <summary>
/// 数据库类型索引SO，维护"实体类型 → 记录键列表"的映射。
/// 重载系统通过它按类型快速批量获取记录，避免遍历全部键。
/// </summary>
[CreateAssetMenu(fileName = "DatabaseTypeIndex", menuName = "SmallWarThunder/数据库/索引/类型索引")]
public class DatabaseTypeIndexSO : ScriptableObject
{
    [Header("类型索引条目")]
    [Tooltip("每种实体类型对应的记录键集合")]
    public TypeIndexGroup[] typeGroups;

    /// <summary>
    /// 按实体类型获取该类型下的所有记录键。
    /// </summary>
    public string[] GetKeysByType(string entityType)
    {
        if (typeGroups == null) return Array.Empty<string>();
        for (int i = 0; i < typeGroups.Length; i++)
        {
            if (typeGroups[i] != null && typeGroups[i].entityType == entityType)
                return typeGroups[i].recordKeys ?? Array.Empty<string>();
        }
        return Array.Empty<string>();
    }
}

/// <summary>
/// 类型索引分组，表示一种实体类型对应的全部记录键。
/// </summary>
[System.Serializable]
public class TypeIndexGroup
{
    [Tooltip("实体类型标识")]
    public string entityType;

    [Tooltip("该实体类型对应的所有记录键")]
    public string[] recordKeys;
}
