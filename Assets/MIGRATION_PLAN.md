# 项目目录参考手册

> 当前项目已完成目录重组。此文件为**当前实际目录索引**，修改文件前先查此表定位目标路径。

---

## 一、源数据 `Assets/Data/`

| 目录 | 内容 |
|------|------|
| `Data/MissionsCSV/` | 任务 CSV 源数据 |

---

## 二、运行时配置 `Assets/GameData/` (ScriptableObject)

| 目录 | 内容 |
|------|------|
| `GameData/TankConfigs/` | 坦克配置（原 `TankSO/`） |
| `GameData/AimConfigs/` | 瞄准配置（原 `SOManager/AimConfigSO`） |
| `GameData/MissionConfigs/` | 任务配置（原 `SOManager/TextShowSO`） |
| `GameData/TimeConfigs/` | 时间配置（原 `SOManager/TimeSO`） |
| `GameData/LevelConfigs/` | 关卡配置（原 `GameData/LevelsData`） |
| `GameData/CameraConfigs/` | 摄像机配置 |
| `GameData/MapConfigs/` | 地图配置 |
| `GameData/MissionsCSV/` | 任务 CSV（与 `Data/MissionsCSV` 同步） |

---

## 三、场景 `Assets/Levels/`

| 文件 | 说明 |
|------|------|
| `Levels/Game.unity` | 主游戏场景 |
| `Levels/GameOverScene.unity` | 游戏结束场景 |
| `Levels/LoadingScene.unity` | 加载场景 |
| `Levels/MainMenuScene.unity` | 主菜单场景 |

---

## 四、代码 `Assets/Scripts/`（按功能域划分）

### 4.1 坦克系统 `Scripts/Tank/`

| 子目录 | 功能 | 来源 |
|--------|------|------|
| `Tank/TankerController/` | 坦克驾驶员控制器 | 原 `Scripts/TankerController` |
| `Tank/FireController/` | 坦克发射控制 | 原 `Scripts/TankFireController` |
| `Tank/CannonBall/` | 炮弹逻辑 | 原 `Scripts/ConnonBall` |
| `Tank/Turret/` | 炮塔旋转/俯仰 | 原 `Scripts/MoveManager/TankTurretMove` |
| `Tank/Movement/` | 车体移动 | 原 `Scripts/MoveManager/TankBodyMove` |
| `Tank/Track/` | 履带 | 原 `Scripts/MoveManager/TankTrackMove` |
| `Tank/Suspension/` | 悬挂系统 | 原 `Scripts/MoveManager/.../SuspenSystem` |
| `Tank/Camera/` | 坦克摄像机 | 原 `Scripts/CameraManager` 中坦克相关 |
| `Tank/Collision/` | 碰撞/命中检测 | 原 `GameMainAllSystem/CollideSystem` |
| `Tank/UI/` | 坦克相关 UI（瞄准等） | |
| `Tank/ObjectPool/` | 坦克弹药池 | |

### 4.2 战斗系统 `Scripts/Combat/`

| 子目录 | 功能 | 来源 |
|--------|------|------|
| `Combat/Damage/` | 伤害计算/上报 | 原 `GameMainAllSystem/DamageSystem` |

### 4.3 AI 系统 `Scripts/AI/`

| 子目录 | 功能 | 来源 |
|--------|------|------|
| `AI/` | 敌方 AI | 原 `Scripts/EnemyManager` |

### 4.4 地图系统 `Scripts/MapSystem/`

| 文件 | 说明 |
|------|------|
| `MapSystem/MapRenderingEngine.cs` | 地图渲染引擎 (UI Toolkit) |
| `MapSystem/MapCameraPosition.cs` | 地图摄像机 |
| `MapSystem/MapConfigSO.cs` | 地图配置 SO |
| `MapSystem/MapSnapshot.cs` | 地图快照数据结构 |

来源：原 `GameMainAllSystem/MapSystem` + `MapCamera` 等合并。

### 4.5 任务系统 `Scripts/Mission/`

来源：原 `Scripts/GameLevelManager` + `MissionRegistrySystem` 等合并。

### 4.6 玩家系统 `Scripts/Player/`

来源：原 `Scripts/PlayerManager` + `GameMainAllSystem/PlayerSystem` 合并。

### 4.7 框架基础设施 `Scripts/Core/`

| 子目录 | 功能 | 来源 |
|--------|------|------|
| `Core/GameManager/` | 游戏主管理器 | 原 `Scripts/GameManager` |
| `Core/Input/` | 输入系统 | 原 `GameMainAllSystem/GameInputSystem` |
| `Core/Time/` | 时间管理 | 原 `GameMainAllSystem/GameTimeManagementSystem` |
| `Core/Scene/` | 场景切换 | 原 `GameMainAllSystem/GameSceneSwitchSystem` |
| `Core/Engine/` | 全局引擎服务 | 原 `GameMainAllSystem/Global EngineService` |
| `Core/Registry/` | 中央注册表 | 原 `GameMainAllSystem/CentralRegistrySystem` |
| `Core/Subtitle/` | 自动字幕引擎 | 原 `GameMainAllSystem/AutomatedSubtitleEngineSystem` |
| `Core/ObjectPool/` | 通用对象池 | |
| `Core/CG/` | CG 系统 | 原 `GameMainAllSystem/CGSystem` |

### 4.8 全局 UI `Scripts/UI/`

| 子目录 | 说明 |
|--------|------|
| `UI/DifferentUIController/` | 不同 UI 控制器 |
| `UI/SettingManager/` | 设置管理 |

### 4.9 音频管理 `Scripts/Audio/`

| 子目录 | 来源 |
|--------|------|
| `Audio/Manager/` | 原 `Scripts/AudioManager` |

### 4.10 天气系统 `Scripts/Weather/`

来源：原 `Scripts/WeatherController`。

---

## 五、美术资产 `Assets/Art/`

| 子目录 | 内容 |
|--------|------|
| `Art/Aim/` | 瞄准素材 |
| `Art/CannonBall/` | 炮弹素材 |
| `Art/Font/` | 字体 |
| `Art/Map/` | 地图贴图 |
| `Art/Pictures/` | 图片 |
| `Art/TankModel/` | 坦克模型 |
| `Art/车体部件模型/` | 车体部件模型 |

---

## 六、音频资产 `Assets/Audio/`

| 子目录 / 文件 | 说明 |
|------|------|
| `Audio/Bank/` | FMOD Bank 文件（15 个 .bank） |
| `Audio/*.mp3` | 背景音乐 |

---

## 七、预制体 `Assets/Prefabs/`

| 子目录 | 内容 |
|--------|------|
| `prefabs/Enemy坦克预制体/` | 敌方坦克 |
| `prefabs/Player坦克预制体/` | 玩家坦克 |
| `prefabs/Scene/` | 场景预制体 |
| `prefabs/UI/` | UI 预制体 |
| `prefabs/坦克部件/` | 坦克部件 |
| `prefabs/天气/` | 天气系统 |
| `prefabs/弹药/` | 弹药预制体 |
| `prefabs/靶标测试/` | 靶标测试 |

---

## 八、其他目录

| 目录 | 说明 |
|------|------|
| `PhysicMaterials/` | 物理材质（`PhysicMat_Ground`, `PhysicMat_Tank`） |
| `Plugins/FMOD/` | FMOD 音频插件 |
| `Resources/` | Resources 加载（`SceneCatalog.asset`） |
| `Editor/` | 编辑器工具（`Area.cs`, `BuildAssetZip.cs`, `MissionDataImporter.cs`, `MeshBreak/` 等） |
| `GamePlayer/` | 玩家状态快照 |
