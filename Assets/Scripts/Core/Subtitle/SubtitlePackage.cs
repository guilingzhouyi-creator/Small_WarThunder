using System;
using System.Collections.Generic;

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
