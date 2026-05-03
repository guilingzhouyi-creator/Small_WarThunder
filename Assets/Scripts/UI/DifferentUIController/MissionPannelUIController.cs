using System;
using UnityEngine;

/// <summary>
/// 任务面板 UIController。
/// 控制 GlobalSubtitleEngine 的字幕播放生命周期。
/// 将字幕文本桥接到独立的 SubtitleOverlayController UI Toolkit 叠加层。
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
        // 仅解绑事件转发，不清除文本、不暂停引擎。
        // 叙事包应独立于 Tab 面板继续运行，显示/隐藏由 SubtitleOverlayController.ApplyVisibility 门禁决定。
        if (_textBridgeBound && GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.OnSubtitleTextChanged -= HandleSubtitleTextChanged;
            _textBridgeBound = false;
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
            // Tab 关闭：仅解绑事件转发，不清除文本、不暂停引擎。
            // 叙事包独立运行，显示由 ApplyVisibility 门禁控制。
            if (_textBridgeBound && GlobalSubtitleEngine.Instance != null)
            {
                GlobalSubtitleEngine.Instance.OnSubtitleTextChanged -= HandleSubtitleTextChanged;
                _textBridgeBound = false;
            }
        }

        SubtitleOverlayController.Instance?.SetNarrativeActive(active);
    }

    private void BindTextBridge()
    {
        if (_textBridgeBound)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance == null || SubtitleOverlayController.Instance == null)
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

        // 不清除字幕文本：SubtitleOverlayController 有自己的直接绑定，
        // 清空文本会导致 Tab 关闭后字幕消失。
        _textBridgeBound = false;
    }

    private void HandleSubtitleTextChanged(string text)
    {
        SubtitleOverlayController.Instance?.SetText(text);
    }

    /// <summary>
    /// 接收 GameLevelManager 传来的 SubtitlePackage，立即播放。
    /// 不再依赖 isActiveAndEnabled / _displayActive 状态 —— Tab 面板 inactive 时叙事包仍可驱动字幕。
    /// 字幕的最终显示/隐藏由 SubtitleOverlayController 的门禁引擎（ApplyVisibility）决定。
    /// </summary>
    public void PresentNarrative(SubtitlePackage package)
    {
        if (package == null)
        {
            return;
        }

        _requestedNarrative = package;
        _displayActive = true;

        // BindTextBridge 是幂等的，GameObject inactive 时也能安全绑定事件
        BindTextBridge();
        SubtitleOverlayController.Instance?.SetNarrativeActive(true);

        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.PlayOrResume(package);
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
