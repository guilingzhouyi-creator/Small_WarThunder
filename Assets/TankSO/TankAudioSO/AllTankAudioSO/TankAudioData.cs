using System;
using FMODUnity;
using UnityEngine;

public static class TankAudioCueIds
{
    public const string FirePrimary = "fire.primary";
    public const string ReloadPrimary = "reload.primary";
    public const string HitGeneric = "hit.generic";
}

public enum EngineAudioStateType
{
    Off = 0,
    Startup = 1,
    Idle = 2,
    Move = 3,
    Shutdown = 4
}

[System.Serializable]
public class EngineAudioLayerDefinition
{
    public string LayerId = "main";
    public EventReference Event;
    [Range(0f, 1f)] public float Volume = 1f;
    public AudioVolumeCategory Category = AudioVolumeCategory.Engine;
    public bool DriveRpmParameter = true;
    public bool DriveSpeedParameter = true;
    public bool DriveLoadParameter = true;
    public bool DriveStateParameter = false;
}

[Serializable]
public class TankEngineStateAudioDefinition
{
    [Header("Hierarchical Engine State Machine")]
    public bool UseHierarchicalEngineStates = false;

    [Header("FMOD Parameter Names")]
    public string RpmParameter = "rpm";
    public string SpeedParameter = "speed";
    public string LoadParameter = "load";
    public string LoadSwitchParameter = "loadswitch";
    public string StateParameter = "engine_state";

    [Header("FMOD Parameter Tuning")]
    [Range(-1f, 1f)] public float LoadSwitchOffValue = 0f;
    [Range(-1f, 1f)] public float LoadSwitchOnValue = 1f;
    [Min(0f)] public float LoadSwitchSpeedThresholdKmh = 6f;
    [Range(0f, 1f)] public float LoadSwitchLoadThreshold = 0.12f;

    [Header("Transition Durations")]
    [Min(0f)] public float StartupDuration = 1.25f;
    [Min(0f)] public float ShutdownDuration = 1f;

    [Header("RPM Estimation")]
    [Range(0f, 1f)] public float IdleRpmNormalized = 0.22f;
    [Range(0f, 1f)] public float MaxRpmNormalized = 1f;

    [Header("State Layers")]
    public EngineAudioLayerDefinition[] StartupLayers;
    public EngineAudioLayerDefinition[] IdleLayers;
    public EngineAudioLayerDefinition[] MoveLayers;
    public EngineAudioLayerDefinition[] ShutdownLayers;
}

[System.Serializable]
public class TankAudioCueParameterDefinition
{
    public string Name;
    public float Value;
}

[System.Serializable]
public class TankAudioCueDefinition
{
    public string CueId;
    public EventReference Event;
    public AudioVolumeCategory Category = AudioVolumeCategory.Default;
    [Range(0f, 1f)] public float VolumeScale = 1f;
    public TankAudioCueParameterDefinition[] Parameters;
}

[CreateAssetMenu(fileName = "TankAudioData", menuName = "TankAudioSystem/TankAudioData")]
public class TankAudioData : ScriptableObject
{
    [Header("Tank Identity")]
    public TankType TankType;

    [Header("Legacy Engine Loop")]
    public EventReference EngineLoopSound;
    public AudioVolumeCategory EngineLoopCategory = AudioVolumeCategory.Engine;

    [Header("Hierarchical Engine Audio")]
    public TankEngineStateAudioDefinition EngineStateAudio = new TankEngineStateAudioDefinition();

    [Header("One-Shot Cues")]
    public TankAudioCueDefinition[] OneShotCues;

    public bool UsesHierarchicalEngineAudio =>
        EngineStateAudio != null &&
        (EngineStateAudio.UseHierarchicalEngineStates || HasConfiguredStateLayers(EngineStateAudio));

    public EngineAudioLayerDefinition[] GetLayers(EngineAudioStateType stateType)
    {
        if (EngineStateAudio == null)
        {
            return null;
        }

        return stateType switch
        {
            EngineAudioStateType.Startup => EngineStateAudio.StartupLayers,
            EngineAudioStateType.Idle => EngineStateAudio.IdleLayers,
            EngineAudioStateType.Move => EngineStateAudio.MoveLayers,
            EngineAudioStateType.Shutdown => EngineStateAudio.ShutdownLayers,
            _ => null
        };
    }

    public AudioVolumeCategory GetEngineLoopVolumeCategory()
    {
        return ResolveDefaultCategory(EngineLoopCategory, AudioVolumeCategory.Engine);
    }

    public AudioVolumeCategory ResolveEngineLayerVolumeCategory(EngineAudioLayerDefinition layer)
    {
        return layer == null
            ? AudioVolumeCategory.Engine
            : ResolveDefaultCategory(layer.Category, AudioVolumeCategory.Engine);
    }

    public bool TryGetCue(string cueId, out TankAudioCueDefinition cue)
    {
        cue = null;

        if (string.IsNullOrWhiteSpace(cueId) || OneShotCues == null || OneShotCues.Length == 0)
        {
            return false;
        }

        for (int index = 0; index < OneShotCues.Length; index++)
        {
            TankAudioCueDefinition candidate = OneShotCues[index];
            if (candidate == null || candidate.Event.IsNull)
            {
                continue;
            }

            if (!CueIdsMatch(candidate.CueId, cueId))
            {
                continue;
            }

            cue = candidate;
            return true;
        }

        return false;
    }

    public AudioVolumeCategory ResolveCueVolumeCategory(TankAudioCueDefinition cue)
    {
        if (cue == null)
        {
            return AudioVolumeCategory.Default;
        }

        return ResolveDefaultCategory(cue.Category, ResolveCueFallbackCategory(cue.CueId));
    }

    public float ResolveCueVolumeScale(TankAudioCueDefinition cue)
    {
        return cue != null ? Mathf.Clamp01(cue.VolumeScale) : 1f;
    }

    private static bool HasConfiguredStateLayers(TankEngineStateAudioDefinition stateAudio)
    {
        if (stateAudio == null)
        {
            return false;
        }

        return HasConfiguredLayer(stateAudio.StartupLayers) ||
               HasConfiguredLayer(stateAudio.IdleLayers) ||
               HasConfiguredLayer(stateAudio.MoveLayers) ||
               HasConfiguredLayer(stateAudio.ShutdownLayers);
    }

    private static bool HasConfiguredLayer(EngineAudioLayerDefinition[] layers)
    {
        if (layers == null || layers.Length == 0)
        {
            return false;
        }

        foreach (EngineAudioLayerDefinition layer in layers)
        {
            if (layer != null && !layer.Event.IsNull)
            {
                return true;
            }
        }

        return false;
    }

    private static AudioVolumeCategory ResolveDefaultCategory(AudioVolumeCategory configuredCategory, AudioVolumeCategory fallbackCategory)
    {
        return configuredCategory == AudioVolumeCategory.Default
            ? fallbackCategory
            : configuredCategory;
    }

    private static bool CueIdsMatch(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static AudioVolumeCategory ResolveCueFallbackCategory(string cueId)
    {
        if (CueIdsMatch(cueId, TankAudioCueIds.FirePrimary))
        {
            return AudioVolumeCategory.Weapon;
        }

        if (CueIdsMatch(cueId, TankAudioCueIds.ReloadPrimary))
        {
            return AudioVolumeCategory.Reload;
        }

        if (CueIdsMatch(cueId, TankAudioCueIds.HitGeneric))
        {
            return AudioVolumeCategory.Impact;
        }

        return AudioVolumeCategory.Default;
    }
}
