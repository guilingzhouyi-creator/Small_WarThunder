using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局单位注册表运行时管理器。
/// 封装注册表的增删改查，提供只读查询接口给业务系统。
/// 注册表 SO 作为唯一真源数据持有，本管理器负责运行时操作和事件中转。
/// </summary>
public class GlobalUnitRegistryManager
{
    private GlobalUnitRegistrySO _registrySO;
    private readonly Dictionary<string, GlobalUnitEntry> _entryIndex;

    /// <summary>获取当前注册单位数</summary>
    public int Count => _entryIndex.Count;

    /// <summary>获取注册表最大容量</summary>
    public int MaxCapacity => _registrySO != null ? _registrySO.maxRegistryCapacity : 0;

    public GlobalUnitRegistryManager(GlobalUnitRegistrySO registrySO)
    {
        _registrySO = registrySO;
        _entryIndex = new Dictionary<string, GlobalUnitEntry>(registrySO.maxRegistryCapacity);

        // 初始化预注册单位
        foreach (var entry in registrySO.preRegisteredUnits)
        {
            if (!_entryIndex.ContainsKey(entry.uid))
            {
                _entryIndex[entry.uid] = entry;
            }
        }
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 注册表初始化完成，预加载 {_entryIndex.Count} 个单位。");
    }

    // —— 注册与注销 ——

    /// <summary>注册新单位到真源</summary>
    public bool RegisterUnit(string uid, EUnitFaction faction, string unitType, Vector3 position, float yaw)
    {
        if (_entryIndex.Count >= _registrySO.maxRegistryCapacity)
        {
            Debug.LogWarning($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 注册表容量已满，拒绝注册 UID={uid}。");
            return false;
        }
        if (_entryIndex.ContainsKey(uid))
        {
            Debug.LogWarning($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} UID={uid} 已存在，跳过重复注册。");
            return false;
        }
        _entryIndex[uid] = GlobalUnitEntry.CreateDefault(uid, faction, unitType, position, yaw);
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 单位已注册 UID={uid} 类型={unitType} 阵营={faction}。");
        return true;
    }

    /// <summary>注销单位并从真源移除</summary>
    public bool UnregisterUnit(string uid)
    {
        if (_entryIndex.Remove(uid))
        {
            Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 单位已注销 UID={uid}。");
            return true;
        }
        Debug.LogWarning($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 单位注销失败，UID={uid} 不存在。");
        return false;
    }

    // —— 更新方法 ——

    /// <summary>更新单位位置</summary>
    public bool UpdatePosition(string uid, Vector3 worldPosition)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.worldPosition = worldPosition;
        _entryIndex[uid] = entry;
        return true;
    }

    /// <summary>更新单位朝向</summary>
    public bool UpdateYaw(string uid, float yaw)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.yaw = yaw;
        _entryIndex[uid] = entry;
        return true;
    }

    /// <summary>更新生命状态</summary>
    public bool UpdateLifeStatus(string uid, EUnitLifeStatus lifeStatus)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.lifeStatus = lifeStatus;
        _entryIndex[uid] = entry;
        return true;
    }

    /// <summary>设置侦测状态</summary>
    public bool SetDetected(string uid, bool isDetected, float timestamp)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.isDetected = isDetected;
        entry.lastDetectedTime = timestamp;
        _entryIndex[uid] = entry;
        return true;
    }

    /// <summary>设置高亮状态</summary>
    public bool SetHighlighted(string uid, bool isHighlighted)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.isHighlighted = isHighlighted;
        _entryIndex[uid] = entry;
        return true;
    }

    /// <summary>更新跟踪状态</summary>
    public bool SetTrackStatus(string uid, EUnitTrackStatus trackStatus)
    {
        if (!_entryIndex.TryGetValue(uid, out var entry)) return false;
        entry.trackStatus = trackStatus;
        _entryIndex[uid] = entry;
        return true;
    }

    // —— 只读查询 ——

    /// <summary>按 UID 查询单位</summary>
    public bool TryGetUnit(string uid, out GlobalUnitEntry entry)
    {
        return _entryIndex.TryGetValue(uid, out entry);
    }

    /// <summary>获取所有已注册单位</summary>
    public IEnumerable<GlobalUnitEntry> GetAllUnits()
    {
        return _entryIndex.Values;
    }

    /// <summary>按阵营筛选</summary>
    public List<GlobalUnitEntry> GetUnitsByFaction(EUnitFaction faction)
    {
        var result = new List<GlobalUnitEntry>();
        foreach (var kv in _entryIndex)
        {
            if (kv.Value.faction == faction)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>按可见性（侦测中）筛选</summary>
    public List<GlobalUnitEntry> GetDetectedUnits()
    {
        var result = new List<GlobalUnitEntry>();
        foreach (var kv in _entryIndex)
        {
            if (kv.Value.isDetected)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>按最近侦测时间筛选（大于等于 minTimestamp）</summary>
    public List<GlobalUnitEntry> GetUnitsByLastDetectedTime(float minTimestamp)
    {
        var result = new List<GlobalUnitEntry>();
        foreach (var kv in _entryIndex)
        {
            if (kv.Value.lastDetectedTime >= minTimestamp)
                result.Add(kv.Value);
        }
        return result;
    }

    /// <summary>检查 UID 是否存在</summary>
    public bool Contains(string uid) => _entryIndex.ContainsKey(uid);

    /// <summary>清空注册表（用于场景退出）</summary>
    public void Clear()
    {
        _entryIndex.Clear();
        Debug.Log($"{GlobalUnitTrackingConstants.LOG_TAG_REGISTRY} 注册表已清空。");
    }
}
