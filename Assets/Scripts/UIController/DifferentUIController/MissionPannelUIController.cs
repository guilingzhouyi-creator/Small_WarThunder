using UnityEngine;

/// <summary>
/// 任务面板 UIController。
/// 接收 GameLevelManager 传来的 SubtitlePackage，转交给 GlobalSubtitleEngine 渲染。
/// OnEnable 时恢复暂停的播放，OnDisable 时暂停播放。
/// </summary>
public class MissionPannelUIController : MonoBehaviour
{
    public static MissionPannelUIController Instance { get; private set; }

    private SubtitlePackage _requestedNarrative;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        MissionNarrativeRuntime.RebindMissionPanel();

        if (_requestedNarrative != null)
        {
            SyncRequestedNarrative();
            return;
        }

        if (GlobalSubtitleEngine.Instance != null
            && GlobalSubtitleEngine.Instance.HasActivePackage
            && !GlobalSubtitleEngine.Instance.IsPlaying)
        {
            GlobalSubtitleEngine.Instance.ResumePlayback();
            return;
        }

        GlobalSubtitleEngine.Instance?.ShowIdleState();
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
    public void PresentNarrative(SubtitlePackage package)
    {
        if (package == null)
        {
            return;
        }

        _requestedNarrative = package;

        if (isActiveAndEnabled)
        {
            SyncRequestedNarrative();
        }
    }

    public void PlayNarrative(SubtitlePackage package)
    {
        PresentNarrative(package);
    }

    public void ClearNarrative()
    {
        _requestedNarrative = null;
        GlobalSubtitleEngine.Instance?.ShowIdleState();
    }

    private void SyncRequestedNarrative()
    {
        if (_requestedNarrative == null || GlobalSubtitleEngine.Instance == null)
        {
            return;
        }

        GlobalSubtitleEngine.Instance.PlayOrResume(_requestedNarrative);
    }
}
