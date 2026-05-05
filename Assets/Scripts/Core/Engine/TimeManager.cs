using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局时间管理器 — 集中管理 Time.timeScale
/// 
/// 职责：
/// 1. 统一暂停/恢复接口，避免各处直接操作 Time.timeScale
/// 2. 自动监听场景切换，非 GameScene 强制恢复正常时间流速
/// 3. 提供 EnsureNormalTime() 防御性 API，LoadingScene / 主菜单等场景主动保底
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    /// <summary>当前是否处于暂停状态（timeScale == 0）</summary>
    public bool IsPaused { get; private set; }

    /// <summary>当前时间缩放值（只读便捷属性）</summary>
    public float CurrentTimeScale => Time.timeScale;

    /// <summary>游戏暂停时触发</summary>
    public event Action OnPaused;

    /// <summary>游戏恢复时触发</summary>
    public event Action OnResumed;

    /// <summary>时间缩放发生任何变化时触发（参数为新值）</summary>
    public event Action<float> OnTimeScaleChanged;

    private float _prePauseTimeScale = 1f;

    #region 自动创建 & 生命周期

    /// <summary>
    /// 确保 TimeManager 在任何场景加载前就已存在，无需手动挂载。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("TimeManager");
        go.AddComponent<TimeManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TimeManager: 检测到重复实例，销毁新建实例。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region 公开 API

    /// <summary>
    /// 暂停游戏时间流速（timeScale = 0）
    /// </summary>
    public void Pause()
    {
        if (IsPaused)
        {
            Debug.LogWarning("TimeManager: 游戏已经处于暂停状态，忽略重复 Pause 调用。");
            return;
        }

        _prePauseTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        IsPaused = true;
        Time.timeScale = 0f;

        OnPaused?.Invoke();
        OnTimeScaleChanged?.Invoke(0f);

        Debug.Log("[TimeManager] 游戏已暂停 (timeScale = 0)");
    }

    /// <summary>
    /// 恢复游戏时间流速（回到暂停前的 timeScale 值）
    /// </summary>
    public void Resume()
    {
        if (!IsPaused)
        {
            Debug.LogWarning("TimeManager: 游戏当前未暂停，忽略 Resume 调用。");
            return;
        }

        IsPaused = false;
        Time.timeScale = _prePauseTimeScale;

        OnResumed?.Invoke();
        OnTimeScaleChanged?.Invoke(_prePauseTimeScale);

        Debug.Log($"[TimeManager] 游戏已恢复 (timeScale = {_prePauseTimeScale})");
    }

    /// <summary>
    /// 无条件确保时间流速为 1f（防御性 API）
    /// 用于 LoadingScene、主菜单等不需要暂停的场景主动保底。
    /// </summary>
    public void EnsureNormalTime()
    {
        if (IsPaused)
        {
            Debug.LogWarning("[TimeManager] EnsureNormalTime 调用时游戏处于暂停状态，强制恢复。");
        }

        IsPaused = false;
        Time.timeScale = 1f;
        _prePauseTimeScale = 1f;

        OnTimeScaleChanged?.Invoke(1f);

        Debug.Log("[TimeManager] 时间流速已确保为 1f");
    }

    /// <summary>
    /// 设置自定义时间缩放（不影响暂停状态）
    /// 如果是暂停状态下调用，只记录期望值，恢复后生效。
    /// </summary>
    public void SetTimeScale(float scale)
    {
        scale = Mathf.Max(0f, scale);

        if (!IsPaused)
        {
            Time.timeScale = scale;
            _prePauseTimeScale = scale;
            OnTimeScaleChanged?.Invoke(scale);
        }
        else
        {
            _prePauseTimeScale = scale;
            Debug.Log($"[TimeManager] 暂停状态下记录期望 timeScale = {scale}，恢复后生效。");
        }
    }

    #endregion

    #region 场景切换自动恢复

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            Debug.Log($"[TimeManager] 检测到非 GameScene ({scene.name})，自动恢复正常时间。");
            EnsureNormalTime();
        }
    }

    #endregion
}
