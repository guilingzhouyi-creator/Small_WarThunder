using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;



/// <summary>
/// 音频核心处理代码
/// </summary>
public partial class AudioManager : MonoBehaviour
{
    private readonly Dictionary<int, TankAudioData> _boundConfigs = new Dictionary<int, TankAudioData>();

    private bool CoreTryGetConfig(TankType type, out TankAudioData config)
    {
        config = _tankAudioDatabase != null ? _tankAudioDatabase.GetConfig(type) : null;
        return config != null;
    }

    private void CoreRegisterVehicle(TankType type, GameObject emitter)
    {
        if (emitter == null || !CoreTryGetConfig(type, out TankAudioData config))
        {
            return;
        }

        int id = emitter.GetInstanceID();
        _boundConfigs[id] = config;

    }

    private void CoreSetParam(GameObject emitter, string name, float value)
    {
        InternalSetLoopParameter(emitter, name, value);
    }

    private void CoreSetParam(GameObject emitter, string loopSlot, string name, float value)
    {
        InternalSetLoopParameter(emitter, name, value, loopSlot);
    }

    private void CorePlayCue(GameObject emitter, string cueId)
    {
        if (emitter == null)
        {
            return;
        }

        CorePlayCue(emitter, cueId, emitter.transform.position);
    }

    private void CorePlayCue(GameObject emitter, string cueId, Vector3 position)
    {
        if (!CoreTryResolveCue(emitter, cueId, out TankAudioData config, out TankAudioCueDefinition cue))
        {
            return;
        }

        EventInstance instance = RuntimeManager.CreateInstance(cue.Event);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.setVolume(ResolveCategoryScaledSfxVolume(config.ResolveCueVolumeCategory(cue), config.ResolveCueVolumeScale(cue)));
        ApplyCueParameters(instance, cue.Parameters);
        instance.start();
        instance.release();
    }

    private void CoreUnregister(GameObject emitter)
    {
        if (emitter == null)
        {
            return;
        }

        int id = emitter.GetInstanceID();
        InternalStopAllLoopSounds(emitter);
        _boundConfigs.Remove(id);
    }

    private bool CoreTryResolveCue(GameObject emitter, string cueId, out TankAudioData config, out TankAudioCueDefinition cue)
    {
        config = null;
        cue = null;

        if (emitter == null || string.IsNullOrWhiteSpace(cueId))
        {
            return false;
        }

        if (!_boundConfigs.TryGetValue(emitter.GetInstanceID(), out config) || config == null)
        {
            return false;
        }

        return config.TryGetCue(cueId, out cue) && cue != null && !cue.Event.IsNull;
    }

    private static void ApplyCueParameters(EventInstance instance, TankAudioCueParameterDefinition[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return;
        }

        for (int index = 0; index < parameters.Length; index++)
        {
            TankAudioCueParameterDefinition parameter = parameters[index];
            if (parameter == null || string.IsNullOrWhiteSpace(parameter.Name))
            {
                continue;
            }

            instance.setParameterByName(parameter.Name, parameter.Value);
        }
    }
}
