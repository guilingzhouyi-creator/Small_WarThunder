using UnityEngine;
using NNewUIFramework;

public partial class SettingManager : UGUIViewAdapter
{
    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_subSettingEntries == null || _subSettingEntries.Count <= 0)
        {
            Debug.LogWarning("SettingManager: _subSettingEntries 为空，应用/取消按钮由 Tab 控制器路由。", this);
        }

        if (_settingTabNavigationButtons == null || _settingTabNavigationButtons.Count <= 0)
        {
            Debug.LogWarning("SettingManager: _settingTabNavigationButtons 为空，将仅支持鼠标点击切换。", this);
        }

        BindUIListeners();

        // 默认打开第一个 Tab
        if (_subSettingEntries != null && _subSettingEntries.Count > 0)
        {
            SwitchTab(0);
        }

        _isInitialized = true;
    }
}
