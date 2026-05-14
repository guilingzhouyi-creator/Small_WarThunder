using System;

/// <summary>
/// 音频设置运行时状态。
/// </summary>
[Serializable]
public struct AudioSettingState
{
    /// <summary>音乐音量 (0~1)</summary>
    [UnityEngine.Range(0f, 1f)]
    public float MusicVolume;

    /// <summary>音效音量 (0~1)</summary>
    [UnityEngine.Range(0f, 1f)]
    public float SfxVolume;

    /// <summary>分类音量数组</summary>
    public AudioCategoryVolumeSetting[] CategoryVolumes;
}
