using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌方高亮地图适配层。
/// 接收 EnemyHighlightManager 输出的标准化标记列表，
/// 将实时高亮 Marker 和最后快照 Marker 写入 MapSnapshot.Markers，
/// 供 MapRenderingEngine 同时渲染小地图和大地图。
/// </summary>
public class EnemyHighlightMapAdapter
{
    private EnemyHighlightManager _manager;
    private EnemyHighlightMapStyleSO _style;
    private EnemyHighlightMapProjectionSO _projection;
    private List<MapMarkerData> _workingBuffer = new List<MapMarkerData>(32);

    /// <summary>小地图和大地图共用的最终标记输出。</summary>
    public IReadOnlyList<MapMarkerData> currentMarkers => _workingBuffer;

    public EnemyHighlightMapAdapter(EnemyHighlightManager manager, EnemyHighlightMapStyleSO style, EnemyHighlightMapProjectionSO projection)
    {
        _manager = manager;
        _style = style;
        _projection = projection;

        if (_manager != null)
        {
            _manager.onMarkerDataUpdated += ApplyMarkerData;
        }

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_MAP_ADAPTER} 已初始化。");
    }

    public void Dispose()
    {
        if (_manager != null)
        {
            _manager.onMarkerDataUpdated -= ApplyMarkerData;
        }

        _workingBuffer.Clear();
        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_MAP_ADAPTER} 已释放。");
    }

    /// <summary>
    /// 根据当前 MapSnapshot 的模式（小地图/大地图），
    /// 从缓存中生成最终的地图标记数据。
    /// 每次地图渲染前由 MapRenderingEngine 调用。
    /// </summary>
    public void PopulateSnapshot(ref MapSnapshot snapshot)
    {
        if (_manager == null || _style == null) return;

        bool isFullMap = snapshot.IsFullMapMode;

        snapshot.MapMarkers.Clear();
        for (int i = 0; i < _workingBuffer.Count; i++)
        {
            MapMarkerData raw = _workingBuffer[i];
            MapMarkerData styled = new MapMarkerData
            {
                MapWorldPosition = raw.MapWorldPosition,
                DisplayLabel = isFullMap ? raw.DisplayLabel : null,
            };

            // 半径区分：小地图 vs 大地图
            bool isRealTime = raw.DisplayColor.a > 0.5f;
            if (isRealTime)
            {
                styled.DisplayRadius = isFullMap ? _style.bigmapLiveRadius : _style.minimapLiveRadius;
                styled.DisplayColor = new Color(raw.DisplayColor.r, raw.DisplayColor.g, raw.DisplayColor.b, raw.DisplayColor.a);
            }
            else
            {
                styled.DisplayRadius = isFullMap ? _style.bigmapLastSeenRadius : _style.minimapLastSeenRadius;
                styled.DisplayColor = new Color(raw.DisplayColor.r, raw.DisplayColor.g, raw.DisplayColor.b, raw.DisplayColor.a);
            }

            snapshot.MapMarkers.Add(styled);
        }
    }

    /// <summary>
    /// 来自 EnemyHighlightManager 的标记数据更新回调。
    /// 将最新标记列表写入内部缓冲区。
    /// </summary>
    private void ApplyMarkerData(List<MapMarkerData> markerList)
    {
        _workingBuffer.Clear();
        if (markerList != null)
        {
            _workingBuffer.AddRange(markerList);
        }
    }
}
