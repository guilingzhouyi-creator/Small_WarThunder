using System.Collections.Generic;

/// <summary>
/// 富文本颜色渲染引擎。
/// 
/// == 概述 ==================================================================
/// 接收纯文本，根据已注册的 ColorThemeSO 配色方案，在指定位置插入颜色富文本标签，
/// 最终输出带颜色标记的字符串（不修改原始文本内容）。
/// 
/// == 特性 ==================================================================
/// · 纯静态类，无需挂载 GameObject，无 MonoBehaviour 生命周期依赖。
/// · 中央注册表设计：通过字符串 tag 区分不同 UI 消费端，支持多主题并行。
/// · 双重渲染目标：TextMeshPro 与 UI Toolkit 各自输出兼容的富文本格式。
/// · 关键词高亮：支持基于关键词的局部着色覆盖（纯字符串包含匹配）。
/// · 频道着色：按 ColorThemeSO.ChannelOverrides 中定义的频道规则整段着色。
/// · 零运行时开销：Process() 为纯字符串运算，无额外 GC Alloc 压力（除结果字符串本身）。
/// 
/// == 迁移说明 ==============================================================
/// 
/// 【步骤 1】复制以下 2 个文件到新项目的任意目录：
///     · SubtitleColorRenderEngine.cs（本文件）
///     · ColorThemeSO.cs
/// 
/// 【步骤 2】在新项目中通过 Unity 菜单 Create → Subtitle → Color Theme 创建 SO 资源。
///     每个 SO 代表一种配色方案。根据 UI 需求配置 DefaultColorHex、ChannelOverrides、
///     KeywordRules 等字段。
/// 
/// 【步骤 3】在游戏初始化阶段注册主题：
///     <code>
///     var overlayTheme = Resources.Load<ColorThemeSO>("Themes/OverlayDefault");
///     SubtitleColorRenderEngine.RegisterTheme("Overlay", overlayTheme);
///     </code>
///     也可以用自定义加载方式（Addressables、Inspector 拖入等）。
/// 
/// 【步骤 4】在目标 UI 组件的文本赋值处插入 Process() 调用：
///     <code>
///     // TMP 例子
///     myTMPLabel.text = SubtitleColorRenderEngine.Process(
///         plainText, "Intelligence", "Mission");
///     
///     // UI Toolkit 例子
///     myLabel.text = SubtitleColorRenderEngine.Process(
///         plainText, "Overlay", "Dialogue");
///     </code>
/// 
/// 【步骤 5】（可选）场景卸载时清理注册表：
///     <code>
///     SubtitleColorRenderEngine.ClearRegistry();
///     </code>
/// 
/// 【注意】唯一需要按新项目适配的内容：
///     1. 创建符合需求的 ColorThemeSO 资源（Inspector 配置）
///     2. ChannelOverrides 中的 ChannelTag 是纯字符串，按你项目的命名习惯填写即可
///     3. 本引擎不强制依赖任何项目特有的枚举/类型
/// 
/// == 使用示例（完整）=======================================================
/// <code>
/// // ========== 初始化阶段 ==========
/// var overlayTheme = Resources.Load<ColorThemeSO>("Themes/OverlayDefault");
/// var intelligenceTheme = Resources.Load<ColorThemeSO>("Themes/IntelligenceDefault");
/// var taskTextTheme = Resources.Load<ColorThemeSO>("Themes/TaskTextDefault");
/// 
/// SubtitleColorRenderEngine.RegisterTheme("Overlay", overlayTheme);
/// SubtitleColorRenderEngine.RegisterTheme("Intelligence", intelligenceTheme);
/// SubtitleColorRenderEngine.RegisterTheme("TaskText", taskTextTheme);
/// 
/// // ========== 运行时使用 ==========
/// string plain = "目标已摧毁";
/// string colored = SubtitleColorRenderEngine.Process(plain, "Intelligence", "Mission");
/// // colored 可能是 "<color=#4FC3F7>目标已摧毁</color>"
/// 
/// myLabel.text = colored;
/// </code>
/// </summary>
public static class SubtitleColorRenderEngine
{
    // ──────────────────────── 中央注册表 ────────────────────────

