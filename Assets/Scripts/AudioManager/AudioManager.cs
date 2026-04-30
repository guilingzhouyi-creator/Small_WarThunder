using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using FMOD.Studio;


/// <summary>
/// 音频管理器，负责处理游戏中的所有音频相关功能，包括但不限
/// 于坦克音频配置管理、全局音量控制、音效播放等。
/// </summary>
public partial class AudioManager : MonoBehaviour
{
    private const string DefaultLoopSlot = "default";

    public static AudioManager Instance { get; private set; }

    [Header("Tank Audio Database")]
    [SerializeField] private TankAudioDatabase _tankAudioDatabase;

    [Header("Global FMOD Events")]
    [SerializeField] private EventReference _explosionSound;
    [SerializeField] private EventReference _engineSound;

    [Header("Legacy Music")]// 
    [SerializeField] private AudioSource musicSource;
    public AudioClip[] _backgroundMusic;

    private readonly Dictionary<string, EventInstance> _activeLoopInstances = new Dictionary<string, EventInstance>();
    private readonly Dictionary<string, float> _loopVolumeScales = new Dictionary<string, float>();
    private readonly Dictionary<string, AudioVolumeCategory> _loopCategories = new Dictionary<string, AudioVolumeCategory>();
    private readonly Dictionary<AudioVolumeCategory, float> _categoryVolumes = new Dictionary<AudioVolumeCategory, float>();
    private Coroutine _musicLoopCoroutine;
    private float _sfxVolume = 1f;
    private float _musicVolume = 0.4f;

