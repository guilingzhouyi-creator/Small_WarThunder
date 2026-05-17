using System;
using System.Collections.Generic;
using NNewUIFramework;
using NSettingSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音频设置 Tab 控制器：管理音量滑块 UI、分类音量列表、持久化存储。
/// 实现 ISettingTabController 由 SettingManager 统一调度生命周期与按钮路由。
/// apply/cancel 按钮由 SettingManager 统一管理，不再自绑定。
/// </summary>
public partial class AudioSettingController : MonoBehaviour, ISettingTabController
{
    public string tabKey => SettingConstants.TabKeyAudio;

    public event Action<AudioSettingState> SettingsChanged;
    public event Action<AudioSettingState> SettingsApplied;

    [Header("Audio Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [SerializeField] private TMP_Text sfxVolumeValueText;

    [Header("Category Volumes")]
    [SerializeField] private RectTransform categoryVolumeContent;
    [SerializeField] private GameObject categoryVolumeItemPrefab;

    private AudioSettingState _currentAudioSettings;
    private AudioSettingState _appliedAudioSettings;
    private readonly List<AudioCategoryVolumeItem> _categoryVolumeItems = new List<AudioCategoryVolumeItem>();
    private AudioManager _audioManager;
    private bool _isInitialized;

    public void OnTabOpened()
    {
        Debug.Log("[AudioSettingController] OnTabOpened");
        EnsureInitialized();
        LoadSettingsFromStorageOrDefault();
        BuildCategoryVolumeUI();
        RefreshUI();
    }

    public void OnTabClosed()
    {
        Debug.Log("[AudioSettingController] OnTabClosed");
    }

    public bool OnBackRequested()
    {
        Debug.Log("[AudioSettingController] OnBackRequested — nothing to save, allow close");
        return true;
    }

    /// <summary>SettingManager 统一调度的 Apply 入口。</summary>
    public SettingActionResult OnApplyRequested()
    {
        Debug.Log("[AudioSettingController] OnApplyRequested");
        return ApplySettings();
    }

    public SettingActionResult OnResetRequested()
    {
        return SettingActionResult.NoOp(tabKey, SettingActionType.Reset);
    }

    /// <summary>SettingManager 统一调度的 Cancel 入口。</summary>
    public SettingActionResult OnCancelRequested()
    {
        Debug.Log("[AudioSettingController] OnCancelRequested");
        return CancelSettings();
    }

    public void Initialize(AudioManager audioManager)
    {
        _audioManager = audioManager != null ? audioManager : AudioManager.Instance;
        EnsureInitialized();
        ApplyCurrentSettingsToAudio();
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

    private void Awake()
    {
        Debug.Log("[AudioSettingController] Awake");
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

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        ValidateRequiredReferences();
        NormalizePrimarySliderInteraction(musicVolumeSlider, musicVolumeValueText);
        NormalizePrimarySliderInteraction(sfxVolumeSlider, sfxVolumeValueText);
        BindUIListeners();
        _isInitialized = true;
        Debug.Log("[AudioSettingController] Initialized");
    }

    private void ValidateRequiredReferences()
    {
        if (musicVolumeSlider == null) Debug.LogError("AudioSettingController: musicVolumeSlider 未设置", this);
        if (sfxVolumeSlider == null) Debug.LogError("AudioSettingController: sfxVolumeSlider 未设置", this);
        if (categoryVolumeContent == null) Debug.LogError("AudioSettingController: categoryVolumeContent 未设置", this);
        if (categoryVolumeItemPrefab == null) Debug.LogError("AudioSettingController: categoryVolumeItemPrefab 未设置", this);
        if (_audioManager == null) Debug.LogWarning("AudioSettingController: AudioManager 尚未准备好", this);
    }

    private void BindUIListeners()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }

    private void RefreshUI()
    {
        SetSliderValueWithoutNotify(musicVolumeSlider, _currentAudioSettings.MusicVolume);
        SetSliderValueWithoutNotify(sfxVolumeSlider, _currentAudioSettings.SfxVolume);
        EnsureCategoryVolumeUIMatchesSettings();
        RefreshCategoryVolumeUI();
        UpdateVolumeLabels();

        if (musicVolumeSlider != null) musicVolumeSlider.interactable = true;
        if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = true;
    }

    private void UpdateVolumeLabels()
    {
        if (musicVolumeValueText != null) musicVolumeValueText.text = FormatVolumePercent(_currentAudioSettings.MusicVolume);
        if (sfxVolumeValueText != null) sfxVolumeValueText.text = FormatVolumePercent(_currentAudioSettings.SfxVolume);
    }

    private static string FormatVolumePercent(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    private void OnMusicVolumeChanged(float value)
    {
        _currentAudioSettings.MusicVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();

        if (_audioManager != null)
        {
            _audioManager.SetGlobalVolume(_currentAudioSettings.MusicVolume, 1);
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        _currentAudioSettings.SfxVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();

        if (_audioManager != null)
        {
            _audioManager.SetGlobalVolume(_currentAudioSettings.SfxVolume, 0);
        }
    }

    private void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke(CloneAudioSettingState(_currentAudioSettings));
    }

    private SettingActionResult ApplySettings()
    {
        ApplyCurrentSettingsToAudio();
        SaveSettingsToStorage();

        _appliedAudioSettings = CloneAudioSettingState(_currentAudioSettings);

        RefreshUI();
        SettingsApplied?.Invoke(CloneAudioSettingState(_currentAudioSettings));
        Debug.Log("[AudioSettingController] Settings applied and saved");
        return SettingActionResult.Success(tabKey, SettingActionType.Apply, "音频设置已应用");
    }

    private SettingActionResult CancelSettings()
    {
        _currentAudioSettings = CloneAudioSettingState(_appliedAudioSettings);
        RefreshUI();
        return SettingActionResult.CancelExit(tabKey);
    }

    public void ApplyCurrentSettingsToAudio()
    {
        if (_audioManager == null) _audioManager = AudioManager.Instance;
        if (_audioManager == null) return;

        _audioManager.SetGlobalVolume(_currentAudioSettings.MusicVolume, 1);
        _audioManager.SetGlobalVolume(_currentAudioSettings.SfxVolume, 0);
        _audioManager.SetCategoryVolumes(_currentAudioSettings.CategoryVolumes);
    }

    private void BuildCategoryVolumeUI()
    {
        foreach (AudioCategoryVolumeItem item in _categoryVolumeItems)
        {
            if (item != null) Destroy(item.gameObject);
        }

        _categoryVolumeItems.Clear();

        if (categoryVolumeContent == null || categoryVolumeItemPrefab == null) return;

        AudioCategoryVolumeSetting[] settings = _currentAudioSettings.CategoryVolumes;
        if (settings == null || settings.Length == 0) return;

        for (int i = 0; i < settings.Length; i++)
        {
            AudioCategoryVolumeSetting setting = settings[i];
            GameObject itemGo = Instantiate(categoryVolumeItemPrefab, categoryVolumeContent);
            if (itemGo == null) continue;

            AudioCategoryVolumeItem item = itemGo.GetComponent<AudioCategoryVolumeItem>();
            if (item == null)
            {
                Destroy(itemGo);
                continue;
            }

            item.Bind(setting.Category, setting.Volume, OnCategoryVolumeChanged);
            _categoryVolumeItems.Add(item);
        }
    }

    private void RefreshCategoryVolumeUI()
    {
        AudioCategoryVolumeSetting[] settings = _currentAudioSettings.CategoryVolumes;
        if (settings == null) return;

        for (int i = 0; i < _categoryVolumeItems.Count && i < settings.Length; i++)
        {
            _categoryVolumeItems[i]?.SetVolumeWithoutNotify(settings[i].Volume);
        }
    }

    private void OnCategoryVolumeChanged(AudioVolumeCategory category, float value)
    {
        _currentAudioSettings.CategoryVolumes = SetCategoryVolumeInternal(_currentAudioSettings.CategoryVolumes, category, value);
        UpdateVolumeLabels();
        NotifySettingsChanged();
    }

    private void EnsureCategoryVolumeUIMatchesSettings()
    {
        AudioCategoryVolumeSetting[] settings = _currentAudioSettings.CategoryVolumes;
        int expectedCount = settings != null ? settings.Length : 0;

        if (_categoryVolumeItems.Count != expectedCount)
        {
            BuildCategoryVolumeUI();
            return;
        }

        for (int index = 0; index < _categoryVolumeItems.Count; index++)
        {
            if (_categoryVolumeItems[index] == null)
            {
                BuildCategoryVolumeUI();
                return;
            }
        }
    }

    private static AudioSettingState CloneAudioSettingState(AudioSettingState state)
    {
        state.CategoryVolumes = CloneCategoryVolumes(state.CategoryVolumes);
        return state;
    }

    private static AudioCategoryVolumeSetting[] CloneCategoryVolumes(AudioCategoryVolumeSetting[] settings)
    {
        if (settings == null || settings.Length == 0) return CreateDefaultCategoryVolumes();

        AudioCategoryVolumeSetting[] cloned = new AudioCategoryVolumeSetting[settings.Length];
        Array.Copy(settings, cloned, settings.Length);
        return NormalizeCategoryVolumes(cloned);
    }

    private static AudioCategoryVolumeSetting[] CreateDefaultCategoryVolumes()
    {
        AudioVolumeCategory[] categories = (AudioVolumeCategory[])Enum.GetValues(typeof(AudioVolumeCategory));
        AudioCategoryVolumeSetting[] defaults = new AudioCategoryVolumeSetting[categories.Length];

        for (int i = 0; i < categories.Length; i++)
        {
            defaults[i] = new AudioCategoryVolumeSetting { Category = categories[i], Volume = 1f };
        }

        return defaults;
    }

    private static AudioCategoryVolumeSetting[] NormalizeCategoryVolumes(AudioCategoryVolumeSetting[] settings)
    {
        AudioCategoryVolumeSetting[] normalized = CreateDefaultCategoryVolumes();
        if (settings == null || settings.Length == 0) return normalized;

        for (int i = 0; i < normalized.Length; i++)
        {
            AudioVolumeCategory category = normalized[i].Category;
            if (TryGetCategoryVolume(settings, category, out float volume))
            {
                normalized[i].Volume = Mathf.Clamp01(volume);
            }
        }

        return normalized;
    }

    private static bool TryGetCategoryVolume(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category, out float volume)
    {
        if (settings != null)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i].Category == category)
                {
                    volume = settings[i].Volume;
                    return true;
                }
            }
        }

