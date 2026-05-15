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

    /// <summary>设置导航按钮名称</summary>
    public const string ButtonNameGeneralTab = "GeneralPannelButton";
    public const string ButtonNameVisualTab = "VisualPannelButton";
    public const string ButtonNameAudioTab = "SoundPannelButton";
    public const string ButtonNameKeyBindingTab = "KeyBingPannelButton";

    /// <summary>设置面板名称</summary>
    public const string PanelNameGeneral = "GeneralSettingPannel";
    public const string PanelNameVisual = "VisualSettingPannel";
    public const string PanelNameAudio = "SoundSettingPannel";
    public const string PanelNameKeyBinding = "KeyBingSettingPannel";

    /// <summary>通用操作按钮基础名称</summary>
    public const string ButtonNameApply = "ApplyButton";
    public const string ButtonNameCancel = "CancelButton";

    /// <summary>各 Tab 具体操作按钮名称</summary>
    public const string ButtonNameGeneralApply = "GApplyButton";
    public const string ButtonNameGeneralCancel = "GCancelButton";
    public const string ButtonNameGeneralReset = "GResetButton";
    public const string ButtonNameVisualApply = "VApplyButton";
    public const string ButtonNameVisualCancel = "VCancelButton";
    public const string ButtonNameVisualReset = "VResetButton";
    public const string ButtonNameAudioApply = "SApplyButton";
    public const string ButtonNameAudioCancel = "SCancelButton";
    public const string ButtonNameKeyBindingApply = "KApplyButton";
    public const string ButtonNameKeyBindingCancel = "KCancelButton";

    /// <summary>PlayerPrefs 中音频设置的存储键</summary>
    public const string PrefsKeyAudioSettings = "SmallWarThunder.AudioSettings";

    public static bool TryGetTabKeyFromNavigationButtonName(string buttonName, out string tabKey)
    {
        if (string.IsNullOrEmpty(buttonName))
        {
            tabKey = null;
            return false;
        }

        if (string.Equals(buttonName, ButtonNameGeneralTab, System.StringComparison.OrdinalIgnoreCase))
        {
            tabKey = TabKeyGeneral;
            return true;
        }

        if (string.Equals(buttonName, ButtonNameVisualTab, System.StringComparison.OrdinalIgnoreCase))
        {
            tabKey = TabKeyVisual;
            return true;
        }

        if (string.Equals(buttonName, ButtonNameAudioTab, System.StringComparison.OrdinalIgnoreCase))
        {
            tabKey = TabKeyAudio;
            return true;
        }

        if (string.Equals(buttonName, ButtonNameKeyBindingTab, System.StringComparison.OrdinalIgnoreCase))
        {
            tabKey = TabKeyKeyBinding;
            return true;
        }

        tabKey = null;
        return false;
    }

    public static string GetPanelName(string tabKey)
    {
        switch (tabKey)
        {
            case TabKeyGeneral:
                return PanelNameGeneral;
            case TabKeyVisual:
                return PanelNameVisual;
            case TabKeyAudio:
                return PanelNameAudio;
            case TabKeyKeyBinding:
                return PanelNameKeyBinding;
            default:
                return null;
        }
    }

    public static string[] GetActionButtonNames(string tabKey, bool isApplyButton)
    {
        string genericName = isApplyButton ? ButtonNameApply : ButtonNameCancel;

        switch (tabKey)
        {
            case TabKeyGeneral:
                return isApplyButton ? new[] { ButtonNameGeneralApply, genericName } : new[] { ButtonNameGeneralCancel, genericName };
            case TabKeyVisual:
                return isApplyButton ? new[] { ButtonNameVisualApply, genericName } : new[] { ButtonNameVisualCancel, genericName };
            case TabKeyAudio:
                return isApplyButton ? new[] { ButtonNameAudioApply, genericName } : new[] { ButtonNameAudioCancel, genericName };
            case TabKeyKeyBinding:
                return isApplyButton ? new[] { ButtonNameKeyBindingApply, genericName } : new[] { ButtonNameKeyBindingCancel, genericName };
            default:
                return new[] { genericName };
        }
    }

    public static string GetPlaceholderTitle(string tabKey)
    {
        switch (tabKey)
        {
            case TabKeyGeneral:
                return "通用设置";
            case TabKeyVisual:
                return "画面设置";
            case TabKeyAudio:
                return "音频设置";
            case TabKeyKeyBinding:
                return "按键设置";
            default:
                return "设置";
        }
    }
}
