using System;
using System.Collections;

/// <summary>
/// GlobalSubtitleEngine 的情报栏渲染模块（partial）。
/// 负责 Mission 频道整片文本一次性推送到 targetLabel 和 OnIntelligenceTextChanged 事件。
/// </summary>
partial class GlobalSubtitleEngine
{
    /// <summary>
    /// 情报栏文本更新事件 — 由 Mission 频道驱动，整片文本一次性推送
    /// </summary>
    public event Action<string> OnIntelligenceTextChanged;

    /// <summary>
    /// 清空情报栏当前输出。
    /// 仅影响 Mission 频道显示，不清理面板叙事缓冲。
    /// </summary>
    private void ClearIntelligenceBuffer()
    {
        if (targetLabel != null)
        {
            targetLabel.text = string.Empty;
        }

        OnIntelligenceTextChanged?.Invoke(string.Empty);
    }

    /// <summary>
    /// 情报渲染协程：Mission 频道专用，整片文本一次性推送到 targetLabel 和 OnIntelligenceTextChanged。
    /// 输出前通过 SubtitleColorRenderEngine 进行 TMP 富文本着色。
    /// </summary>
    private IEnumerator IntelligenceRoutine(SubtitlePackage package)
    {
        SyncPanelNarrativeBufferForProgress(package);

        while (package.CurrentLineIndex < package.ContentList.Count)
        {
            string fullText = package.ContentList[package.CurrentLineIndex];
            AppendPanelNarrativeLine(fullText);
            string colored = GetRenderedPanelNarrativeBuffer(package.Channel);

            if (targetLabel != null)
            {
                targetLabel.text = colored;
            }

            OnIntelligenceTextChanged?.Invoke(colored);

            package.CurrentLineIndex++;

            yield return WaitForSeconds_Two;

            // 2.0s 行间停顿供阅读
        }

        _activePackage = null;
        package.OnFinished?.Invoke();
        TryPlayNext();
    }
}
