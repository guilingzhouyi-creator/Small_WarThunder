using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡区域注册表：管理多个地图及其子区域。
/// 资产中配置 MapEntry（地图预制体 + 世界位置），
/// LevelStreamingEngine 运行时 Instantiate 地图并扫描子物体自动注册为 RegionEntry。
/// </summary>
[CreateAssetMenu(fileName = "LevelRegistrySystem", menuName = "SmallWarThunder/核心/注册表/关卡注册表")]
public class LevelRegistrySystem : ScriptableObject
{
    [Header("地图列表（在 Inspector 中配置）")]
    public List<MapEntry> allMaps = new List<MapEntry>();

    /// <summary>
    /// 扁平查找表：regionId → RegionEntry，跨所有地图
    /// </summary>
    private Dictionary<string, RegionEntry> _lookup;

    // ==================== 初始化 ====================

    public void Initialize()
    {
        _lookup = new Dictionary<string, RegionEntry>();

        foreach (var map in allMaps)
        {
            if (map == null || map.regions == null) continue;

            foreach (var region in map.regions)
            {
                if (region != null && !_lookup.ContainsKey(region.regionId))
                {
                    _lookup.Add(region.regionId, region);
                }
            }
        }
    }

    // ==================== 查询 ====================

    public RegionEntry Get(string regionId)
    {
        if (_lookup == null) Initialize();
        return _lookup.TryGetValue(regionId, out var entry) ? entry : null;
    }

    public bool IsLoaded(string regionId)
    {
        var entry = Get(regionId);
        return entry != null && entry.loadedRoot != null && entry.loadedRoot.activeInHierarchy;
    }

    /// <summary>
    /// 获取所有已加载区域的副本列表（用于遍历）
    /// </summary>
    public List<RegionEntry> GetAllLoaded()
    {
        if (_lookup == null) Initialize();

        var result = new List<RegionEntry>();
        foreach (var kvp in _lookup)
        {
            if (kvp.Value.loadedRoot != null)
            {
                result.Add(kvp.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// 根据 mapId 获取地图条目
    /// </summary>
    public MapEntry GetMap(string mapId)
    {
        foreach (var map in allMaps)
        {
            if (map != null && map.mapId == mapId)
                return map;
        }
        return null;
    }

    // ==================== 注册 ====================

    /// <summary>
    /// 注册已加载到场景中的区域物体
    /// </summary>
    public void RegisterLoaded(string regionId, Transform regionTransform, GameObject root)
    {
        if (_lookup == null) Initialize();

        if (_lookup.TryGetValue(regionId, out var entry))
        {
            entry.regionTransform = regionTransform;
            entry.loadedRoot = root;
        }
        else
        {
            var newEntry = new RegionEntry
            {
                regionId = regionId,
                regionTransform = regionTransform,
                loadedRoot = root,
            };
            _lookup.Add(regionId, newEntry);
        }
    }

    // ==================== 清理 ====================

    public void ClearAll()
    {
        if (_lookup != null)
        {
            _lookup.Clear();
        }

        foreach (var map in allMaps)
        {
            if (map != null)
            {
                map.regions?.Clear();
                map.mapRoot = null;
            }
        }
    }
}

// ==================== 数据类 ====================

/// <summary>
/// 单张地图的配置条目。
/// 在 LevelRegistrySystem 资产中手动配置 mapId、worldMapPrefab、worldPosition。
/// regions 和 mapRoot 由 LevelStreamingEngine 运行时填充。
/// </summary>
[System.Serializable]
public class MapEntry
{
    [Header("地图标识")]
    public string mapId;

    [Header("地图预制体（父级，包含所有子区域）")]
    public GameObject worldMapPrefab;

    [Header("地图在世界坐标中的位置")]
    public Vector3 worldPosition;

    // ========== 运行时填充 ==========

    /// <summary>该地图下所有子区域（LevelStreamingEngine 扫描子物体自动填充）</summary>
    [System.NonSerialized]
    public List<RegionEntry> regions;

    /// <summary>Instantiate 后的地图根 GameObject</summary>
    [System.NonSerialized]
    public GameObject mapRoot;
}

/// <summary>
/// 单个区域的运行时条目。
/// regionId 格式为 "mapId_childName"，由 LevelStreamingEngine 自动生成。
/// </summary>
[System.Serializable]
public class RegionEntry
{
    [Header("区域标识（自动生成，格式: mapId_childName）")]
    public string regionId;

    /// <summary>指向子物体的 Transform（运行时设置）</summary>
    [System.NonSerialized]
    public Transform regionTransform;

    /// <summary>子物体 GameObject 引用（等于 regionTransform.gameObject）</summary>
    [System.NonSerialized]
    public GameObject loadedRoot;
}
