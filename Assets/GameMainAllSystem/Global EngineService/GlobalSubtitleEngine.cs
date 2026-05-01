using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;





public enum SubtitleChannel { System = 0, Dialogue = 1, Mission = 2, Ambient = 3 }




/// <summary>
/// 字幕包：包含一个字幕频道和一个字符串列表，表示一系列要显示的字幕文本。
/// 还包含当前显示的行索引和字符索引，以支持断点续显功能。
/// </summary>
public class SubtitlePackage
{
    public SubtitleChannel Channel;
    public List<string> ContentList;
    public int CurrentLineIndex = 0;   // 断点行索引
    public int CurrentCharIndex = 0;   // 断点字符索引
    public Action OnFinished;

    public bool HasContent => ContentList != null && ContentList.Count > 0;
    public bool HasFinished => !HasContent || CurrentLineIndex >= ContentList.Count;

    public SubtitlePackage(SubtitleChannel channel, List<string> contents)
    {
        Channel = channel;
        ContentList = contents;
    }

    public void ResetProgress()
    {
        CurrentLineIndex = 0;
        CurrentCharIndex = 0;
    }
}




/// <summary>
/// 字幕引擎：负责在屏幕上显示临时的字幕信息，如提示、警告等。提供静态接口，允许其他系统调用来显示字幕。
/// 字幕会在一定时间后自动消失，或者当新的字幕出现时替换旧的字幕。
/// </summary>
public class GlobalSubtitleEngine : MonoBehaviour
{
    private const string IdleText = "暂无";

    public static GlobalSubtitleEngine Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI targetLabel;
    [SerializeField] private float typingSpeed = 0.05f;

    private static SubtitlePackage _activePackage;


    //将字幕包添加到优先级池中，按照频道优先级进行排序，确保高优先级的字幕能够及时显示
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
        ShowIdleState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            _activePackage = null;
        }
    }




    /// <summary>
    /// 请求显示一个新的字幕包。如果当前有正在显示的字幕包，根据新包的频道优先级决定是立即替换还是加入优先级池等待显示。
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
            // 如果新包的频道优先级高于当前正在显示的包，则立即切换到新包；否则将新包加入优先级池等待显示
            if ((int)newPackage.Channel < (int)_activePackage.Channel)
            {
                StopCurrentAndSave();
                _priorityPool.Add(_activePackage); // 旧包带进度回池
                PlayPackage(newPackage);
            }
            else
            {
                _priorityPool.Add(newPackage); // 新包进池等待
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

    private IEnumerator SubtitleRoutine(SubtitlePackage package)
    {
        while (package.CurrentLineIndex < package.ContentList.Count)
        {
            string text = package.ContentList[package.CurrentLineIndex];

            for (int i = package.CurrentCharIndex; i <= text.Length; i++)
            {
                package.CurrentCharIndex = i; // 更新当前字符索引，支持断点续显
                targetLabel.text = text.Substring(0, i);

                yield return new WaitForSeconds(typingSpeed);
            }

            package.CurrentCharIndex = 0; // 行索引重置，准备显示下一行
            package.CurrentLineIndex++;

            yield return new WaitForSeconds(1.5f); // 行间停顿
        }

        _activePackage = null; // 当前包显示完毕，清空引用
        package.OnFinished?.Invoke(); // 触发完成回调
        TryPlayNext(); // 尝试播放优先级池中的下一个包
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
            targetLabel.text = IdleText;
        }
    }

    public void ShowIdleState()
    {
        if (targetLabel == null)
        {
            return;
        }

        targetLabel.text = IdleText;
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

    private void SortPool() => _priorityPool.Sort((a, b) => ((int)a.Channel).CompareTo((int)b.Channel)); // 按频道优先级排序，数值越小优先级越高



}