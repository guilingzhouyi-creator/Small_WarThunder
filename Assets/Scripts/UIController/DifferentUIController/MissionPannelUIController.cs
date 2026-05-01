using UnityEngine;

/// <summary>
/// 任务面板 UIController。
/// 接收 GameLevelManager 传来的 SubtitlePackage，转交给 GlobalSubtitleEngine 渲染。
/// OnEnable 时恢复暂停的播放，OnDisable 时暂停播放。
/// </summary>
public class MissionPannelUIController : MonoBehaviour
{
    private SubtitlePackage _pendingNarrative;

    private void OnEnable()
    {
        if (GlobalSubtitleEngine.Instance != null
            && GlobalSubtitleEngine.Instance.HasActivePackage
            && !GlobalSubtitleEngine.Instance.IsPlaying)
        {
            GlobalSubtitleEngine.Instance.ResumePlayback();
            return;
        }

        if (_pendingNarrative != null)
        {
            TryPlayPendingNarrative();
        }
    }

    private void OnDisable()
    {
        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.PausePlayback();
        }
    }

    /// <summary>
    /// 接收 GameLevelManager 传来的 SubtitlePackage，播放或缓存等待 UI 激活后播放。
    /// </summary>
    public void PlayNarrative(SubtitlePackage package)
    {
        if (package == null)
        {
            return;
        }

        _pendingNarrative = package;

        if (isActiveAndEnabled)
        {
            TryPlayPendingNarrative();
        }
    }

    private void TryPlayPendingNarrative()
    {
        if (_pendingNarrative == null || GlobalSubtitleEngine.Instance == null)
        {
            return;
        }

        GlobalSubtitleEngine.Instance.ResetPlayback();
        GlobalSubtitleEngine.Instance.RequestSubtitle(_pendingNarrative);
        _pendingNarrative = null;
    }
}
