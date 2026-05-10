using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using NNewUIFramework;

/// <summary>
/// CG 播放系统（执行层）。
/// 负责视频渲染、锁定输入、与 UIManager 协调状态。
/// 单例，挂载在 DontDestroyOnLoad 物体上。
/// </summary>
public class CgPlaybackSystem : MonoBehaviour
{
    public static CgPlaybackSystem Instance { get; private set; }

    [Header("渲染组件")]
    [SerializeField] private RawImage _renderTarget;
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private AudioSource _audioSource;

    [Header("UI 提示")]
    [SerializeField] private GameObject _skipPrompt;

    private CgClip _currentClip;
    private Action _onFinished;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CgPlaybackSystem] 已存在一个实例，当前实例将被销毁。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_videoPlayer == null) _videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (_audioSource == null) _audioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SetupVideoPlayerEvents();
    }

    private void OnEnable()
    {
        SetupVideoPlayerEvents();

        if (_currentClip != null && _videoPlayer != null && _isPlaying)
        {
            _videoPlayer.loopPointReached += HandleVideoFinished;
        }
    }

    private void OnDisable()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }

    private void Update()
    {
        if (!_isPlaying || _currentClip == null)
        {
            return;
        }

        // 检测跳过输入
        if (_currentClip.skippable && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            SkipCg();
        }
    }

    private void SetupVideoPlayerEvents()
    {
        if (_videoPlayer == null) return;

        _videoPlayer.loopPointReached -= HandleVideoFinished;
        _videoPlayer.loopPointReached += HandleVideoFinished;
    }

    /// <summary>
    /// 播放指定的 CG 片段。
    /// </summary>
    /// <param name="clip">CG 数据资产</param>
    /// <param name="onFinished">播完回调</param>
    public void PlayCg(CgClip clip, Action onFinished)
    {
        if (clip == null)
        {
            Debug.LogError("[CgPlaybackSystem] PlayCg 调用时 clip 为 null。", this);
            onFinished?.Invoke();
            return;
        }

        if (clip.videoClip == null)
        {
            Debug.LogError($"[CgPlaybackSystem] CgClip '{clip.name}' 未配置 videoClip。", this);
            onFinished?.Invoke();
            return;
        }

        if (_videoPlayer == null)
        {
            Debug.LogError("[CgPlaybackSystem] VideoPlayer 组件未找到。", this);
            onFinished?.Invoke();
            return;
        }

        _currentClip = clip;
        _onFinished = onFinished;

        NewUIManager.instance?.SetCgPlaying(true);

        // 配置 VideoPlayer
        _videoPlayer.clip = clip.videoClip;
        _videoPlayer.isLooping = false;

        if (clip.audioClip != null && _audioSource != null)
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.SetTargetAudioSource(0, _audioSource);
        }
        else
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        // 显示渲染目标和跳过提示
        if (_renderTarget != null)
        {
            _renderTarget.gameObject.SetActive(true);
        }

        if (_skipPrompt != null)
        {
            _skipPrompt.SetActive(clip.skippable);
        }

        _isPlaying = true;
        _videoPlayer.Play();
    }

    /// <summary>
    /// 跳过当前 CG。
    /// </summary>
    public void SkipCg()
    {
        if (!_isPlaying || _currentClip == null) return;

        _videoPlayer.Stop();
        HandleVideoFinished(_videoPlayer);
    }

    private void HandleVideoFinished(VideoPlayer vp)
    {
        if (!_isPlaying) return;

        _isPlaying = false;

        if (_renderTarget != null)
        {
            _renderTarget.gameObject.SetActive(false);
        }

        if (_skipPrompt != null)
        {
            _skipPrompt.SetActive(false);
        }

        NewUIManager.instance?.SetCgPlaying(false);

        CgClip finishedClip = _currentClip;
        Action callback = _onFinished;

        _currentClip = null;
        _onFinished = null;

        callback?.Invoke();
    }
}
