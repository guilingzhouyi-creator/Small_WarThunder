using UnityEngine;

/// <summary>
/// 文本颜色渲染主题（ScriptableObject）。
/// 
/// 定义一个可配置的配色方案，由 SubtitleColorRenderEngine 在运行时消费。
/// 通过中央注册表（RegisterTheme/UnregisterTheme）管理多个主题的激活状态。
/// 
/// == 配置说明 ==================================================================
/// TargetType        — 输出目标类型（TextMeshPro / UI Toolkit），影响富文本标签语法。
/// DefaultColorHex   — 默认整段文本应用的基础颜色（例如 "#FFFFFF" 白色）。
/// ChannelOverrides  — 按 SubtitleChannel 枚举覆盖颜色/样式（编译期类型安全）。
/// KeywordRules      — 按关键词匹配，对文本片段进行局部高亮覆盖。
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/核心/字幕/颜色主题", fileName = "ColorTheme_Default")]
public class ColorThemeSO : ScriptableObject
{
    /// <summary>主题名称（用于 Inspector 区分）</summary>
    public string ThemeName = "Default";

    /// <summary>渲染目标类型：TextMeshPro 或 UI Toolkit</summary>
    public RenderTarget TargetType = RenderTarget.TextMeshPro;

    /// <summary>默认整段文本颜色</summary>
    [ColorUsage(false, false)]
    public string DefaultColorHex = "#FFFFFF";

    // —— 是否对整体应用 DefaultColorHex 包装 ——
    //     true:  整段文本统一着色
    //     false: 仅对匹配到关键词的片段着色，其余保持原色
    //   public bool ApplyDefaultColor = true;   // 未来扩展

    /// <summary>按频道覆盖的颜色/样式列表</summary>
    [Tooltip("按频道标识（字符串）进行颜色/样式覆盖。匹配规则：第一个匹配项生效。")]
    public ChannelColorOverride[] ChannelOverrides;

    /// <summary>按关键词高亮的规则列表</summary>
    [Tooltip("按关键词进行局部高亮。关键词匹配为纯字符串包含匹配（非正则）。")]
    public KeywordHighlightRule[] KeywordRules;
}

/// <summary>渲染目标枚举（影响富文本标签语法）</summary>
public enum RenderTarget
{
    /// <summary>TextMeshPro 专用富文本标签（<color=#RRGGBB> 格式）</summary>
    TextMeshPro,

    /// <summary>UI Toolkit Label 兼容的富文本标签（与 TMP 语法兼容但独立通道）</summary>
    UIToolkit,
}

/// <summary>频道级别的颜色/样式覆盖（编译期类型安全，基于 SubtitleChannel 枚举）</summary>
[System.Serializable]
public struct ChannelColorOverride
{
    /// <summary>频道标识（SubtitleChannel 枚举，Inspector 中显示为下拉选择）</summary>
    [Tooltip("频道标识（枚举），匹配 SubtitleColorRenderEngine.Process() 传入的 channelTag。")]
    public SubtitleChannel ChannelTag;

    /// <summary>应用于该频道的颜色（十六进制，如 "#FF4444"）</summary>
    [ColorUsage(false, false)]
    public string ColorHex;

    /// <summary>是否对该频道文本加粗</summary>
    public bool ApplyBold;

    /// <summary>是否对该频道文本应用斜体</summary>
    public bool ApplyItalic;
}

/// <summary>关键词高亮规则（纯字符串包含匹配）</summary>
[System.Serializable]
public struct KeywordHighlightRule
{
    /// <summary>要匹配的关键词</summary>
    [Tooltip("要匹配的关键词。使用纯字符串包含匹配，对大小写不敏感（除非勾选 CaseSensitive）。")]
    public string Keyword;

    /// <summary>高亮颜色（十六进制）</summary>
    [ColorUsage(false, false)]
    public string ColorHex;

    /// <summary>是否区分大小写</summary>
    public bool CaseSensitive;
}
