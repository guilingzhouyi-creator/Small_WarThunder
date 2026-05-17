using System;

/// <summary>
/// GlobalSubtitleEngine 的跨层级字幕覆盖层模块（partial）。
/// 负责 System/Dialogue/Ambient 频道的逐字打字机推送。
/// </summary>
partial class GlobalSubtitleEngine
{
    /// <summary>
    /// 全局覆盖层文本更新事件 — 由 System/Dialogue/Ambient 频道驱动，逐字打字机推送
    /// </summary>
    public event Action<string> OnOverlayTextChanged;

    /// <summary>
    /// [弃用] 保留兼容旧引用，等价于 OnOverlayTextChanged
    /// </summary>
    // [Obsolete("请使用 OnOverlayTextChanged 替代 OnSubtitleTextChanged")]
    // public event Action<string> OnSubtitleTextChanged
    // {
    //     add { OnOverlayTextChanged += value; }
    //     remove { OnOverlayTextChanged -= value; }
    // }
}
