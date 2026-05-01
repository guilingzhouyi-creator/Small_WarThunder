# 《东七三》开发日志

按时间倒序记录每次提交的开发变更。

---

## 2026-05

### 2026-05-01 多地图架构重构

**新增内容：**
- `LevelRegistrySystem` — MapEntry 双层结构，支持多张地图 + 子区域注册
- `LevelStreamingEngine` — 启动时一次性 Instantiate 所有地图，自动扫描子物体注册区域，SetActive 控制可见性
- `PlayerMaker` / `EnemyMaker` — 空 Marker 组件，替代 Tag 字符串检测
- 预留 `LoadMap(mapId)` / `UnloadMap(mapId)` 接口，未来按需加载

**改动及优化描述：**
- `LevelRegistrySystem` 从单层 RegionEntry 扩展到 MapEntry → RegionEntry 双层
- `LevelStreamingEngine.LoadAllMaps()` 遍历 allMaps，Instantiate 父级预制体后扫描直接子物体
- 每个子物体自动挂载 `GameLevelMaker`（如果缺失），regionId 格式为 `"mapId_childName"`
- `GameLevelMaker.IsTank()` 从 `CompareTag` 改为 `TryGetComponent<PlayerMaker/EnemyMaker>`，不再依赖 Tag 字符串
- `ShowNearbyRegions()` 优先用 Collider.bounds 计算区域中心，regionSize 兜底
