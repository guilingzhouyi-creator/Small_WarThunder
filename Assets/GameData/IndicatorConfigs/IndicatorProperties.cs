using System;
using UnityEngine;

/// <summary>
/// 指示属性数据结构：存储每种指示类型的视觉配置
/// </summary>
[Serializable]
public struct IndicatorProperties
{
    [Header("颜色")]
    [Tooltip("指示图标主色")]
    public Color mainColor;

    [Tooltip("指示图标边缘色")]
    public Color edgeColor;

    [Header("尺寸")]
    [Tooltip("屏幕空间图标大小 (像素)")]
    public float iconSize;

    [Tooltip("箭头大小 (像素)")]
    public float arrowSize;

    [Header("图标")]
    [Tooltip("指示图标 Sprite，为空时使用默认箭头")]
    public Sprite iconSprite;

    [Header("行为")]
    [Tooltip("是否显示距离文本")]
    public bool showDistance;

    [Tooltip("默认优先级 (值越小优先级越高)")]
    public int defaultPriority;

    [Tooltip("默认持续时长 (秒)，0 表示持久")]
    public float defaultDuration;
}
