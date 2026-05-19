using System;
using UnityEngine;

/// <summary>
/// 全局单位跟踪系统事件参数——单位生成事件
/// </summary>
public class UnitSpawnedEventArgs : EventArgs
{
    public string uid;
    public EUnitFaction faction;
    public EUnitLifeStatus lifeStatus;
    public Vector3 worldPosition;
    public float spawnTime;

    public UnitSpawnedEventArgs(string uid, EUnitFaction faction, EUnitLifeStatus lifeStatus, Vector3 worldPosition, float spawnTime)
    {
        this.uid = uid;
        this.faction = faction;
        this.lifeStatus = lifeStatus;
        this.worldPosition = worldPosition;
        this.spawnTime = spawnTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件参数——单位销毁事件
/// </summary>
public class UnitDestroyedEventArgs : EventArgs
{
    public string uid;
    public float destroyTime;

    public UnitDestroyedEventArgs(string uid, float destroyTime)
    {
        this.uid = uid;
        this.destroyTime = destroyTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件参数——单位被侦测事件
/// </summary>
public class UnitDetectedEventArgs : EventArgs
{
    public string uid;
    public Vector3 worldPosition;
    public float detectTime;

    public UnitDetectedEventArgs(string uid, Vector3 worldPosition, float detectTime)
    {
        this.uid = uid;
        this.worldPosition = worldPosition;
        this.detectTime = detectTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件参数——单位丢失侦测事件
/// </summary>
public class UnitLostEventArgs : EventArgs
{
    public string uid;
    public Vector3 lastKnownPosition;
    public float lossTime;

    public UnitLostEventArgs(string uid, Vector3 lastKnownPosition, float lossTime)
    {
        this.uid = uid;
        this.lastKnownPosition = lastKnownPosition;
        this.lossTime = lossTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件参数——单位位置更新事件
/// </summary>
public class UnitPositionUpdatedEventArgs : EventArgs
{
    public string uid;
    public Vector3 newPosition;
    public float updateTime;

    public UnitPositionUpdatedEventArgs(string uid, Vector3 newPosition, float updateTime)
    {
        this.uid = uid;
        this.newPosition = newPosition;
        this.updateTime = updateTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件参数——单位状态变更事件
/// </summary>
public class UnitStateChangedEventArgs : EventArgs
{
    public string uid;
    public EUnitLifeStatus newLifeStatus;
    public float changeTime;

    public UnitStateChangedEventArgs(string uid, EUnitLifeStatus newLifeStatus, float changeTime)
    {
        this.uid = uid;
        this.newLifeStatus = newLifeStatus;
        this.changeTime = changeTime;
    }
}

/// <summary>
/// 全局单位跟踪系统事件广播层接口。
/// 负责向敌方高亮系统和地图系统广播单位状态变化事件。
/// 采用事件驱动加定时校准的混合方式，避免纯轮询造成的性能开销。
/// </summary>
public interface IGlobalUnitEventBus
{
    /// <summary>单位生成事件</summary>
    event EventHandler<UnitSpawnedEventArgs> onUnitSpawned;

    /// <summary>单位销毁事件</summary>
    event EventHandler<UnitDestroyedEventArgs> onUnitDestroyed;

    /// <summary>单位进入侦测事件</summary>
    event EventHandler<UnitDetectedEventArgs> onUnitDetected;

    /// <summary>单位退出侦测事件</summary>
    event EventHandler<UnitLostEventArgs> onUnitLost;

    /// <summary>单位位置更新事件</summary>
    event EventHandler<UnitPositionUpdatedEventArgs> onUnitPositionUpdated;

    /// <summary>单位状态变更事件（生命状态等）</summary>
    event EventHandler<UnitStateChangedEventArgs> onUnitStateChanged;

    /// <summary>广播单位生成事件</summary>
    void RaiseUnitSpawned(string uid, EUnitFaction faction, EUnitLifeStatus lifeStatus, Vector3 worldPosition);

    /// <summary>广播单位销毁事件</summary>
    void RaiseUnitDestroyed(string uid);

    /// <summary>广播单位被侦测事件</summary>
    void RaiseUnitDetected(string uid, Vector3 worldPosition);

    /// <summary>广播单位丢失侦测事件</summary>
    void RaiseUnitLost(string uid, Vector3 lastKnownPosition);

    /// <summary>广播单位位置更新事件</summary>
    void RaiseUnitPositionUpdated(string uid, Vector3 newPosition);

    /// <summary>广播单位状态变更事件</summary>
    void RaiseUnitStateChanged(string uid, EUnitLifeStatus newLifeStatus);
}

/// <summary>
/// 全局单位事件总线实现。
/// 单例模式，游戏场景启动时初始化，场景退出时释放。
/// </summary>
public class GlobalUnitEventBus : IGlobalUnitEventBus
{
    private static GlobalUnitEventBus _instance;
    private static readonly object _lock = new object();

    public static GlobalUnitEventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GlobalUnitEventBus();
                        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_EVENT_BUS} 实例已创建。");
                    }
                }
            }
            return _instance;
        }
    }

    public event EventHandler<UnitSpawnedEventArgs> onUnitSpawned;
    public event EventHandler<UnitDestroyedEventArgs> onUnitDestroyed;
    public event EventHandler<UnitDetectedEventArgs> onUnitDetected;
    public event EventHandler<UnitLostEventArgs> onUnitLost;
    public event EventHandler<UnitPositionUpdatedEventArgs> onUnitPositionUpdated;
    public event EventHandler<UnitStateChangedEventArgs> onUnitStateChanged;

    private GlobalUnitEventBus() { }

    /// <summary>释放所有事件订阅并销毁实例</summary>
    public void Release()
    {
        onUnitSpawned = null;
        onUnitDestroyed = null;
        onUnitDetected = null;
        onUnitLost = null;
        onUnitPositionUpdated = null;
        onUnitStateChanged = null;
        _instance = null;
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_EVENT_BUS} 实例已释放。");
    }

    public void RaiseUnitSpawned(string uid, EUnitFaction faction, EUnitLifeStatus lifeStatus, Vector3 worldPosition)
    {
        onUnitSpawned?.Invoke(this, new UnitSpawnedEventArgs(uid, faction, lifeStatus, worldPosition, Time.time));
    }

    public void RaiseUnitDestroyed(string uid)
    {
        onUnitDestroyed?.Invoke(this, new UnitDestroyedEventArgs(uid, Time.time));
    }

    public void RaiseUnitDetected(string uid, Vector3 worldPosition)
    {
        onUnitDetected?.Invoke(this, new UnitDetectedEventArgs(uid, worldPosition, Time.time));
    }

    public void RaiseUnitLost(string uid, Vector3 lastKnownPosition)
    {
        onUnitLost?.Invoke(this, new UnitLostEventArgs(uid, lastKnownPosition, Time.time));
    }

    public void RaiseUnitPositionUpdated(string uid, Vector3 newPosition)
    {
        onUnitPositionUpdated?.Invoke(this, new UnitPositionUpdatedEventArgs(uid, newPosition, Time.time));
    }

    public void RaiseUnitStateChanged(string uid, EUnitLifeStatus newLifeStatus)
    {
        onUnitStateChanged?.Invoke(this, new UnitStateChangedEventArgs(uid, newLifeStatus, Time.time));
    }
}
