/// <summary>
/// 字幕渲染作用域常量（编译期类型安全的 themeTag 字符串）。
/// 
/// 原来的魔法字符串 "Overlay"/"Intelligence"/"TaskText" 统一收敛到此文件，
/// 调用方通过常量引用，避免拼写错误导致的静默失败。
/// </summary>
public static class SubtitleRenderScope
{
    /// <summary>覆盖层打字机（System/Dialogue/Ambient 频道逐字渲染）</summary>
    public const string Overlay = "Overlay";

    /// <summary>情报栏整片推送（Mission 频道整段渲染）</summary>
    public const string Intelligence = "Intelligence";

    /// <summary>任务文本渲染</summary>
    public const string TaskText = "TaskText";
}
