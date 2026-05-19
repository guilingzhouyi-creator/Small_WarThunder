using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// CG 片段数据（ScriptableObject）。
/// 配置 CG 视频资源和播放参数。
/// </summary>
[CreateAssetMenu(fileName = "CgClip", menuName = "SmallWarThunder/核心/CG/片段")]
public class CgClip : ScriptableObject
{
    [Header("CG 资源")]
    [Tooltip("CG 视频文件")]
    public VideoClip videoClip;

    [Tooltip("CG 音频轨道（可选，不配置则静音）")]
    public AudioClip audioClip;

    [Header("播放参数")]
    [Tooltip("是否允许玩家跳过")]
    public bool skippable = true;

    [Tooltip("淡入时长（秒）")]
    [Range(0f, 3f)]
    public float fadeInDuration = 0.5f;

    [Tooltip("淡出时长（秒）")]
    [Range(0f, 3f)]
    public float fadeOutDuration = 0.5f;
}
