using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 按键绑定持久化管理器。
/// 负责将玩家自定义的绑定覆盖序列化到 KeyBindingSaveData (ScriptableObject) 并反序列化。
/// 通过 InputSystem 的 bindingOverrides 格式（JSON string）与内部字典互转。
/// </summary>
public class KeyBindingSaveManager
{
    private readonly KeyBindingSaveData _saveData;
    private readonly InputActionAsset _inputActionAsset;

    /// <param name="saveData">用于存储覆盖数据的 ScriptableObject</param>
    /// <param name="inputActionAsset">当前的 InputActionAsset 资产实例</param>
    public KeyBindingSaveManager(KeyBindingSaveData saveData, InputActionAsset inputActionAsset)
    {
        _saveData = saveData;
        _inputActionAsset = inputActionAsset;
        Debug.Log("[KeyBindingSaveManager] 初始化完成");
    }

    /// <summary>
    /// 从 ScriptableObject 加载覆盖数据并应用到当前 InputActionAsset。
    /// 返回实际应用成功的覆盖数量。
    /// </summary>
    public int LoadOverrides()
    {
        if (_saveData.overrides == null || _saveData.overrides.Count == 0)
        {
            Debug.Log("[KeyBindingSaveManager] SaveData 中无覆盖数据，跳过加载");
            return 0;
        }

        var dict = new Dictionary<string, string>();
        foreach (var entry in _saveData.overrides)
        {
            if (!string.IsNullOrEmpty(entry.actionName))
                dict[entry.actionName] = entry.bindingPath ?? string.Empty;
        }

        int appliedCount = ApplyOverridesFromDict(dict);
        Debug.Log($"[KeyBindingSaveManager] 加载完成，应用 {appliedCount} 个覆盖绑定");
        return appliedCount;
    }

    /// <summary>
    /// 将当前 InputActionAsset 中的覆盖数据保存到 ScriptableObject。
    /// 只保存与默认绑定不同的覆盖。
    /// </summary>
    public void SaveOverrides()
    {
        var dict = ExtractOverrideDict();
        _saveData.overrides.Clear();

        foreach (var kv in dict)
        {
            _saveData.overrides.Add(new KeyBindingOverride
            {
                actionName = kv.Key,
                bindingPath = kv.Value
            });
        }

        Debug.Log($"[KeyBindingSaveManager] 保存完成，{_saveData.overrides.Count} 个覆盖绑定");
    }

    /// <summary>
    /// 重置所有绑定到默认值，并清除 SaveData。
    /// </summary>
    public void ResetToDefaults()
    {
        if (_inputActionAsset != null)
        {
            _inputActionAsset.RemoveAllBindingOverrides();
        }

        _saveData.overrides.Clear();
        Debug.Log("[KeyBindingSaveManager] 重置所有绑定为默认值");
    }

    /// <summary>
    /// 从当前 InputActionAsset 提取所有覆盖绑定为字典。
    /// 键为 actionName，值为 bindingPath。
    /// </summary>
    public Dictionary<string, string> ExtractOverrideDict()
    {
        var dict = new Dictionary<string, string>();
        if (_inputActionAsset == null) return dict;

        foreach (var action in _inputActionAsset)
        {
            foreach (var binding in action.bindings)
            {
                // 跳过组合键（composite binding，如 WASD）
                if (binding.isComposite) continue;

                // 如果当前 binding 有覆盖路径，说明已被修改
                if (!string.IsNullOrEmpty(binding.overridePath))
                {
                    dict[action.name] = binding.overridePath;
                }
            }
        }

        return dict;
    }

    /// <summary>
    /// 将字典中的绑定覆盖应用到 InputActionAsset。
    /// 返回成功应用的数量。
    /// </summary>
    private int ApplyOverridesFromDict(Dictionary<string, string> overrides)
    {
        int count = 0;
        foreach (var kv in overrides)
        {
            var action = _inputActionAsset?.FindAction(kv.Key);
            if (action == null) continue;

            // 找到第一个非复合的 binding 并应用 overridePath
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite)
                {
                    action.ApplyBindingOverride(i, new InputBinding { overridePath = kv.Value });
                    count++;
                    break;
                }
            }
        }
        return count;
    }
}
