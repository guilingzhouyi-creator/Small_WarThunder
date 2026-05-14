using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可重绑定按键的元数据条目，用于 UI 列表生成。
/// 每个条目对应 InputActionAsset 中一个特定的 Action（通过 actionId 引用）。
/// </summary>
[Serializable]
public class KeyBindingEntry
{
    /// <summary>InputAction 的 ID（GUID 字符串），用于跨重命名定位 Action</summary>
    [Tooltip("InputAction 的唯一标识符 (GUID)")]
    public string actionId;

    /// <summary>UI 中展示的本地化键，如 "前移"</summary>
    [Tooltip("UI 中展示的按键名称")]
    public string displayName;

    /// <summary>所属分组名称，用于 UI 折叠分类，如 "移动" / "战斗"</summary>
    [Tooltip("UI 分组名称")]
    public string groupName;

    /// <summary>排序优先级（升序排列）</summary>
    [Tooltip("排序优先级")]
    public int sortOrder;
}

/// <summary>
/// ScriptableObject：存储所有可重绑定按键的元数据。
/// 实际绑定覆盖数据通过 InputSystem 的 JSON 序列化，不在此 SO 中持久化。
/// 位于 Assets/GameData/InputConfigs/ 目录下。
/// </summary>
[CreateAssetMenu(fileName = "KeyBindingData", menuName = "SmallWarThunder/Input/KeyBindingData", order = 1)]
public class KeyBindingData : ScriptableObject
{
    /// <summary>可重绑定按键列表</summary>
    [Tooltip("所有可重绑定的按键定义")]
    public KeyBindingEntry[] entries;

    /// <summary>
    /// 通过 actionId 查找条目（O(n)，列表规模小故不建字典）。
    /// </summary>
    /// <param name="actionId">InputAction 的 ID (GUID)</param>
    /// <returns>找到的条目，未找到返回 null</returns>
    public KeyBindingEntry FindById(string actionId)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].actionId == actionId)
                return entries[i];
        }
        return null;
    }

    /// <summary>
    /// 按 groupName 分组返回条目（保持 sortOrder 排序）。
    /// </summary>
    /// <returns>分组名 → 条目列表</returns>
    public Dictionary<string, List<KeyBindingEntry>> GroupByCategory()
    {
        var result = new Dictionary<string, List<KeyBindingEntry>>();
        if (entries == null) return result;

        for (int i = 0; i < entries.Length; i++)
        {
            string group = entries[i].groupName ?? string.Empty;
            if (!result.TryGetValue(group, out var list))
            {
                list = new List<KeyBindingEntry>();
                result[group] = list;
            }
            list.Add(entries[i]);
        }

        // 每组内按 sortOrder 排序
        foreach (var kv in result)
        {
            kv.Value.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        }

        return result;
    }
}
