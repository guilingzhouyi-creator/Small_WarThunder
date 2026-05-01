using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡区域流式加载引擎：对齐 GlobalSubtitleEngine 的单例风格。
/// 启动时一次性 Instantiate 所有地图（父级预制体），自动扫描子物体注册为区域。
/// 玩家移动时通过 GameLevelMaker 触发 ShowNearbyRegions，控制区域可见性。
///
/// 未来扩展预留接口：LoadMap(string mapId) / UnloadMap(string mapId)
///
/// 加载时扫描子物体上的 LevelMissionMarker，如果存在则激活 GameLevelManager 以触发注册和字幕。
/// </summary>
public class LevelStreamingEngine : MonoBehaviour
{
    public static LevelStreamingEngine Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private LevelRegistrySystem _registrySystem;

    [Header("所有加载地图的父节点（可选）")]
    [SerializeField] private Transform _mapsParent;

    [Header("区域可见性配置")]
    [SerializeField] private int _visibleRange = 3;      // 显示附近 N×N 块区域
    [SerializeField] private float _regionSize = 1000f;   // 单块区域边长（米），用于距离判断的兜底
    [SerializeField] private bool _autoRefreshByPlayerPosition = true;

    private Transform _playerTankTransform;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPlayerPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (_registrySystem != null)
        {
            LoadAllMaps();
            StartCoroutine(RefreshVisibleRegionsAfterBootRoutine());
        }
        else
        {
            Debug.LogError("[LevelStreamingEngine] 未设置 LevelRegistrySystem 资产");
        }
    }

    private System.Collections.IEnumerator RefreshVisibleRegionsAfterBootRoutine()
    {
        yield return null;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (TryResolvePlayerTankTransform(out _))
            {
                RefreshVisibleRegionsNow();
                yield break;
            }

            yield return null;
        }
    }

    private void Update()
    {
        if (!_autoRefreshByPlayerPosition || _registrySystem == null)
        {
            return;
        }

        if (!TryResolvePlayerTankTransform(out Transform playerTransform))
        {
            return;
        }

        Vector3 playerPosition = playerTransform.position;
        if (_hasLastPlayerPosition && (playerPosition - _lastPlayerPosition).sqrMagnitude < 0.01f)
        {
            return;
        }

        _playerTankTransform = playerTransform;
        _lastPlayerPosition = playerPosition;
        _hasLastPlayerPosition = true;

        ShowNearbyRegions(playerPosition);
    }

    /// <summary>
    /// 立即按当前玩家位置刷新一次区域可见性。
    /// </summary>
    public void RefreshVisibleRegionsNow()
    {
        if (!TryResolvePlayerTankTransform(out Transform playerTransform))
        {
            return;
        }

        Vector3 playerPosition = playerTransform.position;
        _playerTankTransform = playerTransform;
        _lastPlayerPosition = playerPosition;
        _hasLastPlayerPosition = true;

        ShowNearbyRegions(playerPosition);
    }

    // ==================== 加载 ====================

    /// <summary>
    /// 一次性加载所有配置的地图，扫描子物体注册为区域，默认全部隐藏。
    /// 如果子物体上有 LevelMissionMarker 且带有 GameLevelManager，则激活以触发注册和字幕。
    /// </summary>
    public void LoadAllMaps()
    {
        if (_registrySystem == null)
        {
            Debug.LogError("[LevelStreamingEngine] RegistrySystem 为空");
            return;
        }

        _registrySystem.Initialize();

        foreach (var mapEntry in _registrySystem.allMaps)
        {
            if (mapEntry == null)
            {
                Debug.LogWarning("[LevelStreamingEngine] 地图条目为空，跳过");
                continue;
            }

            LoadSingleMap(mapEntry);
        }
    }

    /// <summary>
    /// 加载单张地图：Instantiate 父级预制体 → 扫描子物体 → 注册区域 → 默认隐藏。
    /// 如果子物体上有 LevelMissionMarker 且 Tag 为 "MissionRegion"，则激活以触发 GameLevelManager 注册和字幕。
    /// </summary>
    private void LoadSingleMap(MapEntry mapEntry)
    {
        if (mapEntry.worldMapPrefab == null)
        {
            Debug.LogWarning($"[LevelStreamingEngine] 地图 {mapEntry.mapId} 的 worldMapPrefab 为空，跳过");
            return;
        }

        Transform parent = _mapsParent != null ? _mapsParent : transform;
        GameObject mapRoot = Instantiate(mapEntry.worldMapPrefab, mapEntry.worldPosition, Quaternion.identity, parent);
        mapEntry.mapRoot = mapRoot;

        // 初始化该地图的区域列表
        if (mapEntry.regions == null)
            mapEntry.regions = new List<RegionEntry>();
        else
            mapEntry.regions.Clear();

        // 扫描子物体（非递归，只扫描直接子物体）
        for (int i = 0; i < mapRoot.transform.childCount; i++)
        {
            Transform child = mapRoot.transform.GetChild(i);
            string regionId = $"{mapEntry.mapId}_{child.name}";

            // ─── 任务区域处理（带 LevelMissionMarker）───
            if (child.TryGetComponent<LevelMissionMarker>(out var missionMarker))
            {
                var gameLevel = child.GetComponent<GameLevelManager>();
                if (gameLevel == null)
                {
                    Debug.LogWarning(
                        $"[LevelStreamingEngine] 区域 {child.name} 有 LevelMissionMarker 但缺少 GameLevelManager，" +
                        "无法触发任务注册和字幕。请确保子区域预制体上同时挂载了 GameLevelManager 组件。");
                }
                else
                {
                    Debug.Log(
                        $"[LevelStreamingEngine] 检测到任务区域 {child.name}（关卡 {missionMarker.LevelIndex}），" +
                        "激活 GameLevelManager 以触发注册和字幕。");
                }

                // 激活子物体以触发 GameLevelManager.OnEnable → 注册 → 字幕
                child.gameObject.SetActive(true);
                continue; // 跳过区域列表注册和默认隐藏
            }

            // ─── 普通区域处理 ───
            var regionEntry = new RegionEntry
            {
                regionId = regionId,
                regionTransform = child,
                loadedRoot = child.gameObject,
            };

            mapEntry.regions.Add(regionEntry);
            _registrySystem.RegisterLoaded(regionId, child, child.gameObject);

            // 默认隐藏
            child.gameObject.SetActive(false);
        }

        Debug.Log($"[LevelStreamingEngine] 地图 {mapEntry.mapId} 加载完成，共 {mapEntry.regions.Count} 个普通区域");
    }

    // ==================== 运行时可见性 ====================

    public void ShowNearbyRegions(Vector3 playerPos)
    {
        if (_registrySystem == null) return;

        var allEntries = _registrySystem.GetAllLoaded();
        if (!TryGetNearestRegionGrid(playerPos, allEntries, out Vector2Int anchorGrid))
        {
            return;
        }

        int gridRadius = Mathf.Max(0, (_visibleRange - 1) / 2);
        foreach (var entry in allEntries)
        {
            if (entry?.loadedRoot == null) continue;

            bool isNearby = IsRegionNearby(entry, playerPos, anchorGrid, gridRadius);
            entry.loadedRoot.SetActive(isNearby);
        }
    }

    private bool TryResolvePlayerTankTransform(out Transform playerTransform)
    {
        if (_playerTankTransform != null)
        {
            playerTransform = _playerTankTransform;
            return true;
        }

        if (GameManager.Instance != null && GameManager.Instance.PlayerTank != null)
        {
            _playerTankTransform = GameManager.Instance.PlayerTank.transform;
            playerTransform = _playerTankTransform;
            return true;
        }

        playerTransform = null;
        return false;
    }

    public void ShowRegion(string regionId)
    {
        var entry = _registrySystem?.Get(regionId);
        if (entry?.loadedRoot != null)
        {
            entry.loadedRoot.SetActive(true);
        }
    }

    public void HideRegion(string regionId)
    {
        var entry = _registrySystem?.Get(regionId);
        if (entry?.loadedRoot != null)
        {
            entry.loadedRoot.SetActive(false);
        }
    }

    // ==================== 内部辅助 ====================

    private bool IsRegionNearby(RegionEntry entry, Vector3 playerPos, Vector2Int anchorGrid, int gridRadius)
    {
        if (TryGetRegionGridCoordinate(entry, out Vector2Int regionGrid))
        {
            return Mathf.Abs(regionGrid.x - anchorGrid.x) <= gridRadius
                && Mathf.Abs(regionGrid.y - anchorGrid.y) <= gridRadius;
        }

        if (!TryGetRegionWorldBounds(entry, out Bounds regionBounds))
        {
            return false;
        }

        float visibleSize = Mathf.Max(1, _visibleRange) * _regionSize;
        Bounds visibleBounds = new Bounds(
            new Vector3(playerPos.x, regionBounds.center.y, playerPos.z),
            new Vector3(visibleSize, Mathf.Max(regionBounds.size.y, 1000f), visibleSize)
        );

        return visibleBounds.Intersects(regionBounds);
    }

    private bool TryGetNearestRegionGrid(Vector3 playerPos, List<RegionEntry> allEntries, out Vector2Int nearestGrid)
    {
        nearestGrid = default;
        float nearestDistance = float.PositiveInfinity;
        bool found = false;

        for (int index = 0; index < allEntries.Count; index++)
        {
            RegionEntry entry = allEntries[index];
            if (entry?.loadedRoot == null)
            {
                continue;
            }

            if (!TryGetRegionGridCoordinate(entry, out Vector2Int regionGrid))
            {
                continue;
            }

            if (!TryGetRegionWorldBounds(entry, out Bounds regionBounds))
            {
                continue;
            }

            float distance = regionBounds.SqrDistance(playerPos);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestGrid = regionGrid;
            found = true;
        }

        return found;
    }

    private bool TryGetRegionGridCoordinate(RegionEntry entry, out Vector2Int regionGrid)
    {
        regionGrid = default;

        string regionName = entry?.regionTransform != null ? entry.regionTransform.name : string.Empty;
        if (string.IsNullOrWhiteSpace(regionName))
        {
            return false;
        }

        int separatorIndex = regionName.IndexOf('-');
        if (separatorIndex <= 0)
        {
            return false;
        }

        int xEnd = separatorIndex;
        int yStart = separatorIndex + 1;
        int yEnd = yStart;
        while (yEnd < regionName.Length && char.IsDigit(regionName[yEnd]))
        {
            yEnd++;
        }

        if (yEnd <= yStart)
        {
            return false;
        }

        if (!int.TryParse(regionName.Substring(0, xEnd), out int gridX))
        {
            return false;
        }

        if (!int.TryParse(regionName.Substring(yStart, yEnd - yStart), out int gridY))
        {
            return false;
        }

        regionGrid = new Vector2Int(gridX, gridY);
        return true;
    }

    private bool TryGetRegionWorldBounds(RegionEntry entry, out Bounds regionBounds)
    {
        regionBounds = default;

        if (entry?.regionTransform == null)
        {
            return false;
        }

        MeshFilter meshFilter = entry.regionTransform.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            regionBounds = TransformLocalBounds(meshFilter.sharedMesh.bounds, entry.regionTransform.localToWorldMatrix);
            return true;
        }

        Collider col = entry.regionTransform.GetComponent<Collider>();
        if (col != null && col.enabled && entry.regionTransform.gameObject.activeInHierarchy)
        {
            regionBounds = col.bounds;
            return true;
        }

        regionBounds = new Bounds(entry.regionTransform.position, Vector3.zero);
        return true;
    }

    private static Bounds TransformLocalBounds(Bounds localBounds, Matrix4x4 localToWorldMatrix)
    {
        Vector3 localMin = localBounds.min;
        Vector3 localMax = localBounds.max;

        Vector3[] localCorners =
        {
            new Vector3(localMin.x, localMin.y, localMin.z),
            new Vector3(localMin.x, localMin.y, localMax.z),
            new Vector3(localMin.x, localMax.y, localMin.z),
            new Vector3(localMin.x, localMax.y, localMax.z),
            new Vector3(localMax.x, localMin.y, localMin.z),
            new Vector3(localMax.x, localMin.y, localMax.z),
            new Vector3(localMax.x, localMax.y, localMin.z),
            new Vector3(localMax.x, localMax.y, localMax.z),
        };

        Vector3 worldCorner = localToWorldMatrix.MultiplyPoint3x4(localCorners[0]);
        Bounds worldBounds = new Bounds(worldCorner, Vector3.zero);

        for (int index = 1; index < localCorners.Length; index++)
        {
            worldBounds.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(localCorners[index]));
        }

        return worldBounds;
    }

    // ==================== 预留：未来按需加载 ====================

    public void LoadMap(string mapId)
    {
        var mapEntry = _registrySystem?.GetMap(mapId);
        if (mapEntry == null)
        {
            Debug.LogWarning($"[LevelStreamingEngine] 地图 {mapId} 不存在");
            return;
        }

        if (mapEntry.mapRoot != null)
        {
            Debug.Log($"[LevelStreamingEngine] 地图 {mapId} 已加载，跳过");
            return;
        }

        LoadSingleMap(mapEntry);
    }

    public void UnloadMap(string mapId)
    {
        var mapEntry = _registrySystem?.GetMap(mapId);
        if (mapEntry == null) return;

        if (mapEntry.regions != null)
        {
            foreach (var region in mapEntry.regions)
            {
                if (region != null)
                {
                    region.regionTransform = null;
                    region.loadedRoot = null;
                }
            }
        }

        if (mapEntry.mapRoot != null)
        {
            Object.Destroy(mapEntry.mapRoot);
            mapEntry.mapRoot = null;
        }

        Debug.Log($"[LevelStreamingEngine] 地图 {mapId} 已卸载");
    }
}
