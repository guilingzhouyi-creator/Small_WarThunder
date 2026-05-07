using UnityEngine;
using TMPro;
using System.Collections.Generic;
using SmallWar.Data;

/// <summary>
/// GlobalSubtitleEngine 的任务文本渲染模块（partial）。
/// 负责监听 TaskDistributionSystem 并将任务进度文本写入多个 TMP 组件。
/// 输出前通过 SubtitleColorRenderEngine 进行 TMP 富文本着色。
/// </summary>
partial class GlobalSubtitleEngine
{
    [Header("任务栏渲染表（可拖入多个 TMP 组件）")]
    [SerializeField] private List<TextMeshProUGUI> taskRenderLabels = new List<TextMeshProUGUI>();

    private void Start()
    {
        if (TaskDistributionSystem.Instance != null)
        {
            TaskDistributionSystem.Instance.OnTaskTextChanged += OnTaskTextReceived;
        }
    }

    /// <summary>
    /// 将任务文本写入所有任务栏 TMP 组件。由 TaskDistributionSystem.OnTaskTextChanged 驱动。
    /// 最终写入前通过 SubtitleColorRenderEngine 进行 TMP 富文本着色。
    /// </summary>
    public void SetTaskText(string text)
    {
        if (taskRenderLabels == null || taskRenderLabels.Count == 0)
        {
            return;
        }

        string colored = SubtitleColorRenderEngine.Process(text, SubtitleRenderScope.TaskText, SubtitleChannel.Mission);

        foreach (var label in taskRenderLabels)
        {
            if (label != null)
            {
                label.text = colored;
            }
        }
    }

    /// <summary>
    /// 清空所有任务栏 TMP 组件的文本。
    /// </summary>
    public void ClearTaskText()
    {
        SetTaskText(string.Empty);
    }

    /// <summary>
    /// 监听 TaskDistributionSystem.OnTaskTextChanged，将格式化后的任务文本写入所有 taskRenderLabels。
    /// </summary>
    private void OnTaskTextReceived(TaskProgress progress)
    {
        if (progress == null)
        {
            return;
        }

        SetTaskText(progress.FormattedText);
    }
}
