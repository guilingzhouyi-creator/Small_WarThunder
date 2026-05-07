using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 字幕引擎（主文件 / 核心调度模块）。
/// 采用 partial class 架构，将不同渲染职责拆分到同名 partial 文件：
///   - GlobalSubtitleEngine.TaskText.cs     → 任务文本渲染模块
///   - GlobalSubtitleEngine.Intelligence.cs → 情报栏整片推送模块
///   - GlobalSubtitleEngine.Overlay.cs      → 跨层级字幕打字机模块
/// 本文件负责：单例、优先级调度池、生命周期、公共 API 入口、打字机协程分发。
/// </summary>
public partial class GlobalSubtitleEngine : MonoBehaviour
{
    private static WaitForSeconds WaitForSeconds_Two = new WaitForSeconds(2.0f);
    private static WaitForSeconds WaitForSeconds_LinePause = new WaitForSeconds(1.5f);

    public static GlobalSubtitleEngine Instance { get; private set; }

    private TextMeshProUGUI targetLabel;

    [SerializeField] private float typingSpeed = 0.05f;

    private static SubtitlePackage _activePackage;
    private List<SubtitlePackage> _priorityPool = new List<SubtitlePackage>();
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

    // ──────────────────────── 场景加载回调 ────────────────────────

    /// <summary>
    /// 当加载到 GameScene 时，通过 Tag + 组件校验自动绑定 targetLabel；
    /// 离开 GameScene 时置空 targetLabel 并重置播放状态。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            GameObject go = GameObject.FindGameObjectWithTag("MissionUI");
            if (go != null && go.GetComponent<MissionPannelUIController>() != null)
            {
                targetLabel = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                targetLabel = null;
                Debug.LogWarning("[GlobalSubtitleEngine] 未找到 Tag=MissionUI 且含 MissionPannelUIController 的 GameObject，targetLabel 置空。");
            }
        }
        else
        {
            targetLabel = null;
            ResetPlayback();
        }
    }

    // ──────────────────────── 公开 API ────────────────────────

    /// <summary>
    /// 请求显示一个新的字幕包。根据新包的频道优先级决定是立即替换还是加入优先级池等待显示。
    /// </summary>
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

        _typeRoutine = StartCoroutine(SubtitleRoutine(_activePackage));
    }

    // ──────────────────────── 内部调度 ────────────────────────

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

    private void StopActiveRoutine()
    {
        if (_typeRoutine == null)
        {
            return;
        }

        StopCoroutine(_typeRoutine);
        _typeRoutine = null;
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

    // ──────────────────────── 核心分发协程 ────────────────────────

    /// <summary>
    /// 字幕主协程。根据 SubtitleChannel 分发到 IntelligenceRoutine（Mission）或
    /// 覆盖层打字机逻辑（System/Dialogue/Ambient）。
    /// </summary>
    private IEnumerator SubtitleRoutine(SubtitlePackage package)
    {
        // Mission 频道 → 情报栏整片推送（定义在 GlobalSubtitleEngine.Intelligence.cs）
        if (package.Channel == SubtitleChannel.Mission)
        {
            yield return IntelligenceRoutine(package);
            yield break;
        }

        // ─── 覆盖层打字机（System / Dialogue / Ambient）───

        while (package.CurrentLineIndex < package.ContentList.Count)
        {
            string text = package.ContentList[package.CurrentLineIndex];

            // ★ 缓存复用：只调用一次 Process 对整个文本着色，打字机循环中用 GetVisibleSubstring 截取
            string richCached = SubtitleColorRenderEngine.Process(text, SubtitleRenderScope.Overlay, package.Channel);

            for (int i = package.CurrentCharIndex; i <= text.Length; i++)
            {
                package.CurrentCharIndex = i;

                string colored = SubtitleColorRenderEngine.GetVisibleSubstring(richCached, i);

                if (targetLabel != null)
                {
                    targetLabel.text = colored;
                }

                OnOverlayTextChanged?.Invoke(colored);

                yield return new WaitForSeconds(typingSpeed);
            }

            package.CurrentCharIndex = 0;
            package.CurrentLineIndex++;

            yield return WaitForSeconds_LinePause;
        }

        _activePackage = null;
        package.OnFinished?.Invoke();
        TryPlayNext();
    }
}
