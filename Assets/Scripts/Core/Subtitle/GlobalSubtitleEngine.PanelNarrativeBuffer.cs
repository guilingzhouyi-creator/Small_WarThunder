using System.Text;
using UnityEngine;

partial class GlobalSubtitleEngine
{
    private readonly StringBuilder _panelNarrativeBuffer = new StringBuilder(256);

    private void ClearPanelNarrativeBuffer()
    {
        _panelNarrativeBuffer.Clear();
    }

    private void SyncPanelNarrativeBufferForProgress(SubtitlePackage package)
    {
        ClearPanelNarrativeBuffer();

        if (package?.ContentList == null || package.CurrentLineIndex <= 0)
        {
            return;
        }

        int completedLineCount = Mathf.Min(package.CurrentLineIndex, package.ContentList.Count);
        for (int i = 0; i < completedLineCount; i++)
        {
            AppendPanelNarrativeLine(package.ContentList[i]);
        }
    }

    private void AppendPanelNarrativeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (_panelNarrativeBuffer.Length > 0)
        {
            _panelNarrativeBuffer.Append('\n');
        }

        _panelNarrativeBuffer.Append(line);
    }

    private string GetRenderedPanelNarrativeBuffer(SubtitleChannel channel)
    {
        return SubtitleColorRenderEngine.Process(
            _panelNarrativeBuffer.ToString(),
            SubtitleRenderScope.Intelligence,
            channel);
    }
}
