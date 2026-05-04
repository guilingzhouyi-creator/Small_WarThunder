# 《东七三》开发日志 (Internal Dev Log)

> ⚠️ 此文件为内部开发日志，记录每日提交的具体实现细节。外部贡献者无需关注本文件，请参阅 [CHANGELOG.md](./CHANGELOG.md) 了解版本级别的更新内容。

按时间倒序记录每次提交的开发变更。

## 项目架构说明

本项目整体采用"场景内运行时组件 + 常驻单例服务 + 数据驱动配置"的分层式架构，围绕多场景运行稳定性、玩法协同一致性与配置复用效率进行组织。

- 场景内运行时组件负责具体玩法执行，包括玩家生成、相机绑定、任务触发、CG 播放、UI 刷新与战斗交互等职责。
- 常驻单例服务负责跨场景基础能力支撑，包括全局管理、音频、设置、字幕以及部分运行时会话状态维护。
- 配置层使用 ScriptableObject 承载可复用数据资源，例如地图、CG 与任务相关配置，以减少硬编码依赖并提升可维护性。
- 多地图系统采用 MapEntry → RegionEntry 的双层注册结构，由 LevelRegistrySystem 与 LevelStreamingEngine 协同完成流式加载、区域扫描与可见性管理。
- 任务叙事采用事件与回调驱动的运行时会话模式，将 CG、字幕、任务面板和输入锁定串联为统一流程，保证播放顺序与状态同步。

---

<!-- DEVLOG_ENTRIES_START -->

## 2026-05

### 2026-05-02 CG 播放系统 + 字幕重播间隔修复

**新增内容：**
- `CgSystem/CgClip.cs` — ScriptableObject 数据资产，配置 VideoClip/AudioClip/可跳过/淡入淡出
- `CgSystem/CgPlaybackSystem.cs` — 单例 CG 播放器，DontDestroyOnLoad，支持全屏播放 + 任意键跳过 + 完成回调

**改动及优化：**
- `GameLevelManager.cs`：
  - 新增 `TriggerPolicy` 枚举（OnceOnly / Repeatable），拆分字幕和 CG 的触发策略
  - 新增 `_cgClip` / `_cgPolicy` 字段，支持 CG → 字幕的顺序播放管线
  - CG 播完后通过回调链触发字幕，不阻塞后续逻辑
  - **修复字幕重播间隔**：包播完（HasFinished）后才重置 `_lastDispatchTime`，确保 `_repeatInterval` 从播完时刻开始等待
- `UIManager.cs`：新增 `SetCgPlaying(bool)` 和 `IsGameplayControlLocked` 包含 CG 状态，CG 播放时锁定输入并隐藏 HUD
- `MissionPannelUIController.cs` / `GlobalSubtitleEngine.cs`：配合 CG 播放暂停/恢复字幕渲染

**设计细节：**
- CgPlaybackSystem 预制体放 MainMenuScene，常驻 Hierarchy，播放时显示 RawImage，播完后隐藏
- CG 固定 OnceOnly（一次性），字幕策略可选 Repeatable / OnceOnly
- 退出区域时 CG 标记不重置，字幕根据策略决定是否重置

---

### 2026-05-01 教程关卡敌人脚本确定

**新增内容：**

- `PatrolTarget.cs` — 教程关卡巡逻靶标车移动脚本，沿固定路径点循环移动，带 Editor Gizmos 可视化

**审查结论（敌人坦克组件清单）：**

| 组件 | 用途 | 固定靶 | 巡逻靶 |
|------|------|--------|--------|
| `EnemyMaker` | 空标记组件，替代 Tag | ✓ | ✓ |
| `GeneralHitTopLevel` | 命中顶层接收器 | ✓ | ✓ |
| `TargetDamageResolver` | 伤害计算（扣血/摧毁） | ✓ | ✓ |
| `GeneralHitPosition` (子物体) | 各部位命中检测 + Collider | ✓ | ✓ |
| `PatrolTarget` (新建) | 沿路径点移动 | ✗ | ✓ |

**关键发现：**
- 敌人坦克**不能**挂 `Tank`（玩家单例，会冲突）
- 敌人坦克**不需要**挂 `TankMoveController` / `TankFireController` / `TankWeaponController` 等玩家专用脚本
- 命中链路（CannonBall→GeneralHitPosition→GeneralHitTopLevel→TargetDamageResolver）已存在，敌人只需挂这组组件即可正常受击扣血

---

### 2026-05-01 修复场景切换后对象池丢失

**问题：** 从 GameScene 退出到 MainMenu 再进入 GameScene 时，`TankAmmoPoolGroup` 和 `TerrainObjectPoolGroup` 随旧场景被销毁，对象池（弹药/地形）丢失。

**根因：** `ObjectPool` 层级下的两个容器节点均未做跨场景保护：
- `TankAmmoPoolGroup` — 未调用 `DontDestroyOnLoad`
- `TerrainObjectPoolGroup` — 纯空节点，无任何脚本保护

场景卸载时两者及其子物体一并销毁。

**修复：**
1. `TankAmmoPoolGroup.Awake()` — 在 `Instance = this` 后添加 `DontDestroyOnLoad(gameObject)`
2. 新建 `DontDestroyOnLoadMarker.cs` — 极简标记组件，挂载后自动跨场景保持，拖到 `TerrainObjectPoolGroup` 上

**影响范围：** 
- `TankAmmoPoolGroup.cs` — Awake 增加一行
- `Assets/Scripts/ObjectSystem/DontDestroyOnLoadMarker.cs` — 新建
- 需在 Unity Editor 中为 `TerrainObjectPoolGroup` 挂载 `DontDestroyOnLoadMarker` 组件

---

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
