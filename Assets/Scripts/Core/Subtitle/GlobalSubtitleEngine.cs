using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TextCore;

/// <summary>
/// 字幕引擎主调度模块。
/// </summary>
public partial class GlobalSubtitleEngine : MonoBehaviour
{
    private static readonly WaitForSeconds WaitForSeconds_Two = new WaitForSeconds(2.0f);
    private static readonly WaitForSeconds WaitForSeconds_LinePause = new WaitForSeconds(1.5f);

    public static GlobalSubtitleEngine Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI targetLabel;
    [SerializeField] private float typingSpeed = 0.05f;

    private static SubtitlePackage _activePackage;
    private readonly List<SubtitlePackage> _priorityPool = new List<SubtitlePackage>();
    private Coroutine _typeRoutine;

    public bool HasActivePackage => _activePackage != null;
    public bool IsPlaying => _typeRoutine != null;
    public bool IsPaused => _activePackage != null && _typeRoutine == null;
    public SubtitlePackage CurrentPackage => _activePackage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ShowIdleState();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (TaskDistributionSystem.Instance != null)
        {
            TaskDistributionSystem.Instance.OnTaskTextChanged -= OnTaskTextReceived;
        }

        if (Instance == this)
        {
            Instance = null;
            _activePackage = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            TryBindTargetLabel();
        }
        else
        {
            targetLabel = null;
            ResetPlayback();
        }
    }

    public void RequestSubtitle(SubtitlePackage newPackage)
    {
        if (newPackage == null || !newPackage.HasContent)
        {
            return;
        }

        if (ReferenceEquals(_activePackage, newPackage))
        {
            if (_typeRoutine == null)
            {
                ResumePlayback();
            }
            return;
        }

        if (_activePackage != null)
        {
            if ((int)newPackage.Channel < (int)_activePackage.Channel)
            {
                StopCurrentAndSave();
                _priorityPool.Add(_activePackage);
                PlayPackage(newPackage);
            }
            else
            {
                _priorityPool.Add(newPackage);
                SortPool();
            }
        }
        else
        {
            PlayPackage(newPackage);
        }
    }

    public void PlayOrResume(SubtitlePackage package, bool restartIfFinished = false)
    {
        if (package == null || !package.HasContent)
        {
            return;
        }

        TryBindTargetLabel();

        if (ReferenceEquals(_activePackage, package))
        {
            if (package.HasFinished)
            {
                if (!restartIfFinished)
                {
                    return;
                }

                package.ResetProgress();
                ReplaceActivePackage(package);
                return;
            }

            if (_typeRoutine == null)
            {
                ResumePlayback();
            }
            return;
        }

        if (package.HasFinished)
        {
            if (!restartIfFinished)
            {
                return;
            }

            package.ResetProgress();
        }

        ReplaceActivePackage(package);
    }

    public void ShowIdleState()
    {
        TryBindTargetLabel();

        if (targetLabel == null)
        {
            return;
        }

        if (_activePackage != null || MissionNarrativeRuntime.HasCurrentPackage)
        {
            return;
        }

        targetLabel.text = UIStandardTexts.Idle;
        OnOverlayTextChanged?.Invoke(UIStandardTexts.Idle);
    }

    public void ResetPlayback()
    {
        TryBindTargetLabel();
        ClearIntelligenceBuffer();
        ClearPanelNarrativeBuffer();

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        _priorityPool.Clear();
        _activePackage = null;

        if (targetLabel != null)
        {
            targetLabel.text = UIStandardTexts.Idle;
            OnOverlayTextChanged?.Invoke(UIStandardTexts.Idle);
        }
    }

    public void ClearCurrentOutput()
    {
        ClearIntelligenceBuffer();
        ClearPanelNarrativeBuffer();

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        _priorityPool.Clear();
        _activePackage = null;

        if (targetLabel != null)
        {
            targetLabel.text = string.Empty;
        }

        OnOverlayTextChanged?.Invoke(string.Empty);
    }

    public void PausePlayback()
    {
        if (_typeRoutine == null)
        {
            return;
        }

        StopCoroutine(_typeRoutine);
        _typeRoutine = null;
    }

    public void ResumePlayback()
    {
        if (_activePackage == null || _typeRoutine != null)
        {
            return;
        }

        TryBindTargetLabel();
        _typeRoutine = StartCoroutine(SubtitleRoutine(_activePackage));
    }

    private void PlayPackage(SubtitlePackage package)
    {
        _activePackage = package;
        _typeRoutine = StartCoroutine(SubtitleRoutine(package));
    }

    private void ReplaceActivePackage(SubtitlePackage package)
    {
        StopActiveRoutine();
        _priorityPool.Clear();
        PlayPackage(package);
    }

    private void StopCurrentAndSave()
    {
        StopActiveRoutine();
    }

    private bool TryBindTargetLabel()
    {
        if (targetLabel != null)
        {
            return true;
        }

        var missionPanel = MissionPannelUIController.Instance;
        if (missionPanel == null)
        {
            return false;
        }

        targetLabel = missionPanel.SubtitleLabel;
        if (targetLabel == null)
        {
            Debug.LogWarning("[GlobalSubtitleEngine] MissionPannelUIController.SubtitleLabel 为 null，请在 Inspector 中为 MissionPannelUIController 的 _subtitleLabel 字段赋值。", missionPanel);
            return false;
        }

        // 任务面板右栏需要显示累计内容，使用顶部起排避免前文被垂直居中后裁掉。
        targetLabel.alignment = TextAlignmentOptions.TopLeft;
        targetLabel.verticalAlignment = VerticalAlignmentOptions.Top;
        targetLabel.horizontalAlignment = HorizontalAlignmentOptions.Left;

        return true;
    }

    private void StopActiveRoutine()
    {
        if (_typeRoutine == null)
        {
            return;
        }

        StopCoroutine(_typeRoutine);
        _typeRoutine = null;

        if (_activePackage != null && _activePackage.Channel != SubtitleChannel.Mission)
        {
            SubtitleOverlayController.Instance?.SetNarrativeActive(false);
        }
    }

    private void TryPlayNext()
    {
        if (_priorityPool.Count > 0)
        {
            SortPool();

            var nextPackage = _priorityPool[0];
            _priorityPool.RemoveAt(0);
            PlayPackage(nextPackage);
        }
    }

    private void SortPool() =>
        _priorityPool.Sort((a, b) => ((int)a.Channel).CompareTo((int)b.Channel));

    private IEnumerator SubtitleRoutine(SubtitlePackage package)
    {
        if (package.Channel == SubtitleChannel.Mission)
        {
            yield return IntelligenceRoutine(package);
            yield break;
        }

        SubtitleOverlayController.Instance?.SetNarrativeActive(true);
        SyncPanelNarrativeBufferForProgress(package);

        try
        {
            while (package.CurrentLineIndex < package.ContentList.Count)
            {
                string text = package.ContentList[package.CurrentLineIndex];
                string richCached = SubtitleColorRenderEngine.Process(text, SubtitleRenderScope.Overlay, package.Channel);
                AppendPanelNarrativeLine(text);

                if (targetLabel != null)
                {
                    targetLabel.text = GetRenderedPanelNarrativeBuffer(package.Channel);
                }

                for (int i = package.CurrentCharIndex; i <= text.Length; i++)
                {
                    package.CurrentCharIndex = i;

                    string colored = SubtitleColorRenderEngine.GetVisibleSubstring(richCached, i);

                    OnOverlayTextChanged?.Invoke(colored);

                    yield return new WaitForSeconds(typingSpeed);
                }

                package.CurrentCharIndex = 0;
                package.CurrentLineIndex++;

                yield return WaitForSeconds_LinePause;
            }
        }
        finally
        {
            SubtitleOverlayController.Instance?.SetNarrativeActive(false);
        }

        _activePackage = null;
        package.OnFinished?.Invoke();
        TryPlayNext();
    }
}
