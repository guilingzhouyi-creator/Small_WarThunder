/// <summary>
/// 集中管理所有全局 UI 展示文本常量。
/// 为未来接入本地化（I18N）提供单一替换入口。
/// </summary>
public static class UIStandardTexts
{
    // ─── GlobalSubtitleEngine ───
    /// <summary>字幕引擎空闲状态占位文本</summary>
    public const string Idle = "暂无";

    // ─── TankStats ───
    /// <summary>装填完成/就绪状态文本</summary>
    public const string Up = "Up!";

    /// <summary>弹药耗尽状态文本</summary>
    public const string AmmoExhausted = "弹药耗尽!";

    /// <summary>数据不可用占位文本</summary>
    public const string NotAvailable = "N/A";
}