    private static Dictionary<string, ColorThemeSO> _registeredThemes
        = new Dictionary<string, ColorThemeSO>();

    /// <summary>
    /// 注册一个配色主题到中央注册表。
    /// 相同 tag 会覆盖旧主题。注册后 Process() 即可通过 tag 引用该主题。
    /// </summary>
    /// <param name="tag">主题标识（例如 "Overlay"、"Intelligence"、"TaskText"）</param>
    /// <param name="theme">颜色主题配置</param>
    public static void RegisterTheme(string tag, ColorThemeSO theme)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        _registeredThemes[tag] = theme;
    }

    /// <summary>
    /// 从中央注册表移除指定 tag 的主题。移除后该 tag 的 Process() 将返回原文本。
    /// </summary>
    /// <param name="tag">要移除的主题标识</param>
    public static void UnregisterTheme(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        _registeredThemes.Remove(tag);
    }

    /// <summary>
    /// 根据 tag 获取已注册的主题。
    /// </summary>
    /// <param name="tag">主题标识</param>
    /// <returns>ColorThemeSO 实例，若未注册则返回 null</returns>
    public static ColorThemeSO GetTheme(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return null;
        }

        _registeredThemes.TryGetValue(tag, out var theme);
        return theme;
    }

    /// <summary>
    /// 清空整个注册表。通常用于场景卸载时重置状态。
    /// </summary>
    public static void ClearRegistry()
    {
        _registeredThemes.Clear();
    }

    // ──────────────────────── 核心入口 ────────────────────────

    /// <summary>
    /// 核心入口：将纯文本按指定主题和通道处理为富文本。
    /// 
    /// 处理规则应用顺序（按优先级递减）：
    ///   1. 频道覆盖（ChannelOverrides）— 匹配到频道则整段着色，并应用 Bold/Italic。
    ///   2. 关键词高亮（KeywordRules）— 在频道覆盖后的文本上进行局部高亮替换。
    ///   3. 如果以上均未匹配，使用主题的 DefaultColorHex 作为整段着色。
    ///   4. 若主题未注册，返回原文本。
    /// </summary>
    /// <param name="plainText">原始纯文本内容（不会被修改）</param>
    /// <param name="themeTag">已注册的主题标识（字符串 tag，建议使用 SubtitleRenderScope 常量）</param>
    /// <param name="channelTag">
    /// 频道标识（SubtitleChannel 枚举，编译期类型安全），
    /// 用于匹配 ColorThemeSO.ChannelOverrides 中的覆盖规则。
    /// 默认值 SubtitleChannel.System。
    /// </param>
    /// <returns>
    /// 处理后带颜色富文本标签的字符串。
    /// 若主题未注册或 plainText 为 null/空，返回原文本。
    /// </returns>
    /// <example>
    /// string result = SubtitleColorRenderEngine.Process("目标已摧毁", SubtitleRenderScope.Intelligence, SubtitleChannel.Mission);
    /// // 可能返回：<color=#4FC3F7>目标已摧毁</color>
    /// </example>
    public static string Process(string plainText, string themeTag, SubtitleChannel channelTag = SubtitleChannel.System)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText ?? string.Empty;
        }

        if (!_registeredThemes.TryGetValue(themeTag, out var theme) || theme == null)
        {
            // 主题未注册，返回原文本
            return plainText;
        }

        string result = plainText;
        bool hasChannelOverride = false;

        // ── 第 1 层：频道覆盖 ──
        if (theme.ChannelOverrides != null)
        {
            foreach (var rule in theme.ChannelOverrides)
            {
                if (rule.ChannelTag == channelTag)
                {
                    result = WrapColor(result, rule.ColorHex, theme.TargetType);
                    hasChannelOverride = true;

                    if (rule.ApplyBold)
                    {
                        result = WrapBold(result, theme.TargetType);
                    }

                    if (rule.ApplyItalic)
                    {
                        result = WrapItalic(result, theme.TargetType);
                    }

                    break; // 第一个匹配生效
                }
            }
        }

        // ── 第 2 层：关键词高亮（在频道覆盖后的文本上操作） ──
        if (theme.KeywordRules != null)
        {
            foreach (var kwRule in theme.KeywordRules)
            {
                if (string.IsNullOrEmpty(kwRule.Keyword))
                {
                    continue;
                }

                var comparison = kwRule.CaseSensitive
                    ? System.StringComparison.Ordinal
                    : System.StringComparison.OrdinalIgnoreCase;

                int index = 0;
                while (true)
                {
                    index = result.IndexOf(kwRule.Keyword, index, comparison);
                    if (index < 0)
                    {
                        break;
                    }

                    // 提取匹配片段
                    string matched = result.Substring(index, kwRule.Keyword.Length);
                    string colored = WrapColor(matched, kwRule.ColorHex, theme.TargetType);

                    // 替换
                    result = result.Substring(0, index)
                             + colored
                             + result.Substring(index + kwRule.Keyword.Length);

                    index += colored.Length;
                }
            }
        }

        // ── 第 3 层：默认整体着色（仅当无频道覆盖时） ──
        if (!hasChannelOverride && !string.IsNullOrEmpty(theme.DefaultColorHex))
        {
            result = WrapColor(result, theme.DefaultColorHex, theme.TargetType);
        }

        return result;
    }

    // ──────────────────────── 富文本包装器（可移植 API） ────────────────────────

    /// <summary>
    /// 将文本包裹为 TextMeshPro 兼容的富文本颜色标签。
    /// 输出格式：<![CDATA[<color=#RRGGBB>text</color>]]>
    /// </summary>
    /// <param name="text">要着色的文本</param>
    /// <param name="colorHex">颜色值（十六进制，如 "#FF4444"）</param>
    /// <returns>带 <color> 标签的文本</returns>
    public static string WrapTMPColor(string text, string colorHex)
    {
        return WrapTag(text, "color", colorHex);
    }

    /// <summary>
    /// 将文本包裹为 UI Toolkit Label 兼容的富文本颜色标签。
    /// UITK Label 支持与 TMP 相同的 <![CDATA[<color=#RRGGBB>text</color>]]> 格式。
    /// 在此统一封装，如果未来 UITK 富文本语法变化，可在此单一修改。
    /// </summary>
    /// <param name="text">要着色的文本</param>
    /// <param name="colorHex">颜色值（十六进制，如 "#FF4444"）</param>
    /// <returns>带 <color> 标签的文本</returns>
    public static string WrapUITKColor(string text, string colorHex)
    {
        return WrapTag(text, "color", colorHex);
    }

    /// <summary>
    /// 将文本包裹为 TMP 富文本粗体标签。
    /// 输出格式：<![CDATA[<b>text</b>]]>
    /// </summary>
    public static string WrapTMPBold(string text)
    {
        return WrapSimpleTag(text, "b");
    }

    /// <summary>
    /// 将文本包裹为 UI Toolkit 富文本粗体标签。
    /// </summary>
    public static string WrapUITKBold(string text)
    {
        return WrapSimpleTag(text, "b");
    }

    /// <summary>
    /// 将文本包裹为 TMP 富文本斜体标签。
    /// 输出格式：<![CDATA[<i>text</i>]]>
    /// </summary>
    public static string WrapTMPItalic(string text)
    {
        return WrapSimpleTag(text, "i");
    }

    /// <summary>
    /// 将文本包裹为 UI Toolkit 富文本斜体标签。
    /// </summary>
    public static string WrapUITKItalic(string text)
    {
        return WrapSimpleTag(text, "i");
    }

    // ──────────────────────── 缓存复用：可见字符截取 ────────────────────────

    /// <summary>
    /// 从已着色的富文本中截取前 visibleCharCount 个可见字符对应的文本片段。
    /// 富文本中的 <![CDATA[<color=#RRGGBB>]]> 等标签不计入可见字符数。
    /// 
    /// 例如：
    /// richText = <![CDATA[<color=#FF0>目标已摧毁</color>]]>
    /// GetVisibleSubstring(richText, 2) → <![CDATA[<color=#FF0>目标</color>]]>
    /// </summary>
    /// <param name="richText">已通过 Process() 生成的富文本</param>
    /// <param name="visibleCharCount">需要的可见字符数（0 返回空，>= 可见总数返回完整富文本）</param>
    /// <returns>
    /// 前 visibleCharCount 个可见字符对应的带标签富文本片段。
    /// 若 richText 为 null/空，返回 string.Empty。
    /// </returns>
    public static string GetVisibleSubstring(string richText, int visibleCharCount)
    {
        if (string.IsNullOrEmpty(richText) || visibleCharCount <= 0)
        {
            return string.Empty;
        }

        int visibleCount = 0;
        int totalLength = richText.Length;
        int outputEndIndex = 0;

        for (int i = 0; i < totalLength; i++)
        {
            if (richText[i] == '<')
            {
                // 跳过整个富文本标签（<color=#xxx>、</color>、<b>、</b>、<i>、</i>）
                int closeIndex = richText.IndexOf('>', i + 1);
                if (closeIndex > i)
                {
                    i = closeIndex; // 循环末尾 i++ 会移过 '>'，停在下个字符
                    outputEndIndex = i + 1;
                    continue;
                }

                // 未找到闭合 >，跳过分号本身
                outputEndIndex = i + 1;
                continue;
            }

            visibleCount++;
            outputEndIndex = i + 1;

            if (visibleCount >= visibleCharCount)
            {
                break;
            }
        }

        string visiblePart = richText.Substring(0, outputEndIndex);

        // 补全未闭合的富文本标签
        visiblePart = AppendClosingTags(visiblePart);

        return visiblePart;
    }

    /// <summary>
    /// 为截取的富文本片段补全所有未闭合的标签。
    /// 确保 TMP/UITK 能正确解析片段。
    /// </summary>
    private static string AppendClosingTags(string fragment)
    {
        if (string.IsNullOrEmpty(fragment))
        {
            return fragment;
        }

        int length = fragment.Length;
        var tagStack = new System.Collections.Generic.Stack<string>();
        int i = 0;

        while (i < length)
        {
            if (fragment[i] == '<')
            {
                int closeIndex = fragment.IndexOf('>', i + 1);
                if (closeIndex < 0)
                {
                    break;
                }

                string tagContent = fragment.Substring(i + 1, closeIndex - i - 1);

                if (tagContent.StartsWith("/"))
                {
                    // 闭合标签 → 出栈
                    string tagName = tagContent.Substring(1).Trim();
                    if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                    {
                        tagStack.Pop();
                    }
                }
                else
                {
                    // 开始标签 → 入栈（提取 tag name 时忽略 '=' 后的属性和空格）
                    string tagName = tagContent.Split('=', ' ')[0].Trim();
                    tagStack.Push(tagName);
                }

                i = closeIndex + 1;
                continue;
            }

            i++;
        }

        // 按后进先出顺序补全闭合标签
        string closingTags = string.Empty;
        while (tagStack.Count > 0)
        {
            string tagName = tagStack.Pop();
            closingTags += $"</{tagName}>";
        }

        return fragment + closingTags;
    }

    // ──────────────────────── 内部实现 ────────────────────────

    private static string WrapColor(string text, string colorHex, RenderTarget target)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(colorHex))
        {
            return text ?? string.Empty;
        }

        return WrapTag(text, "color", colorHex);
    }

    private static string WrapBold(string text, RenderTarget target)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return WrapSimpleTag(text, "b");
    }

    private static string WrapItalic(string text, RenderTarget target)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return WrapSimpleTag(text, "i");
    }

    private static string WrapTag(string text, string tagName, string value)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        // 格式：<tag=value>text</tag>
        return $"<{tagName}={value}>{text}</{tagName}>";
    }

    private static string WrapSimpleTag(string text, string tagName)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        // 格式：<tag>text</tag>
        return $"<{tagName}>{text}</{tagName}>";
    }
}