    public AudioSource MusicSource => musicSource;
    public float CurrentSfxVolume => _sfxVolume;
    public float CurrentMusicVolume => musicSource != null ? musicSource.volume : _musicVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }


    private void OnEnable()
    {
        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.SettingsChanged += OnVolumeSettingsChanged;
        }
    }

    private void Start()
    {
        // 初始化时应用当前设置
        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.SettingsChanged -= OnVolumeSettingsChanged; // 确保不重复订阅
            SettingManager.Instance.SettingsChanged += OnVolumeSettingsChanged;
        }
    }


    private void OnVolumeSettingsChanged(SettingManager.AudioSettingState newState)
    {
        // 实时接收修改并应用
        SetGlobalVolume(newState.MusicVolume, 1);
        SetGlobalVolume(newState.SfxVolume, 0);
    }

    private void OnDestroy()
    {
        StopAllLoopSounds();
        StopMusicLoopCoroutine();
    }

    private static string ComposeLoopKey(GameObject emitter, string slot)
    {
        string resolvedSlot = string.IsNullOrWhiteSpace(slot) ? DefaultLoopSlot : slot;
        return $"{emitter.GetInstanceID()}::{resolvedSlot}";
    }

    private static bool TryComposeLoopKey(GameObject emitter, string slot, out string key)
    {
        key = null;

        if (emitter == null)
        {
            return false;
        }

        key = ComposeLoopKey(emitter, slot);
        return true;
    }

    private void InternalSetVolume(float volume, int channel)
    {
        if (channel == 0)
        {
            SetSfxVolume(volume);
            return;
        }

        if (channel == 1)
        {
            SetMusicVolume(volume);
        }
    }

    private void InternalPlayBgm()
    {
        PlayPlaylistLoopMusic(_backgroundMusic);
    }

    private void InternalStopAll()
    {
        StopAllLoopSounds();
        StopMusicLoopCoroutine();

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void InternalPlayOneShotSound(EventReference eventRef, Vector3 position = default, AudioVolumeCategory category = AudioVolumeCategory.Default)
    {
        if (eventRef.IsNull)
        {
            return;
        }

        Vector3 resolvedPosition = position == default ? transform.position : position;
        EventInstance instance = RuntimeManager.CreateInstance(eventRef);
        instance.setVolume(ResolveCategoryScaledSfxVolume(category, 1f));
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(resolvedPosition));
        instance.start();
        instance.release();
    }

    private void InternalPlayLoopSound(EventReference eventRef, GameObject emitter, string slot = DefaultLoopSlot, float volumeScale = 1f, AudioVolumeCategory category = AudioVolumeCategory.Default)
    {
        if (eventRef.IsNull || !TryComposeLoopKey(emitter, slot, out string key))
        {
            return;
        }

        if (_activeLoopInstances.ContainsKey(key))
        {
            InternalStopLoopSound(emitter, slot);
        }

        EventInstance instance = RuntimeManager.CreateInstance(eventRef);
        float resolvedVolumeScale = Mathf.Clamp01(volumeScale);
        instance.setVolume(ResolveCategoryScaledSfxVolume(category, resolvedVolumeScale));
        RuntimeManager.AttachInstanceToGameObject(instance, emitter);
        instance.start();
        _activeLoopInstances[key] = instance;
        _loopVolumeScales[key] = resolvedVolumeScale;
        _loopCategories[key] = category;
    }

    private void InternalSetLoopParameter(GameObject emitter, string parameterName, float value, string slot = DefaultLoopSlot)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || !TryComposeLoopKey(emitter, slot, out string key))
        {
            return;
        }

        if (_activeLoopInstances.TryGetValue(key, out EventInstance instance) && instance.isValid())
        {
            instance.setParameterByName(parameterName, value);
        }
    }

    private void InternalStopLoopSound(GameObject emitter, string slot = DefaultLoopSlot)
    {
        if (!TryComposeLoopKey(emitter, slot, out string key))
        {
            return;
        }

        if (_activeLoopInstances.TryGetValue(key, out EventInstance instance))
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            _activeLoopInstances.Remove(key);
            _loopVolumeScales.Remove(key);
            _loopCategories.Remove(key);
        }
    }

    private void InternalStopAllLoopSounds(GameObject emitter)
    {
        if (emitter == null)
        {
            return;
        }

        string prefix = $"{emitter.GetInstanceID()}::";
        List<string> removalKeys = new List<string>();

        foreach (KeyValuePair<string, EventInstance> pair in _activeLoopInstances)
        {
            if (!pair.Key.StartsWith(prefix))
            {
                continue;
            }

            if (pair.Value.isValid())
            {
                pair.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                pair.Value.release();
            }

            removalKeys.Add(pair.Key);
        }

        foreach (string key in removalKeys)
        {
            _activeLoopInstances.Remove(key);
            _loopVolumeScales.Remove(key);
            _loopCategories.Remove(key);
        }
    }

    private void InternalPlayExplosionSound() => InternalPlayOneShotSound(_explosionSound, default, AudioVolumeCategory.Impact);
    private void InternalPlayEngineSound(GameObject emitter) => InternalPlayLoopSound(_engineSound, emitter, DefaultLoopSlot, 1f, AudioVolumeCategory.Engine);

    private void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        RefreshActiveLoopVolumes();
    }

    private void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
        {
            musicSource.volume = _musicVolume;
        }
    }

    private float InternalGetAudioCategoryVolume(AudioVolumeCategory category)
    {
        return _categoryVolumes.TryGetValue(category, out float cachedVolume)
            ? Mathf.Clamp01(cachedVolume)
            : 1f;
    }

    private AudioCategoryVolumeSetting[] InternalGetAudioCategorySettings()
    {
        AudioVolumeCategory[] categories = (AudioVolumeCategory[])Enum.GetValues(typeof(AudioVolumeCategory));
        AudioCategoryVolumeSetting[] settings = new AudioCategoryVolumeSetting[categories.Length];

        for (int index = 0; index < categories.Length; index++)
        {
            AudioVolumeCategory category = categories[index];
            settings[index] = new AudioCategoryVolumeSetting
            {
                Category = category,
                Volume = InternalGetAudioCategoryVolume(category)
            };
        }

        return settings;
    }

    private void InternalSetAudioCategoryVolume(AudioVolumeCategory category, float volume)
    {
        _categoryVolumes[category] = Mathf.Clamp01(volume);
        RefreshActiveLoopVolumes();
    }

    private void InternalSetAudioCategoryVolumes(AudioCategoryVolumeSetting[] settings)
    {
        _categoryVolumes.Clear();

        if (settings != null)
        {
            for (int index = 0; index < settings.Length; index++)
            {
                AudioCategoryVolumeSetting setting = settings[index];
                _categoryVolumes[setting.Category] = Mathf.Clamp01(setting.Volume);
            }
        }

        RefreshActiveLoopVolumes();
    }

    private float ResolveCategoryScaledSfxVolume(AudioVolumeCategory category, float baseVolumeScale)
    {
        return _sfxVolume * Mathf.Clamp01(baseVolumeScale) * InternalGetAudioCategoryVolume(category);
    }

    private void RefreshActiveLoopVolumes()
    {
        foreach (KeyValuePair<string, EventInstance> pair in _activeLoopInstances)
        {
            if (!pair.Value.isValid())
            {
                continue;
            }

            float volumeScale = _loopVolumeScales.TryGetValue(pair.Key, out float cachedScale)
                ? cachedScale
                : 1f;
            AudioVolumeCategory category = _loopCategories.TryGetValue(pair.Key, out AudioVolumeCategory cachedCategory)
                ? cachedCategory
                : AudioVolumeCategory.Default;
            pair.Value.setVolume(ResolveCategoryScaledSfxVolume(category, volumeScale));
        }
    }

    private void PlayPlaylistLoopMusic(AudioClip[] clips)
    {
        if (musicSource == null || clips == null || clips.Length == 0)
        {
            return;
        }

        StopMusicLoopCoroutine();
        _musicLoopCoroutine = StartCoroutine(PlayPlaylistLoopRoutine(clips));
    }

    private IEnumerator PlayPlaylistLoopRoutine(AudioClip[] clips)
    {
        int index = 0;

        while (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[index];
            if (clip != null && musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.loop = false;
                musicSource.Play();
                yield return new WaitWhile(() => musicSource != null && musicSource.isPlaying);
            }

            index = (index + 1) % clips.Length;
        }

        _musicLoopCoroutine = null;
    }

    private void StopMusicLoopCoroutine()
    {
        if (_musicLoopCoroutine == null)
        {
            return;
        }

        StopCoroutine(_musicLoopCoroutine);
        _musicLoopCoroutine = null;
    }

    private void StopAllLoopSounds()
    {
        foreach (EventInstance instance in _activeLoopInstances.Values)
        {
            if (instance.isValid())
            {
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instance.release();
            }
        }

        _activeLoopInstances.Clear();
        _loopVolumeScales.Clear();
        _loopCategories.Clear();
    }
}
