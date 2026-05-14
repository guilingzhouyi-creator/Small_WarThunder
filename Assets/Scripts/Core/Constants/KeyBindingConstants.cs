/// <summary>
/// 按键绑定系统使用的所有字符串常量（ActionMap、Action 名称）。
/// 所有 KeyBinding 相关代码通过此类引用字符串，禁止硬编码。
/// 注意：TankerDriver 是唯一的 InputActionMap，源于 GameInputingSystem.inputactions。
/// </summary>
public static class KeyBindingConstants
{
    // ─── ActionMap 名称 ───
    public const string MapTankerDriver = "TankerDriver";

    // ─── TankerDriver Actions ───
    public const string ActionForward = "Forward";
    public const string ActionBack = "Back";
    public const string ActionLeftTurn = "LeftTurn";
    public const string ActionRightTurn = "RightTurn";
    public const string ActionFire = "Fire";
    public const string ActionReload = "Reload";
    public const string ActionZoomFOV = "ZoomFOV";
    public const string ActionFreeLooking = "FreeLooking";
    public const string ActionPause = "Pause";
    public const string ActionGunAim = "GunAim";
    public const string ActionMachinegunFire = "MachinegunFire";
    public const string ActionEngineSwitch = "EngineSwitch";
    public const string ActionThermalmaging = "Thermalmaging";
    public const string ActionRangefinder = "Rangefinder";
    public const string ActionMIssionLabShow = "MIssionLabShow";
    public const string ActionMapShow = "MapShow";
    public const string ActionDebugButton = "DebugButton";

    // ─── 不可重新绑定的特殊按键路径 ───
    public const string SpecialKeyEscape = "<Keyboard>/escape";
    public const string SpecialKeyWindows = "<Keyboard>/windows";
    public const string SpecialKeyDelete = "<Keyboard>/delete";
    public const string SpecialKeyInsert = "<Keyboard>/insert";

    /// <summary>
    /// 判断给定输入路径是否为禁止绑定的特殊键。
    /// </summary>
    public static bool IsSpecialKey(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath)) return false;
        string lower = bindingPath.ToLowerInvariant();
        return lower == SpecialKeyEscape
            || lower == "<Keyboard>/windows"
            || lower == "<Keyboard>/delete"
            || lower == "<Keyboard>/insert"
            || lower.StartsWith("<Keyboard>/fn", System.StringComparison.OrdinalIgnoreCase);
    }

    // ─── 持久化 Key ───
    public const string SaveKeyBindingOverrides = "KeyBindingOverrides";

    // ─── UI 展示文本 ───
    public const string UiLabelResetToDefault = "恢复默认";
    public const string UiLabelListeningPrompt = "按下新按键...";
    public const string UiLabelConflictWarning = "按键冲突: {0}";
    public const string UiTitleKeyBindingPanel = "按键设置";
}
