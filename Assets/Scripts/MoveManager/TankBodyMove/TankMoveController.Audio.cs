using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    private bool _isAudioHookBound;
    private TankAudioData _engineAudioConfig;
    private EngineAudioStateType _engineAudioState = EngineAudioStateType.Off;
    private float _engineAudioStateElapsed;
    private float _lastEngineRpmNormalized;
    private float _resolvedStartupDurationSeconds = -1f;
    private float _resolvedShutdownDurationSeconds = -1f;
    private readonly HashSet<string> _activeEngineLoopSlots = new HashSet<string>();

    private void InitializeAudioHooks()
    {
        if (_isAudioHookBound)
        {
            return;
        }

        OnEngineStateChanged += HandleEngineStateChanged;
        _isAudioHookBound = true;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            TankType tankType = ResolveTankType();
            audioManager.RegisterTank(tankType, ResolveAudioEmitter());
            bool hasAudioConfig = audioManager.TryGetTankAudioConfig(tankType, out _engineAudioConfig);
            RefreshEngineStateDurations();

            Debug.Log($"[TankAudio] InitializeAudioHooks tank={name} type={tankType} emitter={ResolveAudioEmitter().name} audioConfig={(hasAudioConfig ? _engineAudioConfig.name : "null")} hierarchical={(_engineAudioConfig != null && _engineAudioConfig.UsesHierarchicalEngineAudio)}");
        }
        else
        {
            Debug.LogWarning($"[TankAudio] InitializeAudioHooks tank={name} type={ResolveTankType()} audioManager=null");
        }

        if (_engineAudioConfig != null && _engineAudioConfig.UsesHierarchicalEngineAudio)
        {
            Debug.Log($"[TankAudio] Engine init uses hierarchical audio tank={name} state={(_isEngineOn ? EngineAudioStateType.Move : EngineAudioStateType.Off)}");
            SetEngineAudioState(_isEngineOn ? EngineAudioStateType.Move : EngineAudioStateType.Off, true);
            return;
        }

        Debug.Log($"[TankAudio] Engine init fallback to legacy loop tank={name} type={ResolveTankType()} engineOn={_isEngineOn}");
        HandleEngineStateChanged(_isEngineOn);
    }

    private void CleanupAudioHooks()
    {
        if (!_isAudioHookBound)
        {
            return;
        }

        OnEngineStateChanged -= HandleEngineStateChanged;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.UnregisterTank(ResolveAudioEmitter());
        }

        _activeEngineLoopSlots.Clear();
        _isAudioHookBound = false;
    }

    private void UpdateEngineAudioStateMachine()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        EnsureEngineAudioConfig(audioManager);

        if (_engineAudioConfig == null)
        {
            return;
        }

        if (!_engineAudioConfig.UsesHierarchicalEngineAudio)
        {
            return;
        }

        _engineAudioStateElapsed += Time.fixedDeltaTime;

        if (_engineAudioState == EngineAudioStateType.Startup &&
            _engineAudioStateElapsed >= GetStartupDuration())
        {
            SetEngineAudioState(ResolveDriveEngineAudioState(), false);
        }
        else if (_engineAudioState == EngineAudioStateType.Shutdown &&
                 _engineAudioStateElapsed >= GetShutdownDuration())
        {
            SetEngineAudioState(EngineAudioStateType.Off, false);
        }
        else if (_engineAudioState != EngineAudioStateType.Startup &&
                 _engineAudioState != EngineAudioStateType.Shutdown)
        {
            SetEngineAudioState(ResolveDriveEngineAudioState(), false);
        }

        PushEngineAudioParameters(audioManager);
    }

    private void HandleEngineStateChanged(bool isEngineOn)
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning($"[TankAudio] HandleEngineStateChanged skipped, audioManager=null tank={name} engineOn={isEngineOn}");
            return;
        }

        EnsureEngineAudioConfig(audioManager);
        GameObject emitter = ResolveAudioEmitter();

        Debug.Log($"[TankAudio] HandleEngineStateChanged tank={name} type={ResolveTankType()} engineOn={isEngineOn} config={(_engineAudioConfig != null ? _engineAudioConfig.name : "null")} hierarchical={(_engineAudioConfig != null && _engineAudioConfig.UsesHierarchicalEngineAudio)}");

        if (_engineAudioConfig != null && _engineAudioConfig.UsesHierarchicalEngineAudio)
        {
            Debug.Log($"[TankAudio] Engine state machine transition tank={name} -> {(isEngineOn ? EngineAudioStateType.Startup : EngineAudioStateType.Shutdown)}");
            SetEngineAudioState(isEngineOn ? EngineAudioStateType.Startup : EngineAudioStateType.Shutdown, true);
            PushEngineAudioParameters(audioManager);
            return;
        }

        if (isEngineOn)
        {
            if (_engineAudioConfig != null && !_engineAudioConfig.EngineLoopSound.IsNull)
            {
                Debug.Log($"[TankAudio] Play legacy engine loop tank={name} event={_engineAudioConfig.EngineLoopSound.Path}");
                audioManager.PlayLoopSound(_engineAudioConfig.EngineLoopSound, emitter, "default", 1f, _engineAudioConfig.GetEngineLoopVolumeCategory());
            }
            else
            {
                Debug.Log($"[TankAudio] Play default engine sound tank={name}");
                audioManager.PlayEngineSound(emitter);
            }
        }
        else
        {
            Debug.Log($"[TankAudio] Stop engine sound tank={name}");
            audioManager.StopEngineSound(emitter);
        }
    }

    private void EnsureEngineAudioConfig(AudioManager audioManager)
    {
        if (_engineAudioConfig != null || audioManager == null)
        {
            return;
        }

        if (audioManager.TryGetTankAudioConfig(ResolveTankType(), out _engineAudioConfig))
        {
            RefreshEngineStateDurations();
        }
    }

    private GameObject ResolveAudioEmitter()
    {
        return tanker != null ? tanker : gameObject;
    }

    private TankType ResolveTankType()
    {
        if (tankMoveData != null)
        {
            return tankMoveData.TankType;
        }

        return TankType.Unknown;
    }

    private EngineAudioStateType ResolveDriveEngineAudioState()
    {
        return _isEngineOn ? EngineAudioStateType.Move : EngineAudioStateType.Off;
    }

    private void SetEngineAudioState(EngineAudioStateType nextState, bool forceRestart)
    {
        if (!forceRestart && _engineAudioState == nextState)
        {
            return;
        }

        _engineAudioState = nextState;
        _engineAudioStateElapsed = 0f;

        if (_engineAudioState == EngineAudioStateType.Off)
        {
            _lastEngineRpmNormalized = 0f;
        }

        SyncEngineAudioLayers();
    }

    private void SyncEngineAudioLayers()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        GameObject emitter = ResolveAudioEmitter();
        EngineAudioLayerDefinition[] stateLayers = _engineAudioConfig != null ? _engineAudioConfig.GetLayers(_engineAudioState) : null;
        HashSet<string> desiredSlots = new HashSet<string>();

        if (stateLayers != null)
        {
            for (int index = 0; index < stateLayers.Length; index++)
            {
                EngineAudioLayerDefinition layer = stateLayers[index];
                if (layer == null || layer.Event.IsNull)
                {
                    continue;
                }

                string slot = BuildEngineLoopSlot(_engineAudioState, layer, index);
                desiredSlots.Add(slot);

                if (_activeEngineLoopSlots.Contains(slot))
                {
                    continue;
                }

                audioManager.PlayLoopSound(layer.Event, emitter, slot, layer.Volume, _engineAudioConfig.ResolveEngineLayerVolumeCategory(layer));
            }
        }

        List<string> staleSlots = new List<string>();
        foreach (string slot in _activeEngineLoopSlots)
        {
            if (!desiredSlots.Contains(slot))
            {
                staleSlots.Add(slot);
            }
        }

        foreach (string slot in staleSlots)
        {
            audioManager.StopLoopSound(emitter, slot);
            _activeEngineLoopSlots.Remove(slot);
        }

        _activeEngineLoopSlots.Clear();
        foreach (string slot in desiredSlots)
        {
            _activeEngineLoopSlots.Add(slot);
        }
    }

    private void PushEngineAudioParameters(AudioManager audioManager)
    {
        if (_engineAudioConfig == null || _engineAudioConfig.EngineStateAudio == null || _engineAudioState == EngineAudioStateType.Off)
        {
            return;
        }

        EngineAudioLayerDefinition[] stateLayers = _engineAudioConfig.GetLayers(_engineAudioState);
        if (stateLayers == null || stateLayers.Length == 0)
        {
            return;
        }

        GameObject emitter = ResolveAudioEmitter();
        float speedNormalized = CalculateNormalizedEngineSpeed();
        float loadNormalized = CalculateNormalizedEngineLoad();
        float rpmNormalized = CalculateNormalizedEngineRpm(speedNormalized, loadNormalized);
        float loadSwitchValue = CalculateLoadSwitchValue(speedNormalized, loadNormalized);

        _lastEngineRpmNormalized = rpmNormalized;

        for (int index = 0; index < stateLayers.Length; index++)
        {
            EngineAudioLayerDefinition layer = stateLayers[index];
            if (layer == null || layer.Event.IsNull)
            {
                continue;
            }

            string slot = BuildEngineLoopSlot(_engineAudioState, layer, index);
            TankEngineStateAudioDefinition stateConfig = _engineAudioConfig.EngineStateAudio;

            if (layer.DriveRpmParameter)
            {
                audioManager.SetLoopParameter(emitter, slot, stateConfig.RpmParameter, rpmNormalized);
            }

            if (layer.DriveSpeedParameter)
            {
                audioManager.SetLoopParameter(emitter, slot, stateConfig.SpeedParameter, speedNormalized);
            }

            if (layer.DriveLoadParameter)
            {
                audioManager.SetLoopParameter(emitter, slot, stateConfig.LoadParameter, loadNormalized);
            }

            if (_engineAudioState == EngineAudioStateType.Move &&
                layer.DriveLoadParameter &&
                !string.IsNullOrWhiteSpace(stateConfig.LoadSwitchParameter))
            {
                audioManager.SetLoopParameter(emitter, slot, stateConfig.LoadSwitchParameter, loadSwitchValue);
            }

            if (layer.DriveStateParameter)
            {
                audioManager.SetLoopParameter(emitter, slot, stateConfig.StateParameter, (float)_engineAudioState);
            }
        }
    }

    private float CalculateNormalizedEngineSpeed()
    {
        return Mathf.Max(0f, _engineAudioSpeedKmh);
    }

    private float CalculateNormalizedEngineLoad()
    {
        return Mathf.Clamp01(_engineAudioLoadNormalized);
    }

    private float CalculateNormalizedEngineRpm(float speedNormalized, float loadNormalized)
    {
        TankEngineStateAudioDefinition stateConfig = _engineAudioConfig.EngineStateAudio;
        float idleRpm = Mathf.Clamp01(stateConfig.IdleRpmNormalized);
        float maxRpm = Mathf.Clamp01(Mathf.Max(idleRpm, stateConfig.MaxRpmNormalized));
        float targetRpmNormalized = Mathf.Clamp01(Mathf.InverseLerp(EngineAudioIdleRpmFloor, EngineAudioMaxRpmCeiling, Mathf.Max(EngineAudioIdleRpmFloor, _engineAudioRpm)));
        targetRpmNormalized = Mathf.Clamp(Mathf.Max(idleRpm, targetRpmNormalized), idleRpm, maxRpm);

        if (_engineAudioState == EngineAudioStateType.Startup)
        {
            float startupProgress = GetStartupDuration() > 0f
                ? Mathf.Clamp01(_engineAudioStateElapsed / GetStartupDuration())
                : 1f;
            float startupTargetRpm = Mathf.Max(idleRpm, targetRpmNormalized);
            return Mathf.Lerp(5f, startupTargetRpm * 100f, startupProgress);
        }

        if (_engineAudioState == EngineAudioStateType.Shutdown)
        {
            float shutdownProgress = GetShutdownDuration() > 0f
                ? Mathf.Clamp01(_engineAudioStateElapsed / GetShutdownDuration())
                : 1f;
            float shutdownStartRpm = _lastEngineRpmNormalized > 0f ? _lastEngineRpmNormalized : idleRpm * 100f;
            return Mathf.Lerp(shutdownStartRpm, 0f, shutdownProgress);
        }

        return Mathf.Clamp(targetRpmNormalized * 100f, idleRpm * 100f, maxRpm * 100f);
    }

    private float CalculateLoadSwitchValue(float speedKmh, float loadNormalized)
    {
        TankEngineStateAudioDefinition stateConfig = _engineAudioConfig.EngineStateAudio;
        if (stateConfig == null || string.IsNullOrWhiteSpace(stateConfig.LoadSwitchParameter))
        {
            return 0f;
        }

        bool shouldEnable = _engineAudioState == EngineAudioStateType.Move &&
            (loadNormalized >= Mathf.Clamp01(stateConfig.LoadSwitchLoadThreshold) ||
             speedKmh >= Mathf.Max(0f, stateConfig.LoadSwitchSpeedThresholdKmh));

        return shouldEnable ? stateConfig.LoadSwitchOnValue : stateConfig.LoadSwitchOffValue;
    }

    private float GetStartupDuration()
    {
        if (_resolvedStartupDurationSeconds > 0f)
        {
            return _resolvedStartupDurationSeconds;
        }

        return _engineAudioConfig != null && _engineAudioConfig.EngineStateAudio != null
            ? Mathf.Max(0f, _engineAudioConfig.EngineStateAudio.StartupDuration)
            : 0f;
    }

    private float GetShutdownDuration()
    {
        if (_resolvedShutdownDurationSeconds > 0f)
        {
            return _resolvedShutdownDurationSeconds;
        }

        return _engineAudioConfig != null && _engineAudioConfig.EngineStateAudio != null
            ? Mathf.Max(0f, _engineAudioConfig.EngineStateAudio.ShutdownDuration)
            : 0f;
    }

    private static string BuildEngineLoopSlot(EngineAudioStateType stateType, EngineAudioLayerDefinition layer, int index)
    {
        string layerId = layer != null && !string.IsNullOrWhiteSpace(layer.LayerId)
            ? layer.LayerId.Trim()
            : $"layer_{index}";

        return $"engine/{layerId.ToLowerInvariant()}";
    }

    private void RefreshEngineStateDurations()
    {
        _resolvedStartupDurationSeconds = ResolveStateEventDurationSeconds(EngineAudioStateType.Startup, _engineAudioConfig != null && _engineAudioConfig.EngineStateAudio != null
            ? _engineAudioConfig.EngineStateAudio.StartupDuration
            : 0f);
        _resolvedShutdownDurationSeconds = ResolveStateEventDurationSeconds(EngineAudioStateType.Shutdown, _engineAudioConfig != null && _engineAudioConfig.EngineStateAudio != null
            ? _engineAudioConfig.EngineStateAudio.ShutdownDuration
            : 0f);
    }

    private float ResolveStateEventDurationSeconds(EngineAudioStateType stateType, float fallbackDuration)
    {
        EventReference eventRef = ResolvePrimaryStateEvent(stateType);
        if (!eventRef.IsNull && TryGetEventDurationSeconds(eventRef, out float durationSeconds))
        {
            return durationSeconds;
        }

        return Mathf.Max(0f, fallbackDuration);
    }

    private EventReference ResolvePrimaryStateEvent(EngineAudioStateType stateType)
    {
        if (_engineAudioConfig == null)
        {
            return default;
        }

        EngineAudioLayerDefinition[] layers = _engineAudioConfig.GetLayers(stateType);
        if (layers == null)
        {
            return default;
        }

        for (int index = 0; index < layers.Length; index++)
        {
            EngineAudioLayerDefinition layer = layers[index];
            if (layer != null && !layer.Event.IsNull)
            {
                return layer.Event;
            }
        }

        return default;
    }

    private static bool TryGetEventDurationSeconds(EventReference eventRef, out float durationSeconds)
    {
        durationSeconds = 0f;

        if (eventRef.IsNull)
        {
            return false;
        }

        try
        {
            EventDescription description = RuntimeManager.GetEventDescription(eventRef);
            if (!description.isValid())
            {
                return false;
            }

            description.getLength(out int durationMs);
            if (durationMs <= 0)
            {
                return false;
            }

            durationSeconds = durationMs / 1000f;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
