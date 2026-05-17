using System;
using TMPro;
using UnityEngine;
using NNewUIFramework;

/// <summary>
/// 任务面板 UIController.
/// 控制 GlobalSubtitleEngine 的字幕播放生命周期.
/// 覆盖层字幕文本由 SubtitleOverlayController 直接监听 GlobalSubtitleEngine.OnOverlayTextChanged，
/// 无需本类桥接；本类仅负责叙事包管理和 SubtitleOverlayController 的叙事活跃状态控制。
/// </summary>
public class MissionPannelUIController : UGUIViewAdapter
{
    public override EUIIdentity identity => EUIIdentity.MissionPanel;

    private static MissionPannelUIController _instance;

    public static MissionPannelUIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MissionPannelUIController>(FindObjectsInactive.Include);
            }

            return _instance;
        }
    }

    /// <summary>GlobalSubtitleEngine 的渲染目标，拖入面板内的 TMP 字幕标签</summary>
    [SerializeField] private TextMeshProUGUI _subtitleLabel;

    /// <summary>供 GlobalSubtitleEngine 绑定 targetLabel 使用</summary>
    public TextMeshProUGUI SubtitleLabel => _subtitleLabel;

    private SubtitlePackage _requestedNarrative;
    private bool _displayActive;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    protected override void OnOpened(object data)
    {
        SetDisplayActive(true);
    }

    protected override void OnClosing()
    {
        SetDisplayActive(false);
    }

    private void OnEnable()
    {
        MissionNarrativeRuntime.RebindMissionPanel();

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
        // 覆盖层字幕文本由 SubtitleOverlayController 直接绑定 OnOverlayTextChanged，
        // 本类无需解绑任何事件。显示/隐藏由 SubtitleOverlayController.ApplyVisibility 门禁决定。
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

        SubtitleOverlayController.Instance?.SetNarrativeActive(active);
    }

    /// <summary>
    /// 接收 GameLevelManager 传来的 SubtitlePackage，立即播放。
    /// 不再依赖 isActiveAndEnabled / _displayActive 状态 —— Tab 面板 inactive 时叙事包仍可驱动字幕。
    /// 字幕的最终显示/隐藏由 SubtitleOverlayController 的门禁引擎（ApplyVisibility）决定。
    /// 覆盖层字幕文本由 SubtitleOverlayController 直接监听 GlobalSubtitleEngine.OnOverlayTextChanged 获取。
    /// </summary>
    public void PresentNarrative(SubtitlePackage package)
    {
        if (package == null)
        {
            return;
        }

        if (ReferenceEquals(_requestedNarrative, package)
            && GlobalSubtitleEngine.Instance != null
            && ReferenceEquals(GlobalSubtitleEngine.Instance.CurrentPackage, package))
        {
            return;
        }

        _requestedNarrative = package;
        _displayActive = true;

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
