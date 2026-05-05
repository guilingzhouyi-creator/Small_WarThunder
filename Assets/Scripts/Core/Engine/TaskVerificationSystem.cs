using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

/// <summary>
/// 任务校验系统 — 私钥层。
/// 持有所用单位的 UID 签名与完整任务数据的映射，
/// 在单位销毁时校验对应任务进度，按可配置间隔定时扫描完成状态。
/// </summary>
public class TaskVerificationSystem : MonoBehaviour
{
    public static TaskVerificationSystem Instance { get; private set; }

    [SerializeField] private float verificationInterval = 0.5f;

    // 私钥映射：单位 UID → 关联的 MissionKey 集合
    private readonly Dictionary<string, HashSet<MissionKey>> _unitMissionMap =
        new Dictionary<string, HashSet<MissionKey>>();

    // 所有活跃任务进度（私钥持有完整 TaskDefinition + 当前进度）
    private readonly Dictionary<MissionKey, TaskProgress> _allTasks =
        new Dictionary<MissionKey, TaskProgress>();

    private Coroutine _verificationRoutine;

    /// <summary>
    /// 任务进度更新事件 → TaskDistributionSystem 监听以格式化文本
    /// </summary>
    public event Action<MissionKey, int> OnTaskProgressUpdated;

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
        _verificationRoutine = StartCoroutine(PeriodicVerification());
    }

    private void OnDestroy()
    {
        if (_verificationRoutine != null)
        {
            StopCoroutine(_verificationRoutine);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 注册一个单位到任务中（敌方单位创建时调用）。
    /// </summary>
    public void RegisterUnitToTask(string unitUID, MissionKey missionKey, TaskDefinition definition)
    {
        if (string.IsNullOrEmpty(unitUID))
        {
            return;
        }

        // 存入私钥映射
        if (!_unitMissionMap.ContainsKey(unitUID))
        {
            _unitMissionMap[unitUID] = new HashSet<MissionKey>();
        }

        _unitMissionMap[unitUID].Add(missionKey);

        // 确保任务进度存在
        if (!_allTasks.ContainsKey(missionKey))
        {
            _allTasks[missionKey] = new TaskProgress(definition);
        }
    }

    /// <summary>
    /// 单位销毁时调用 — 校验该 UID 关联的所有任务进度。
    /// </summary>
    public void OnUnitDestroyed(string unitUID)
    {
        if (string.IsNullOrEmpty(unitUID) || !_unitMissionMap.TryGetValue(unitUID, out var missionKeys))
        {
            return;
        }

        foreach (var key in missionKeys)
        {
            if (_allTasks.TryGetValue(key, out var progress))
            {
                if (progress.IsCompleted)
                {
                    continue;
                }

                progress.CurrentCount++;

                if (progress.CurrentCount >= progress.Definition.requiredCount)
                {
                    progress.IsCompleted = true;
                }

                OnTaskProgressUpdated?.Invoke(key, progress.CurrentCount);
            }
        }

        // 清理已销毁单位的映射
        _unitMissionMap.Remove(unitUID);
    }

    /// <summary>
    /// 定时扫描未完成任务的进度，确保完成状态及时同步。
    /// </summary>
    private IEnumerator PeriodicVerification()
    {
        while (true)
        {
            yield return new WaitForSeconds(verificationInterval);

            foreach (var kvp in _allTasks)
            {
                var progress = kvp.Value;
                if (!progress.IsCompleted && progress.CurrentCount >= progress.Definition.requiredCount)
                {
                    progress.IsCompleted = true;
                    OnTaskProgressUpdated?.Invoke(kvp.Key, progress.CurrentCount);
                }
            }
        }
    }

    /// <summary>
    /// 公钥接口：获取某个任务的当前进度（供 TaskDistributionSystem 查询）。
    /// </summary>
    public TaskProgress GetTaskProgress(MissionKey key)
    {
        _allTasks.TryGetValue(key, out var progress);
        return progress;
    }

    /// <summary>
    /// 清除所有注册的任务数据。
    /// </summary>
    public void ClearAllTasks()
    {
        _unitMissionMap.Clear();
        _allTasks.Clear();
    }
}
