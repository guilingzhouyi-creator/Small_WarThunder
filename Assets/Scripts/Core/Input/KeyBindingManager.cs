using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 按键重绑定核心管理器（纯C#，不依赖MonoBehaviour）。
/// 通过构造函数注入 InputActionAsset，管理 TankerDriver ActionMap 内所有 Action 的重绑定。
/// </summary>
public class KeyBindingManager
{
    private readonly InputActionAsset _asset;
    private readonly InputActionMap _tankerDriverMap;
    private InputActionRebindingExtensions.RebindingOperation _activeRebind;

    /// <summary>重绑定完成或取消时触发（非冲突）</summary>
    public event Action<string, string> BindingChanged;

    /// <summary>开始重绑定时触发</summary>
    public event Action<string> RebindStarted;

    /// <summary>重绑定取消时触发</summary>
    public event Action<string> RebindCancelled;

    /// <summary>按键冲突时触发</summary>
    public event Action<string, string, string> BindingConflictOccurred;

    /// <summary>是否正在进行重绑定</summary>
    public bool IsRebinding { get; private set; }

    /// <summary>当前正在重绑定的 actionId</summary>
    public string RebindingActionId { get; private set; }

    /// <summary>
    /// 构造函数，注入 InputActionAsset 并定位 TankerDriver ActionMap。
    /// </summary>
    /// <param name="asset">GameInputingSystem 的 asset 实例</param>
    public KeyBindingManager(InputActionAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("[KeyBindingManager] 构造失败：asset 为空");
            return;
        }

