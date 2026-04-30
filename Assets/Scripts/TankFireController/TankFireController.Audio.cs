using UnityEngine;

/// <summary>
/// 坦克火控系统 - 负责处理坦克的射击逻辑和相关音效（包括机枪和主炮）
public partial class TankFireController : MonoBehaviour
{
    private GameObject ResolveAudioEmitter()
    {
        if (_tank != null)
        {
            return _tank.gameObject;
        }

        return transform.root != null ? transform.root.gameObject : gameObject;
    }

    private Vector3 GetAudioPlaybackPosition()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return _firePoint != null ? _firePoint.position : transform.position;
    }

    private void PlayFireAudio()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            // Debug.LogWarning($"[TankAudio] PlayFireAudio skipped, audioManager=null tank={name}");
            return;
        }
        // Debug.Log($"[TankAudio] PlayFireAudio tank={name} emitter={ResolveAudioEmitter().name} cue={TankAudioCueIds.FirePrimary} pos={GetAudioPlaybackPosition()}");
        audioManager.PlayTankCue(ResolveAudioEmitter(), TankAudioCueIds.FirePrimary, GetAudioPlaybackPosition());
    }

    private void PlayReloadAudio()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            // Debug.LogWarning($"[TankAudio] PlayReloadAudio skipped, audioManager=null tank={name}");
            return;
        }

        // Debug.Log($"[TankAudio] PlayReloadAudio tank={name} emitter={ResolveAudioEmitter().name} cue={TankAudioCueIds.ReloadPrimary} pos={GetAudioPlaybackPosition()}");
        audioManager.PlayTankCue(ResolveAudioEmitter(), TankAudioCueIds.ReloadPrimary, GetAudioPlaybackPosition());
    }
}