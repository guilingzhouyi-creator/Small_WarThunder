# 东七三

《东七三》是一个 Unity 坦克载具项目，当前代码结构按“场景与全局调度、载具控制、火控与 UI、数据资产、资源内容”几层组织。项目使用 ScriptableObject 维护大部分运行时配置，使用 Unity 场景与 Resources 进行内容装载，UI 侧以 UI Toolkit 为主。

## 架构概览

### 1. 场景与全局调度
- `ProjectSettings/` 保存 Unity 项目的全局设置。
- `Assets/GameMainAllSystem/` 放置场景加载、全局注册中心、时间管理、字幕和 UI 入口等系统。
- `Assets/GameSwitchSceneList/` 保存菜单、加载和战斗等场景。
- `Assets/Resources/SceneCatalog.asset` 作为场景目录，供场景加载器按资源引用读取。

### 2. 载具控制层
- `Assets/Scripts/MoveManager/` 负责坦克移动、转向、动力与悬挂相关逻辑。
- `Assets/Scripts/MoveManager/TankTurretMove/` 负责炮塔转动、武器控制和火控上报。
- `Assets/Scripts/FireController/` 负责装填、开火、弹药切换、发射与验证等职责。
- `Assets/GamePlayer/PlayerStateSnapshot/FCSSnapshot.cs` 是火控快照结构，记录炮口位置、朝向、相机矩阵和屏幕尺寸。

### 3. 火控、HUD 和 UI
- `Assets/GameMainAllSystem/CentralRegistrySystem/FCSRegistrySystem.cs` 保存当前玩家火控快照，HUD/UI 从这里读取统一状态。
- `Assets/GameMainAllSystem/Global EngineService/FCSEngine.cs` 负责世界坐标到屏幕坐标、弹道落点等计算。
- `Assets/GameMainAllSystem/NewUIDesignSystem/` 与 `Assets/Scripts/UIController/` 共同负责 UI Toolkit HUD、准星、设置和战斗界面。

### 4. 数据与资源
- `Assets/SOManager/` 与 `Assets/TankSO/` 保存所有核心 ScriptableObject 配置，包括瞄准、炮塔、移动、弹药、任务文本等。
- `Assets/Art/`、`Assets/Audio/`、`Assets/Desktop/`、`Assets/prefabs/` 保存模型、贴图、音频和预制体资源。
- `Assets/Miscellaneous Management System/Plugins/FMOD/` 保存 FMOD 插件和音频资源。

## 运行时数据流

1. 输入系统将移动、开火、自由视角和 UI 状态交给载具控制器。
2. `TankWeaponController` 每帧生成 `FCSSnapshot`，写入 `FCSRegistry`。
3. `FCSRegistry` 供 HUD、准星和 UI 读取统一火控状态。
4. `FCSEngine` 根据快照中的矩阵、炮口方向和屏幕尺寸完成坐标转换与弹道计算。
5. 音频通过 FMOD 和 `AudioManager`/坦克侧 partial 文件分层处理，不直接把音频逻辑写进主业务文件。

## 代码组织原则

- 配置优先：大多数可调参数放在 SO 里，而不是硬编码在控制器里。
- 分层清晰：移动、火控、UI、音频、碰撞各自独立，控制器只负责调用与状态流转。
- 资源与引用成对提交：`.meta` 文件必须保留，避免 Unity GUID 丢失。
- 生成物不入库：`Library/`、`Temp/`、`Logs/`、`*.csproj`、`*.lscache` 等本地生成文件不提交。

## 首次提交范围

- 必须提交：`Assets/`、`Packages/`、`ProjectSettings/`、`README.md`、`.gitignore`
- 仅首次保留一次：`Small_WarThunder.slnx`
- 不提交：Unity 生成目录、IDE 缓存和临时文件
