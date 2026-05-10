using NNewUIFramework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// ���ù����������������Ϸ�еĸ�������ѡ�����������ͼ�����������Ʒ�ʽ�ȡ��ṩ�ӿڹ�����ϵͳ���ã��Ի�ȡ���޸ĵ�ǰ������״̬��
/// </summary>
public partial class SettingManager : UGUIViewAdapter
{
    public override EUIIdentity identity => EUIIdentity.SettingsPanel;

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

    // [SerializeField] private TMP_Dropdown resolutionDropdown;//���÷ֱ��������˵������������ʾ��ѡ����õ���Ļ�ֱ���ѡ��
    // [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [SerializeField] private TMP_Text sfxVolumeValueText;
    [SerializeField] private RectTransform categoryVolumeContent;
    [SerializeField] private GameObject categoryVolumeItemPrefab;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button applyButton;

    // private Resolution[] _availableResolutions;// �洢���õ���Ļ�ֱ����б������ֱ�������ѡ��ʹ��
    [SerializeField] private AudioSettingState _currentAudioSettings;
    [SerializeField] private AudioSettingState _appliedAudioSettings;
    private readonly List<AudioCategoryVolumeItem> _categoryVolumeItems = new List<AudioCategoryVolumeItem>();
    private AudioManager _audioManager;
    private bool _isInitialized;

    // private int _applyQualityIndex;
    // private int _applyResolutionIndex;

    protected override void Awake()
    {
        base.Awake();

        //����Ѿ�����ʵ���Ҳ��ǵ�ǰ���������ٵ�ǰ���󣻷�������ʵ�������Ϊ������
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
    /// ����������ڳ�ʼ�����ù�����������һ�� AudioManager ʵ����Ϊ�������Ա������ý����е�������ʱ�ܹ�ֱ�ӵ��� AudioManager �Ľӿ���Ӧ�����á�ͨ���������������ȷ������Ϸ��ʼʱ��������Ҫ���³�ʼ�����ù�����ʱ���ܹ���ȷ�����úͰ���ص�������¼������������Ҽ��ص�ǰ������״̬�Թ�������ʾ�͵�����
    /// ���磬����Ϸ��ʼʱ��GameManager ���Ե��� SettingManager.Instance.Initialize(audioManager) �����뵱ǰ�� AudioManager ʵ��������ȷ�����ù������ܹ���ȷ��Ӧ�ú�ͬ���������á�����������ý����������������ʱ������ֱ�ӵ��� AudioManager �� SetVolume ������Ӧ���µ��������ã�������Ҫ��ÿ�ε���ʱ�����ж���Ĳ��Һͻ�ȡ AudioManager ʵ���Ĳ�����
    /// </summary>
    public void Initialize(AudioManager audioManager)
    {
        _audioManager = audioManager != null ? audioManager : AudioManager.Instance;
        EnsureInitialized();
        ApplyCurrentSettingsToAudio();
    }



    /// <summary>
    /// ����������ڰ����ý����� UI ������¼������������绬������ֵ�ı��¼��Ͱ�ť�ĵ���¼���ͨ���������������ȷ������������ý������ѡ��ʱ���ܹ���ȷ�ظ����ڲ�������״̬�������ڵ��Ӧ�û�ȡ����ťʱ�ܹ�ִ����Ӧ�Ĳ�����
    /// ���磬����ҵ�����������������ʱ�����Ը��� _applyMusicVolume ������ֵ�����Ҹ��½�������ʾ�������ٷֱ��ı�������ҵ��Ӧ�ð�ťʱ�����Ե��� ApplySettings ��������ǰ������Ӧ�õ���Ϸ�У������浽 PlayerPrefs������ҵ��ȡ����ťʱ�����Ե��� CancelSettings �����ָ����ϴ�Ӧ�õ�����״̬����ˢ�½�����ʾ��
    /// </summary>
    private void BindUIListeners()
    {
        // Debug.Log("��SettingManager�����ڰ󶨻����¼�...");
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            // Debug.Log("�Ѱ��������������¼�");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            // Debug.Log("�Ѱ�SFX���������¼�");
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
            Debug.LogError("SettingManager: categoryVolumeContent δ���ã����� Inspector �з���������� Content �ڵ㡣", this);
        }

        if (categoryVolumeItemPrefab == null)
        {
            Debug.LogError("SettingManager: categoryVolumeItemPrefab δ���ã����� Inspector �з������������Ԥ���塣", this);
        }
    }
    /// <summary>
    /// ˢ�����ý���� UI ��ʾ����������ֵ�������������������ǩ��
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
        // Debug.Log($"�������϶�: {value}");
        _currentAudioSettings.MusicVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();

        // �������޸���ʵʱӦ�õ���Ƶ��������
        if (_audioManager != null)
        {
            _audioManager.SetGlobalVolume(_currentAudioSettings.MusicVolume, 1);
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        // Debug.Log($"�������϶�: {value}");
        _currentAudioSettings.SfxVolume = Mathf.Clamp01(value);
        UpdateVolumeLabels();
        NotifySettingsChanged();


        // �������޸���ʵʱӦ�õ���Ƶ��������
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


        if (NewUIManager.instance != null)
        {
            NewUIManager.instance.ShowPauseUI();
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
        if (NewUIManager.instance != null)
        {
            NewUIManager.instance.ShowSettingsUI();
        }
    }

    public void HideSettingsPanel()
    {
        if (NewUIManager.instance != null)
        {
            NewUIManager.instance.CloseSettingsUI();
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
