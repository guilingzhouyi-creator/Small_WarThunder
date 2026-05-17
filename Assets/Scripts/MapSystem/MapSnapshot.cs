using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图数据快照结构体（每 tick 采集一次）。
/// 包含地图绘制所需的所有世界状态，以及当前模式（小地图/大地图）的参数。
/// </summary>
public struct MapSnapshot
{
    /// <summary>玩家在地图快照中的世界空间位置上下文（通常是坦克根节点坐标）。</summary>
    public Vector3 PlayerContextWorldPosition;

    /// <summary>玩家在地图快照中的朝向上下文（Yaw 角度）。</summary>
    public float PlayerContextYaw;

    /// <summary>地图相机在世界空间中的中心点坐标。</summary>
    public Vector3 MapCameraWorldCenter;

    /// <summary>地图相机的正交半高度（世界单位）。</summary>
    public float MapCameraOrthoSize;

    /// <summary>小地图在 UI 上的像素尺寸。</summary>
    public Vector2 MiniMapPixelSize;

    /// <summary>大地图在 UI 上的像素尺寸。</summary>
    public Vector2 FullMapPixelSize;

    /// <summary>是否为全屏大地图模式（false 为小地图常驻模式）。</summary>
    public bool IsFullMapMode;

    /// <summary>地图上的标记列表。</summary>
    public List<MapMarkerData> MapMarkers;

    /// <summary>
    /// 获取当前模式对应的像素尺寸。
    /// </summary>
    public Vector2 CurrentPixelSize => IsFullMapMode ? FullMapPixelSize : MiniMapPixelSize;

    public static MapSnapshot Empty => new MapSnapshot
    {
        MapMarkers = new List<MapMarkerData>(),
    };
}

/// <summary>
/// 地图上单个标记的数据。
/// </summary>
public struct MapMarkerData
{
    /// <summary>标记在世界空间中的位置。</summary>
    public Vector3 MapWorldPosition;

    /// <summary>标记的显示颜色。</summary>
    public Color DisplayColor;

    /// <summary>可选显示的文字标签。</summary>
    public string DisplayLabel;

    /// <summary>标记圆的显示半径（世界单位）。</summary>
    public float DisplayRadius;
}
