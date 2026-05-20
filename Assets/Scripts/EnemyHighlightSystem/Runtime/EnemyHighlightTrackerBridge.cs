using System;
using UnityEngine;

/// <summary>
/// 敌方高亮系统与全局单位跟踪系统之间的桥接层。
/// 订阅 GlobalUnitEventBus 事件，将其翻译为敌方高亮系统可消费的生命周期变更。
/// 不做状态管理，不做渲染，只做事件翻译和参数适配。
/// </summary>
public class EnemyHighlightTrackerBridge : IDisposable
{
    private readonly Action<string, Vector3> _onDetectedCallback;
    private readonly Action<string, Vector3> _onLostCallback;
    private readonly Action<string, EUnitLifeStatus> _onLifeStatusChangedCallback;
    private readonly Action<string, Vector3> _onPositionUpdatedCallback;
    private bool _disposed;

    /// <summary>
    /// 构建桥接层，注册回调并订阅全局事件总线。
    /// </summary>
    /// <param name="onDetected">单位被侦测回调 (uid, worldPosition)</param>
    /// <param name="onLost">单位失去侦测回调 (uid, lastKnownPosition)</param>
    /// <param name="onLifeStatusChanged">单位生命状态变更回调 (uid, newLifeStatus)</param>
    /// <param name="onPositionUpdated">单位位置更新回调 (uid, newPosition)</param>
    public EnemyHighlightTrackerBridge(
        Action<string, Vector3> onDetected,
        Action<string, Vector3> onLost,
        Action<string, EUnitLifeStatus> onLifeStatusChanged,
        Action<string, Vector3> onPositionUpdated)
    {
        _onDetectedCallback = onDetected;
        _onLostCallback = onLost;
        _onLifeStatusChangedCallback = onLifeStatusChanged;
        _onPositionUpdatedCallback = onPositionUpdated;

        GlobalUnitEventBus.Instance.onUnitDetected += HandleUnitDetected;
        GlobalUnitEventBus.Instance.onUnitLost += HandleUnitLost;
        GlobalUnitEventBus.Instance.onUnitStateChanged += HandleUnitStateChanged;
        GlobalUnitEventBus.Instance.onUnitPositionUpdated += HandleUnitPositionUpdated;

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_BRIDGE} 桥接层已初始化，已订阅 GlobalUnitEventBus。");
    }

    private void HandleUnitDetected(object sender, UnitDetectedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.uid)) return;
        _onDetectedCallback?.Invoke(e.uid, e.worldPosition);
    }

    private void HandleUnitLost(object sender, UnitLostEventArgs e)
    {
        if (string.IsNullOrEmpty(e.uid)) return;
        _onLostCallback?.Invoke(e.uid, e.lastKnownPosition);
    }

    private void HandleUnitStateChanged(object sender, UnitStateChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.uid)) return;
        _onLifeStatusChangedCallback?.Invoke(e.uid, e.newLifeStatus);
    }

    private void HandleUnitPositionUpdated(object sender, UnitPositionUpdatedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.uid)) return;
        _onPositionUpdatedCallback?.Invoke(e.uid, e.newPosition);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (GlobalUnitEventBus.Instance != null)
        {
            GlobalUnitEventBus.Instance.onUnitDetected -= HandleUnitDetected;
            GlobalUnitEventBus.Instance.onUnitLost -= HandleUnitLost;
            GlobalUnitEventBus.Instance.onUnitStateChanged -= HandleUnitStateChanged;
            GlobalUnitEventBus.Instance.onUnitPositionUpdated -= HandleUnitPositionUpdated;
        }

        Debug.Log($"{EnemyHighlightConstants.LOG_TAG_BRIDGE} 桥接层已释放。");
    }
}
