using System;
using UnityEngine;

/// <summary>
/// 指示管理器接口：提供外部系统创建、更新、销毁指示的入口
/// </summary>
public interface IIndicatorManager
{
    int CreateIndicator(EIndicatorType type, Vector3 worldPosition, float customDuration = -1f, int customPriority = -1);
    void UpdateIndicator(int indicatorId, Vector3 newWorldPosition);
    void DestroyIndicator(int indicatorId);
    IndicatorObject GetIndicator(int indicatorId);
    void ClearAllIndicators();
}

/// <summary>
/// 指示渲染器接口：定义指示渲染的核心能力
/// </summary>
public interface IIndicatorRenderer
{
    void SetRegistry(IndicatorCentralRegistry registry);
    void RenderIndicators(System.Collections.Generic.List<IndicatorObject> activeIndicators, Camera renderCamera);
    void ClearAllVisuals();
}