        _asset = asset;
        _tankerDriverMap = asset.FindActionMap(KeyBindingConstants.MapTankerDriver);
        if (_tankerDriverMap == null)
        {
            Debug.LogError($"[KeyBindingManager] 未找到 ActionMap: {KeyBindingConstants.MapTankerDriver}");
        }
    }

    /// <summary>
    /// 开始交互式重绑定指定 Action。
    /// </summary>
    /// <param name="actionId">InputAction 的 ID (GUID)，如 "705be9f3-dee4-4ae3-a54f-09e6e5aa502a"</param>
    /// <param name="isCompositeBinding">是否为组合绑定（TankerDriver 中全为 false）</param>
    /// <param name="bindingIndex">要重绑定的 binding 索引，默认 0</param>
    public void PerformRebind(string actionId, bool isCompositeBinding = false, int bindingIndex = 0)
    {
        if (_tankerDriverMap == null)
        {
            Debug.LogError("[KeyBindingManager] PerformRebind 失败：TankerDriver ActionMap 为空");
            return;
        }

        if (IsRebinding)
        {
            Debug.LogWarning($"[KeyBindingManager] 已在重绑定中，actionId={RebindingActionId}，忽略新请求");
            return;
        }

        InputAction action = _tankerDriverMap.FindAction(actionId);
        if (action == null)
        {
            Debug.LogError($"[KeyBindingManager] 未找到 Action: id={actionId}");
            return;
        }

        // 重绑定前需禁用当前 Action
        if (action.enabled)
        {
            Debug.LogWarning($"[KeyBindingManager] Action {action.name} 已启用，重绑定时将临时禁用");
        }

        IsRebinding = true;
        RebindingActionId = actionId;

        // 查找 binding 索引对应的当前 binding
        int currentBindingIndex = FindBindingIndex(action, bindingIndex);
        if (currentBindingIndex < 0)
        {
            Debug.LogError($"[KeyBindingManager] Action {action.name} 没有 binding index={bindingIndex}");
            IsRebinding = false;
            RebindingActionId = null;
            return;
        }

        _activeRebind = action.PerformInteractiveRebinding(currentBindingIndex)
            // 排除鼠标位置和滚轮，避免误触
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnPotentialMatch(operation =>
            {
                // 检测冲突：遍历同 Map 中所有 Action 的 binding，检查是否存在相同路径
                string targetPath = operation.selectedControl.path;
                if (HasConflict(actionId, targetPath))
                {
                    operation.Cancel();
                    string conflictActionName = GetConflictingActionName(targetPath);
                    Debug.LogWarning($"[KeyBindingManager] 按键冲突: {targetPath} 已被 {conflictActionName} 绑定");
                    BindingConflictOccurred?.Invoke(actionId, targetPath, conflictActionName);
                }
            })
            .OnApplyBinding((operation, newPath) =>
            {
                Debug.Log($"[KeyBindingManager] 重绑定完成: actionId={actionId}, newPath={newPath}");
            })
            .OnComplete(operation =>
            {
                IsRebinding = false;
                _activeRebind = null;

                if (operation.canceled)
                {
                    Debug.Log($"[KeyBindingManager] 重绑定已取消: actionId={actionId}");
                    RebindCancelled?.Invoke(actionId);
                }
                else
                {
                    string displayString = operation.selectedControl != null
                        ? operation.selectedControl.path
                        : string.Empty;
                    Debug.Log($"[KeyBindingManager] 重绑定应用: actionId={actionId}, displayString={displayString}");
                    NotifyBindingChanged(actionId, displayString);
                }

                RebindingActionId = null;
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                Debug.Log($"[KeyBindingManager] 重绑定被取消: actionId={actionId}");
                IsRebinding = false;
                _activeRebind = null;
                RebindCancelled?.Invoke(actionId);
                RebindingActionId = null;
                operation.Dispose();
            });

        _activeRebind.Start();
        Debug.Log($"[KeyBindingManager] 开始重绑定: actionId={actionId}");
        RebindStarted?.Invoke(actionId);
    }

    /// <summary>
    /// 取消当前正在进行的重绑定。
    /// </summary>
    public void CancelRebind()
    {
        if (!IsRebinding || _activeRebind == null)
        {
            return;
        }

        _activeRebind.Cancel();
        Debug.Log("[KeyBindingManager] 手动取消重绑定");
    }

    /// <summary>
    /// 将指定 Action 恢复为默认绑定，并移除所有覆盖。
    /// </summary>
    public void ResetToDefault(string actionId)
    {
        if (_tankerDriverMap == null)
        {
            return;
        }

        InputAction action = _tankerDriverMap.FindAction(actionId);
        if (action == null)
        {
            Debug.LogError($"[KeyBindingManager] ResetToDefault 失败：未找到 Action id={actionId}");
            return;
        }

        action.RemoveBindingOverride(0);
        string currentPath = GetCurrentBindingPath(actionId);
        Debug.Log($"[KeyBindingManager] 已重置为默认: actionId={actionId}, path={currentPath}");
        NotifyBindingChanged(actionId, currentPath);
    }

    /// <summary>
    /// 将所有可重绑定 Action 恢复为默认绑定。
    /// </summary>
    public void ResetAllToDefault()
    {
        if (_tankerDriverMap == null)
        {
            return;
        }

        foreach (InputAction action in _tankerDriverMap.actions)
        {
            if (action.bindings.Count > 0)
            {
                action.RemoveBindingOverride(0);
            }
        }

        Debug.Log("[KeyBindingManager] 所有按键已重置为默认");
    }

    /// <summary>
    /// 将所有 Action 恢复为默认绑定并保存。
    /// </summary>
    public void ResetAllBindings()
    {
        ResetAllToDefault();
        SaveBindings();
        Debug.Log("[KeyBindingManager] 所有按键已重置为默认并保存");
    }

    /// <summary>
    /// 将当前 binding overrides 保存到 PlayerPrefs。
    /// </summary>
    public void SaveBindings()
    {
        if (_asset == null)
        {
            Debug.LogError("[KeyBindingManager] SaveBindings 失败：asset 为空");
            return;
        }

        string json = _asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(KeyBindingConstants.SaveKeyBindingOverrides, json);
        PlayerPrefs.Save();
        Debug.Log($"[KeyBindingManager] 按键绑定已保存, length={json.Length}");
    }

    /// <summary>
    /// 获取指定 Action 当前生效的绑定路径字符串。
    /// </summary>
    public string GetCurrentBindingPath(string actionId)
    {
        if (_tankerDriverMap == null)
        {
            return string.Empty;
        }

        InputAction action = _tankerDriverMap.FindAction(actionId);
        if (action == null || action.bindings.Count == 0)
        {
            return string.Empty;
        }

        // 返回第一个 binding 的有效路径（考虑覆盖）
        int bindingIndex = action.GetBindingIndex();
        if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
        {
            return action.bindings[bindingIndex].effectivePath;
        }

        return action.bindings[0].effectivePath;
    }

    /// <summary>
    /// 获取指定 Action 当前绑定的可读显示字符串（通过 InputControlPath.ToHumanReadableString 转换）。
    /// </summary>
    public string GetCurrentBindingDisplayString(string actionId)
    {
        string path = GetCurrentBindingPath(actionId);
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return InputControlPath.ToHumanReadableString(path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    /// <summary>
    /// 检查指定路径是否已被同 Map 内其他 Action 绑定（冲突检测）。
    /// </summary>
    public bool HasConflict(string excludeActionId, string targetPath)
    {
        if (_tankerDriverMap == null || string.IsNullOrEmpty(targetPath))
        {
            return false;
        }

        foreach (InputAction action in _tankerDriverMap.actions)
        {
            if (action.id.ToString() == excludeActionId)
            {
                continue;
            }

            foreach (InputBinding binding in action.bindings)
            {
                if (!binding.isComposite && !binding.isPartOfComposite
                    && binding.effectivePath == targetPath)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetConflictingActionName(string targetPath)
    {
        if (_tankerDriverMap == null)
        {
            return "未知";
        }

        foreach (InputAction action in _tankerDriverMap.actions)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.effectivePath == targetPath)
                {
                    return action.name;
                }
            }
        }

        return "未知";
    }

    /// <summary>
    /// 查找指定 Action 的第 N 个非组合 binding 的实际索引。
    /// </summary>
    private int FindBindingIndex(InputAction action, int targetIndex)
    {
        int count = 0;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
            {
                if (count == targetIndex)
                {
                    return i;
                }

                count++;
            }
        }

        return -1;
    }

    private void NotifyBindingChanged(string actionId, string displayString)
    {
        BindingChanged?.Invoke(actionId, displayString);
    }
}
