using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// 设置管理器，负责管理游戏中的各种设置选项，例如音量、图像质量、控制方式等。提供接口供其他系统调用，以获取和修改当前的设置状态。
/// </summary>
public partial class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [Serializable]
    public struct AudioSettingState
    {
        [Range(0f, 1f)] public float MusicVolume;
        [Range(0f, 1f)] public float SfxVolume;
        public AudioCategoryVolumeSetting[] CategoryVolumes;
    }

    public event Action<AudioSettingState> SettingsChanged;
    public event Action<AudioSettingState> SettingsApplied;

    // [SerializeField] private TMP_Dropdown resolutionDropdown;//引用分辨率下拉菜单组件，用于显示和选择可用的屏幕分辨率选项
    // [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [SerializeField] private TMP_Text sfxVolumeValueText;
    [SerializeField] private RectTransform categoryVolumeContent;
    [SerializeField] private GameObject categoryVolumeItemPrefab;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button applyButton;

    // private Resolution[] _availableResolutions;// 存储可用的屏幕分辨率列表，供分辨率设置选项使用
    [SerializeField] private AudioSettingState _currentAudioSettings;
    [SerializeField] private AudioSettingState _appliedAudioSettings;
    private readonly List<AudioCategoryVolumeItem> _categoryVolumeItems = new List<AudioCategoryVolumeItem>();
    private AudioManager _audioManager;
    private bool _isInitialized;

    // private int _applyQualityIndex;
    // private int _applyResolutionIndex;

    private void Awake()
    {
        //如果已经存在实例且不是当前对象，则销毁当前对象；否则设置实例并标记为不销毁
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        RefreshUI();
    }




    /// <summary>
    /// 这个方法用于初始化设置管理器，接受一个 AudioManager 实例作为参数，以便在设置界面中调整音量时能够直接调用 AudioManager 的接口来应用设置。通过这个方法，可以确保在游戏开始时或者在需要重新初始化设置管理器时，能够正确地设置和绑定相关的组件和事件监听器，并且加载当前的设置状态以供界面显示和调整。
    /// 例如，在游戏开始时，GameManager 可以调用 SettingManager.Instance.Initialize(audioManager) 来传入当前的 AudioManager 实例，并且确保设置管理器能够正确地应用和同步音量设置。当玩家在设置界面调整音量滑动条时，可以直接调用 AudioManager 的 SetVolume 方法来应用新的音量设置，而不需要在每次调整时都进行额外的查找和获取 AudioManager 实例的操作。
    /// </summary>
    public void Initialize(AudioManager audioManager)
    {
        _audioManager = audioManager != null ? audioManager : AudioManager.Instance;
        EnsureInitialized();
        ApplyCurrentSettingsToAudio();
    }



    /// <summary>
    /// 这个方法用于绑定设置界面中 UI 组件的事件监听器，例如滑动条的值改变事件和按钮的点击事件。通过这个方法，可以确保当玩家在设置界面调整选项时，能够正确地更新内部的设置状态，并且在点击应用或取消按钮时能够执行相应的操作。
    /// 例如，当玩家调整音乐音量滑动条时，可以更新 _applyMusicVolume 变量的值，并且更新界面上显示的音量百分比文本。当玩家点击应用按钮时，可以调用 ApplySettings 方法将当前的设置应用到游戏中，并保存到 PlayerPrefs。当玩家点击取消按钮时，可以调用 CancelSettings 方法恢复到上次应用的设置状态，并刷新界面显示。
    /// </summary>
    private void BindUIListeners()
    {
        // Debug.Log("【SettingManager】正在绑定滑条事件...");
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            // Debug.Log("已绑定音乐音量滑条事件");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            // Debug.Log("已绑定SFX音量滑条事件");
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplySettings);
            applyButton.onClick.AddListener(ApplySettings);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelSettings);
            cancelButton.onClick.AddListener(CancelSettings);
        }

        if (categoryVolumeContent == null)
        {
            Debug.LogError("SettingManager: categoryVolumeContent 未设置，请在 Inspector 中分配分类音量 Content 节点。", this);
        }

        if (categoryVolumeItemPrefab == null)
        {
            Debug.LogError("SettingManager: categoryVolumeItemPrefab 未设置，请在 Inspector 中分配分类音量项预制体。", this);
        }
    }
    /// <summary>
    /// 刷新设置界面的 UI 显示，包括滑条值、分类音量项和音量标签。
    /// </summary>
    private void RefreshUI()
    {
        // if (musicVolumeSlider != null) musicVolumeSlider.interactable = true;
        // if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = true;

        SetSliderValueWithoutNotify(musicVolumeSlider, _currentAudioSettings.MusicVolume);
        SetSliderValueWithoutNotify(sfxVolumeSlider, _currentAudioSettings.SfxVolume);
        RefreshCategoryVolumeUI();
        UpdateVolumeLabels();

        if (musicVolumeSlider != null) musicVolumeSlider.interactable = true;
        if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = true;

    }

    private void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private void UpdateVolumeLabels()
    {
        if (musicVolumeValueText != null)
        {
            musicVolumeValueText.text = FormatVolumePercent(_currentAudioSettings.MusicVolume);
        }

        if (sfxVolumeValueText != null)
        {
            sfxVolumeValueText.text = FormatVolumePercent(_currentAudioSettings.SfxVolume);
        }
    }

    private string FormatVolumePercent(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }


    private void OnMusicVolumeChanged(float value)
    {
        // Debug.Log($"滑条被拖动: {value}");
        _currentAudioSettings.MusicVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();

        // 【核心修复：实时应用到音频管理器】
        if (_audioManager != null)
        {
            _audioManager.SetGlobalVolume(_currentAudioSettings.MusicVolume, 1);
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        // Debug.Log($"滑条被拖动: {value}");
        _currentAudioSettings.SfxVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();


        // 【核心修复：实时应用到音频管理器】
        if (_audioManager != null)
        {
            _audioManager.SetGlobalVolume(_currentAudioSettings.SfxVolume, 0);
        }
    }

    private void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke(CloneAudioSettingState(_currentAudioSettings));
    }

    public void ApplySettings()
    {
        ApplyCurrentSettingsToAudio();
        SaveSettingsToStorage();

        _appliedAudioSettings = CloneAudioSettingState(_currentAudioSettings);

        RefreshUI();
        SettingsApplied?.Invoke(CloneAudioSettingState(_currentAudioSettings));
    }

    private void CancelSettings()
    {
        _currentAudioSettings = CloneAudioSettingState(_appliedAudioSettings);

        RefreshUI();


        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPauseUI();
        }
    }

    public void ApplyCurrentSettingsToAudio()
    {
        if (_audioManager == null)
        {
            _audioManager = AudioManager.Instance;
        }

        if (_audioManager == null)
        {
            return;
        }

        _audioManager.SetGlobalVolume(_currentAudioSettings.MusicVolume, 1);
        _audioManager.SetGlobalVolume(_currentAudioSettings.SfxVolume, 0);
        _audioManager.SetCategoryVolumes(_currentAudioSettings.CategoryVolumes);
    }

    public AudioSettingState GetCurrentAudioSettings()
    {
        return CloneAudioSettingState(_currentAudioSettings);
    }

    public void SetMusicVolume(float value)
    {
        _currentAudioSettings.MusicVolume = Mathf.Clamp01(value);
        RefreshUI();
        NotifySettingsChanged();
    }

    public void SetSfxVolume(float value)
    {
        _currentAudioSettings.SfxVolume = Mathf.Clamp01(value);
        RefreshUI();
        NotifySettingsChanged();
    }

    public void SetAudioSettings(AudioSettingState state)
    {
        _currentAudioSettings = CloneAudioSettingState(state);
        _currentAudioSettings.MusicVolume = Mathf.Clamp01(_currentAudioSettings.MusicVolume);
        _currentAudioSettings.SfxVolume = Mathf.Clamp01(_currentAudioSettings.SfxVolume);
        _currentAudioSettings.CategoryVolumes = NormalizeCategoryVolumes(_currentAudioSettings.CategoryVolumes);
        RefreshUI();
        NotifySettingsChanged();
    }

    public float GetCategoryVolume(AudioVolumeCategory category)
    {
        return GetCategoryVolumeFromSettings(_currentAudioSettings.CategoryVolumes, category);
    }

    public AudioCategoryVolumeSetting[] GetCategoryVolumeSettings()
    {
        return CloneCategoryVolumes(_currentAudioSettings.CategoryVolumes);
    }

    public void SetCategoryVolume(AudioVolumeCategory category, float value)
    {
        _currentAudioSettings.CategoryVolumes = SetCategoryVolumeInternal(_currentAudioSettings.CategoryVolumes, category, value);
        RefreshUI();
        NotifySettingsChanged();
    }

    public void SetCategoryVolumeSettings(AudioCategoryVolumeSetting[] settings)
    {
        _currentAudioSettings.CategoryVolumes = NormalizeCategoryVolumes(settings);
        RefreshUI();
        NotifySettingsChanged();
    }

    public void ShowSettingsPanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettingsUI();
        }
    }

    public void HideSettingsPanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPauseUI();
        }
    }

    private static AudioSettingState CloneAudioSettingState(AudioSettingState state)
    {
        state.CategoryVolumes = CloneCategoryVolumes(state.CategoryVolumes);
        return state;
    }

    private static AudioCategoryVolumeSetting[] CloneCategoryVolumes(AudioCategoryVolumeSetting[] settings)
    {
        if (settings == null || settings.Length == 0)
        {
            return CreateDefaultCategoryVolumes();
        }

        AudioCategoryVolumeSetting[] clonedSettings = new AudioCategoryVolumeSetting[settings.Length];
        Array.Copy(settings, clonedSettings, settings.Length);
        return NormalizeCategoryVolumes(clonedSettings);
    }

    private static AudioCategoryVolumeSetting[] CreateDefaultCategoryVolumes()
    {
        AudioVolumeCategory[] categories = (AudioVolumeCategory[])Enum.GetValues(typeof(AudioVolumeCategory));
        AudioCategoryVolumeSetting[] defaultSettings = new AudioCategoryVolumeSetting[categories.Length];

        for (int index = 0; index < categories.Length; index++)
        {
            defaultSettings[index] = new AudioCategoryVolumeSetting
            {
                Category = categories[index],
                Volume = 1f
            };
        }

        return defaultSettings;
    }

    private static AudioCategoryVolumeSetting[] NormalizeCategoryVolumes(AudioCategoryVolumeSetting[] settings)
    {
        AudioCategoryVolumeSetting[] normalizedSettings = CreateDefaultCategoryVolumes();
        if (settings == null || settings.Length == 0)
        {
            return normalizedSettings;
        }

        for (int index = 0; index < normalizedSettings.Length; index++)
        {
            AudioVolumeCategory category = normalizedSettings[index].Category;
            if (TryGetCategoryVolume(settings, category, out float volume))
            {
                normalizedSettings[index].Volume = Mathf.Clamp01(volume);
            }
        }

        return normalizedSettings;
    }

    private static bool TryGetCategoryVolume(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category, out float volume)
    {
        if (settings != null)
        {
            for (int index = 0; index < settings.Length; index++)
            {
                if (settings[index].Category == category)
                {
                    volume = settings[index].Volume;
                    return true;
                }
            }
        }

        volume = 1f;
        return false;
    }

    private static float GetCategoryVolumeFromSettings(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category)
    {
        return TryGetCategoryVolume(settings, category, out float volume)
            ? Mathf.Clamp01(volume)
            : 1f;
    }

    private static AudioCategoryVolumeSetting[] SetCategoryVolumeInternal(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category, float value)
    {
        AudioCategoryVolumeSetting[] normalizedSettings = NormalizeCategoryVolumes(settings);
        for (int index = 0; index < normalizedSettings.Length; index++)
        {
            if (normalizedSettings[index].Category != category)
            {
                continue;
            }

            normalizedSettings[index].Volume = Mathf.Clamp01(value);
            break;
        }

        return normalizedSettings;
    }



}