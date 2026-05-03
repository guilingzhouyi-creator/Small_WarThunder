using FMODUnity;
using UnityEngine;

public partial class AudioManager : MonoBehaviour
{
    public void RegisterTank(TankType type, GameObject emitter) => CoreRegisterVehicle(type, emitter);
    public bool TryGetTankAudioConfig(TankType type, out TankAudioData config) => CoreTryGetConfig(type, out config);
    public void SetTankPhysics(GameObject emitter, string paramName, float value) => CoreSetParam(emitter, paramName, value);
    public void SetTankPhysics(GameObject emitter, string loopSlot, string paramName, float value) => CoreSetParam(emitter, loopSlot, paramName, value);
    public void PlayTankCue(GameObject emitter, string cueId) => CorePlayCue(emitter, cueId);
    public void PlayTankCue(GameObject emitter, string cueId, Vector3 position) => CorePlayCue(emitter, cueId, position);
    public void UnregisterTank(GameObject emitter) => CoreUnregister(emitter);

    // public void SetVolume(float volume, int channel) => InternalSetVolume(volume, channel);
    public void SetGlobalVolume(float volume, int channel) => InternalSetVolume(volume, channel);
    public float GetCategoryVolume(AudioVolumeCategory category) => InternalGetAudioCategoryVolume(category);
    public AudioCategoryVolumeSetting[] GetCategoryVolumeSettings() => InternalGetAudioCategorySettings();
    public void SetCategoryVolume(AudioVolumeCategory category, float volume) => InternalSetAudioCategoryVolume(category, volume);
    public void SetCategoryVolumes(AudioCategoryVolumeSetting[] settings) => InternalSetAudioCategoryVolumes(settings);

    public void PlayExplosionSound() => InternalPlayExplosionSound();
    public void PlayEngineSound(GameObject emitter) => InternalPlayEngineSound(emitter);
    public void StopEngineSound(GameObject emitter) => InternalStopLoopSound(emitter);

    public void PlayOneShotSound(EventReference eventRef, Vector3 position = default) => InternalPlayOneShotSound(eventRef, position);
    public void PlayOneShotSound(EventReference eventRef, Vector3 position, AudioVolumeCategory category) => InternalPlayOneShotSound(eventRef, position, category);
    public void PlayLoopSound(EventReference eventRef, GameObject emitter) => InternalPlayLoopSound(eventRef, emitter);
    public void PlayLoopSound(EventReference eventRef, GameObject emitter, string loopSlot, float volumeScale = 1f) => InternalPlayLoopSound(eventRef, emitter, loopSlot, volumeScale);
    public void PlayLoopSound(EventReference eventRef, GameObject emitter, string loopSlot, float volumeScale, AudioVolumeCategory category) => InternalPlayLoopSound(eventRef, emitter, loopSlot, volumeScale, category);
    public void SetLoopParameter(GameObject emitter, string parameterName, float value) => InternalSetLoopParameter(emitter, parameterName, value);
    public void SetLoopParameter(GameObject emitter, string loopSlot, string parameterName, float value) => InternalSetLoopParameter(emitter, parameterName, value, loopSlot);
    public void StopLoopSound(GameObject emitter) => InternalStopLoopSound(emitter);
    public void StopLoopSound(GameObject emitter, string loopSlot) => InternalStopLoopSound(emitter, loopSlot);
    public void StopAllLoopSounds(GameObject emitter) => InternalStopAllLoopSounds(emitter);

    public void PlayGameMusic() => InternalPlayBgm();
    public void PlayBGM() => InternalPlayBgm();
    public void StopAllSounds() => InternalStopAll();
}
