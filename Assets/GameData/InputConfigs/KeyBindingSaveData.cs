using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个按键绑定的持久化覆盖数据条目。
/// 存储玩家自定义的绑定路径，键为 Action 名称（如 "Forward"）。
/// </summary>
[Serializable]
public struct KeyBindingOverride
{
    /// <summary>Action 名称，如 "Forward"、"Back"，对应 .inputactions 中的 Action.name</summary>
    public string actionName;

    /// <summary>Input Binding 路径，如 "<Keyboard>/w"</summary>
    public string bindingPath;
}

/// <summary>
/// ScriptableObject 形式的按键绑定持久化数据容器。
/// 存储玩家所有自定义绑定覆盖信息。默认绑定由 GameInputingSystem.inputactions 资产提供。
/// </summary>
[CreateAssetMenu(fileName = "KeyBindingSaveData", menuName = "SmallWarThunder/输入/按键绑定/保存数据")]
public class KeyBindingSaveData : ScriptableObject
{
    /// <summary>
    /// 所有被玩家修改过的绑定覆盖条目列表。
    /// 仅包含与默认不同的绑定。
    /// </summary>
    public List<KeyBindingOverride> overrides = new List<KeyBindingOverride>();
}
