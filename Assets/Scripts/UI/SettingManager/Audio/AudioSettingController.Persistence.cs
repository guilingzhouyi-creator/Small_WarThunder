using System;
using UnityEngine;

/// <summary>
/// AudioSettingController 持久化 Partial：负责音频设置的 PlayerPrefs 读写。
/// </summary>
public partial class AudioSettingController
{
    private const int AudioSettingsPrefsVersion = 1;

    [Serializable]
    private class AudioSettingsSaveData
    {
        public int Version = AudioSettingsPrefsVersion;
        public float MusicVolume = 0.4f;
        public float SfxVolume = 1f;
        public AudioCategoryVolumeSetting[] CategoryVolumes;
    }

    private void LoadSettingsFromStorageOrDefault()
    {
        if (TryLoadSettingsFromStorage(out AudioSettingState loadedState))
        {
            _currentAudioSettings = loadedState;
        }
        else
        {
            _currentAudioSettings = CreateDefaultAudioSettingState();
        }

        _currentAudioSettings = NormalizeAudioSettingState(_currentAudioSettings);
        _appliedAudioSettings = CloneAudioSettingState(_currentAudioSettings);
        Debug.Log("[AudioSettingController] Settings loaded");
    }

    private void SaveSettingsToStorage()
    {
        AudioSettingsSaveData saveData = new AudioSettingsSaveData
        {
            MusicVolume = Mathf.Clamp01(_currentAudioSettings.MusicVolume),
            SfxVolume = Mathf.Clamp01(_currentAudioSettings.SfxVolume),
            CategoryVolumes = NormalizeCategoryVolumes(_currentAudioSettings.CategoryVolumes)
        };

        PlayerPrefs.SetString(SettingConstants.PrefsKeyAudioSettings, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
        Debug.Log("[AudioSettingController] Settings saved to storage");
    }

    private static bool TryLoadSettingsFromStorage(out AudioSettingState state)
    {
        state = default;

        if (!PlayerPrefs.HasKey(SettingConstants.PrefsKeyAudioSettings))
        {
            return false;
        }

        string json = PlayerPrefs.GetString(SettingConstants.PrefsKeyAudioSettings, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        AudioSettingsSaveData saveData;
        try
        {
            saveData = JsonUtility.FromJson<AudioSettingsSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AudioSettingController] 读取音频设置失败，已回退到默认值。{exception.Message}");
            return false;
        }

        if (saveData == null || saveData.Version != AudioSettingsPrefsVersion)
        {
            return false;
        }

        state.MusicVolume = Mathf.Clamp01(saveData.MusicVolume);
        state.SfxVolume = Mathf.Clamp01(saveData.SfxVolume);
        state.CategoryVolumes = NormalizeCategoryVolumes(saveData.CategoryVolumes);
        return true;
    }

    private static AudioSettingState CreateDefaultAudioSettingState()
    {
        AudioSettingState state = default;
        state.MusicVolume = 0.4f;
        state.SfxVolume = 1f;
        state.CategoryVolumes = CreateDefaultCategoryVolumes();
        return state;
    }

    private static AudioSettingState NormalizeAudioSettingState(AudioSettingState state)
    {
        state.MusicVolume = Mathf.Clamp01(state.MusicVolume);
        state.SfxVolume = Mathf.Clamp01(state.SfxVolume);
        state.CategoryVolumes = NormalizeCategoryVolumes(state.CategoryVolumes);
        return state;
    }
}
