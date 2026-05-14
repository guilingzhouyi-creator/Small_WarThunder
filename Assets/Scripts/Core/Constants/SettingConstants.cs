/// <summary>
/// 设置系统使用的所有字符串常量（TabKey、持久化Key等）。
/// 所有 Setting 相关代码通过此类引用字符串，禁止硬编码。
/// </summary>
public static class SettingConstants
{
    /// <summary>通用设置 Tab 标识</summary>
    public const string TabKeyGeneral = "General";

    /// <summary>画面设置 Tab 标识</summary>
    public const string TabKeyVisual = "Visual";

    /// <summary>音频设置 Tab 标识</summary>
    public const string TabKeyAudio = "Audio";

    /// <summary>按键绑定 Tab 标识</summary>
    public const string TabKeyKeyBinding = "KeyBinding";

    /// <summary>PlayerPrefs 中音频设置的存储键</summary>
    public const string PrefsKeyAudioSettings = "SmallWarThunder.AudioSettings";
}