        volume = 1f;
        return false;
    }

    private static float GetCategoryVolumeFromSettings(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category)
    {
        return TryGetCategoryVolume(settings, category, out float volume) ? Mathf.Clamp01(volume) : 1f;
    }

    private static AudioCategoryVolumeSetting[] SetCategoryVolumeInternal(AudioCategoryVolumeSetting[] settings, AudioVolumeCategory category, float value)
    {
        AudioCategoryVolumeSetting[] normalized = NormalizeCategoryVolumes(settings);
        for (int i = 0; i < normalized.Length; i++)
        {
            if (normalized[i].Category == category)
            {
                normalized[i].Volume = Mathf.Clamp01(value);
                break;
            }
        }

        return normalized;
    }

    private static void NormalizePrimarySliderInteraction(Slider slider, TMP_Text valueText)
    {
        if (slider == null)
        {
            return;
        }

        if (valueText != null)
        {
            valueText.raycastTarget = false;
        }

        SetChildGraphicRaycast(slider.transform, "Background", true);
        SetChildGraphicRaycast(slider.transform, "Fill", false);
        SetChildGraphicRaycast(slider.transform, "Handle", true);

        RectTransform handleSlideArea = FindChildRect(slider.transform, "Handle Slide Area") ?? FindChildRect(slider.transform, "Sliding Area");
        if (handleSlideArea != null)
        {
            handleSlideArea.anchorMin = Vector2.zero;
            handleSlideArea.anchorMax = Vector2.one;
            handleSlideArea.anchoredPosition = Vector2.zero;
            handleSlideArea.sizeDelta = new Vector2(-20f, -20f);
        }

        RectTransform handle = FindChildRect(slider.transform, "Handle");
        if (handle != null)
        {
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(20f, 20f);
        }
    }

    private static void SetChildGraphicRaycast(Transform root, string childName, bool raycastTarget)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        Transform child = root.Find(childName);
        if (child == null)
        {
            return;
        }

        Graphic graphic = child.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }

    private static RectTransform FindChildRect(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform child = root.Find(childName);
        return child as RectTransform;
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
}
