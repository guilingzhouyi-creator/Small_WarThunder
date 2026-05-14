using System;
using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

/// <summary>
/// 任务分发系统 — 公钥层。
/// 仅通过 MissionKey（公钥）标识和分发任务，
/// 不持有私钥数据（单位 UID 映射），不参与校验逻辑。
/// 监听 TaskVerificationSystem.OnTaskProgressUpdated → 格式化文本 → 驱动 GlobalSubtitleEngine。
/// </summary>
public class TaskDistributionSystem : MonoBehaviour
{
    public static TaskDistributionSystem Instance { get; private set; }

    // 当前分发的任务进度（公钥层仅持有 MissionKey → TaskProgress 的映射）
    private readonly Dictionary<MissionKey, TaskProgress> _distributedTasks =
        new Dictionary<MissionKey, TaskProgress>();

    /// <summary>
    /// 任务文本更新事件 → GlobalSubtitleEngine 监听
    /// </summary>
    public event Action<TaskProgress> OnTaskTextChanged;

    private bool _verificationEventsBound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BindVerificationEvents();
    }

    private void OnDestroy()
    {
        UnbindVerificationEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 向 UI 推送一个新任务（公钥层发布，调用方负责在 TaskVerificationSystem 注册单位）。
    /// </summary>
    public void PublishTask(TaskDefinition definition)
    {
        if (_distributedTasks.ContainsKey(definition.key))
        {
            return;
        }

        var progress = new TaskProgress(definition);
        _distributedTasks[definition.key] = progress;
        OnTaskTextChanged?.Invoke(progress);
    }

    /// <summary>
    /// 从 TaskVerificationSystem 同步进度到分发层，格式化并推送文本更新。
    /// </summary>
    private void OnTaskProgressUpdated(MissionKey key, int currentCount)
    {
        if (!_distributedTasks.TryGetValue(key, out var progress))
        {
            return;
        }

        progress.CurrentCount = currentCount;

        // 从校验系统拉取完整进度状态（包括 IsCompleted）
        var verified = TaskVerificationSystem.Instance?.GetTaskProgress(key);
        if (verified != null)
        {
            progress.IsCompleted = verified.IsCompleted;
        }

        OnTaskTextChanged?.Invoke(progress);
    }

    /// <summary>
    /// 清除所有分发任务。
    /// </summary>
    public void ClearAllTasks()
    {
        _distributedTasks.Clear();
    }

    private void BindVerificationEvents()
    {
        if (_verificationEventsBound || TaskVerificationSystem.Instance == null)
        {
            return;
        }

        TaskVerificationSystem.Instance.OnTaskProgressUpdated += OnTaskProgressUpdated;
        _verificationEventsBound = true;
    }

    private void UnbindVerificationEvents()
    {
        if (!_verificationEventsBound || TaskVerificationSystem.Instance == null)
        {
            return;
        }

        TaskVerificationSystem.Instance.OnTaskProgressUpdated -= OnTaskProgressUpdated;
        _verificationEventsBound = false;
    }
}
