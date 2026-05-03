using System;
using UnityEngine;

/// <summary>
/// 任务面板 UIController。
/// 控制 GlobalSubtitleEngine 的字幕播放生命周期。
/// 将字幕文本桥接到 MapUIController 的 UI Toolkit 叠加层以实现跨层显示。
/// </summary>
public class MissionPannelUIController : MonoBehaviour
{
    public static MissionPannelUIController Instance { get; private set; }

    private SubtitlePackage _requestedNarrative;
    private bool _displayActive;
    private bool _textBridgeBound;

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
        UnbindTextBridge();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        MissionNarrativeRuntime.RebindMissionPanel();

        if (_displayActive)
        {
            BindTextBridge();
        }

        if (_displayActive && _requestedNarrative != null)
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
        UnbindTextBridge();

        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.PausePlayback();
        }
    }

    /// <summary>
    /// 控制字幕在 UI Toolkit 层的显示/隐藏。
    /// 由 UIManager 调用，替代原来的 GameObject.SetActive 激活方式。
    /// </summary>
    public void SetDisplayActive(bool active)
    {
        if (_displayActive == active)
        {
            return;
        }

        _displayActive = active;

        if (active)
        {
            BindTextBridge();

            if (_requestedNarrative != null)
            {
                SyncRequestedNarrative();
            }
            else if (GlobalSubtitleEngine.Instance != null
                && GlobalSubtitleEngine.Instance.HasActivePackage
                && !GlobalSubtitleEngine.Instance.IsPlaying)
            {
                GlobalSubtitleEngine.Instance.ResumePlayback();
            }
        }
        else
        {
            UnbindTextBridge();

            if (GlobalSubtitleEngine.Instance != null)
            {
                GlobalSubtitleEngine.Instance.PausePlayback();
            }
        }

        MapUIController.Instance?.SetMissionOverlayVisible(active);
    }

    private void BindTextBridge()
    {
        if (_textBridgeBound)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance == null || MapUIController.Instance == null)
        {
            return;
        }

        GlobalSubtitleEngine.Instance.OnSubtitleTextChanged += HandleSubtitleTextChanged;
        _textBridgeBound = true;
    }

    private void UnbindTextBridge()
    {
        if (!_textBridgeBound)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.OnSubtitleTextChanged -= HandleSubtitleTextChanged;
        }

        MapUIController.Instance?.SetMissionText(string.Empty);
        _textBridgeBound = false;
    }

    private void HandleSubtitleTextChanged(string text)
    {
        MapUIController.Instance?.SetMissionText(text);
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

        if (isActiveAndEnabled && _displayActive)
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

        if (!MissionNarrativeRuntime.HasCurrentPackage)
        {
            GlobalSubtitleEngine.Instance?.ShowIdleState();
        }
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
