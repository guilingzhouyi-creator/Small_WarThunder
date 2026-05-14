using System.Collections.Generic;

/// <summary>
/// Action 显示名称翻译层：将底层 InputAction 英文原生名称映射为 UI 中文显示名。
/// 纯展示用途，不影响输入系统、数据存储、控制逻辑。
/// </summary>
public static class ActionDisplayNameMapping
{
    /// <summary>
    /// 英文 Action name → 中文显示名 映射表。
    /// 键为 InputAction.name（与 InputActionAsset 中定义一致），值为 UI 展示文本。
    /// </summary>
    private static readonly Dictionary<string, string> _mapping = new Dictionary<string, string>
    {
        // ─── 移动类 ───
        { KeyBindingConstants.ActionForward, "前进" },
        { KeyBindingConstants.ActionBack, "后退" },
        { KeyBindingConstants.ActionLeftTurn, "左转" },
        { KeyBindingConstants.ActionRightTurn, "右转" },

        // ─── 战斗类 ───
        { KeyBindingConstants.ActionFire, "开火" },
        { KeyBindingConstants.ActionMachinegunFire, "机枪开火" },
        { KeyBindingConstants.ActionReload, "装填" },
        { KeyBindingConstants.ActionGunAim, "瞄准" },

        // ─── 辅助功能类 ───
        { KeyBindingConstants.ActionZoomFOV, "变焦" },
        { KeyBindingConstants.ActionFreeLooking, "自由视角" },
        { KeyBindingConstants.ActionPause, "暂停" },
        { KeyBindingConstants.ActionEngineSwitch, "引擎开关" },
        { KeyBindingConstants.ActionThermalmaging, "热成像" },
        { KeyBindingConstants.ActionRangefinder, "测距仪" },
        { KeyBindingConstants.ActionMIssionLabShow, "任务面板" },
        { KeyBindingConstants.ActionMapShow, "地图" },
        { KeyBindingConstants.ActionDebugButton, "调试按钮" },
    };

    /// <summary>
    /// 获取 Action 的中文显示名。未匹配时返回原始英文名作为兜底。
    /// </summary>
    /// <param name="actionName">InputAction 的原生英文名称</param>
    /// <returns>中文显示名或原始名</returns>
    public static string GetDisplayName(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
        {
            return string.Empty;
        }

        if (_mapping.TryGetValue(actionName, out string displayName))
        {
            return displayName;
        }

        return actionName;
    }
}
