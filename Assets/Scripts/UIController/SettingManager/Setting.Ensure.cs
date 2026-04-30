using UnityEngine;

public partial class SettingManager : MonoBehaviour
{

    private void EnsureInitialized()//确保设置管理器已初始化，如果尚未初始化则执行初始化逻辑
    {
        // 如果已经初始化过了，就直接返回，避免重复执行初始化逻辑，节省性能开销
        if (_isInitialized)
        {
            return;
        }

        if (musicVolumeSlider == null)
        {
            Debug.LogError("SettingManager: musicVolumeSlider 未设置，请在 Inspector 中分配音乐音量滑动条组件。", this);
        }

        if (sfxVolumeSlider == null)
        {
            Debug.LogError("SettingManager: sfxVolumeSlider 未设置，请在 Inspector 中分配音效音量滑动条组件。", this);
        }

        if (applyButton == null)
        {
            Debug.LogError("SettingManager: applyButton 未设置，请在 Inspector 中分配应用按钮组件。", this);
        }

        if (cancelButton == null)
        {
            Debug.LogError("SettingManager: cancelButton 未设置，请在 Inspector 中分配取消按钮组件。", this);
        }

        if (_audioManager == null)
        {
            Debug.LogWarning("SettingManager: AudioManager 尚未准备好，设置会在 AudioManager 可用后再应用。", this);
        }

        if (_audioManager == null)
        {
            _audioManager = AudioManager.Instance;
        }


        BindUIListeners();
        LoadSettingsFromStorageOrDefault();
        BuildCategoryVolumeUI();
        RefreshUI();

        _isInitialized = true;
    }

}