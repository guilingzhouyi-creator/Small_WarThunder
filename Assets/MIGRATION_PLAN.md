# 项目目录参考手册

> 当前项目已完成目录重组。此文件为**当前真实目录索引**，修改文件前先按此表定位实际路径。

---

## 一、源数据 `Assets/Data/`

| 目录 | 内容 |
|------|------|
| `Data/MissionsCSV/` | 任务 CSV 源数据 |

---

## 二、运行时配置 `Assets/GameData/`

| 目录 | 内容 |
|------|------|
| `GameData/AIConfigs/` | AI 配置 |
| `GameData/AIData/` | AI 数据 |
| `GameData/AimConfigs/` | 瞄准配置 |
| `GameData/CameraConfigs/` | 摄像机配置 |
| `GameData/IndicatorConfigs/` | 指示器配置 |
| `GameData/InputConfigs/` | 输入/按键配置 |
| `GameData/LayerConfigs/` | 图层配置 |
| `GameData/LevelConfigs/` | 关卡配置 |
| `GameData/MapConfigs/` | 地图配置 |
| `GameData/MissionConfigs/` | 任务配置 |
| `GameData/MissionsCSV/` | 任务 CSV（与 `Data/MissionsCSV` 同步） |
| `GameData/SettingConfigs/` | 设置配置 |
| `GameData/TaskSystemData/` | 任务系统数据 |
| `GameData/TankConfigs/` | 坦克配置 |
| `GameData/TextRenderConfigs/` | 文本渲染配置 |
| `GameData/TimeConfigs/` | 时间配置 |

### 2.1 已确认的关键配置文件

| 文件 | 说明 |
|------|------|
| `GameData/InputConfigs/KeyBindingData.cs` | 按键绑定配置数据 |
| `GameData/InputConfigs/KeyBindingSaveData.cs` | 按键绑定保存数据 |

### 2.2 任务系统数据 `GameData/TaskSystemData/`

| 目录 | 内容 |
|------|------|
| `TaskSystemData/Config/` | 任务系统配置 |
| `TaskSystemData/Event/` | 任务事件规则 |
| `TaskSystemData/State/` | 任务状态定义 |

#### 任务系统数据关键文件

| 文件 | 说明 |
|------|------|
| `TaskSystemData/State/TaskStateSO.cs` | 任务状态快照 SO |
| `TaskSystemData/Event/TaskEventSO.cs` | 任务事件 SO |
| `TaskSystemData/Event/TaskEventRuleSO.cs` | 任务事件规则 SO |

---

## 三、场景 `Assets/Levels/`

| 文件 | 说明 |
|------|------|
| `Levels/Game.unity` | 主游戏场景 |
| `Levels/GameOverScene.unity` | 游戏结束场景 |
| `Levels/LoadingScene.unity` | 加载场景 |
| `Levels/MainMenuScene.unity` | 主菜单场景 |

---

## 四、代码 `Assets/Scripts/`

> 下面按现有真实目录列出当前仓库中最常用、最容易定位的关键文件。

### 4.1 坦克系统 `Scripts/Tank/`

| 子目录 | 说明 |
|--------|------|
| `Tank/TankerController/` | 坦克主体控制 |
| `Tank/FireController/` | 坦克发射控制 |
| `Tank/CannonBall/` | 炮弹逻辑 |
| `Tank/Turret/` | 炮塔旋转/俯仰 |
| `Tank/Movement/` | 车体移动 |
| `Tank/Track/` | 履带 |
| `Tank/Suspension/` | 悬挂系统 |
| `Tank/Camera/` | 坦克摄像机 |
| `Tank/Collision/` | 碰撞/命中检测 |
| `Tank/UI/` | 坦克相关 UI |
| `Tank/ObjectPool/` | 坦克对象池 |

#### 坦克系统目录文件

| 文件 / 目录 | 说明 |
|------|------|
| `Tank/TankerController/Tank.cs` | 坦克主体 |
| `Tank/TankerController/Tank.API.cs` | 坦克 API |
| `Tank/TankerController/Tank.AmmunitionDepot.cs` | 弹药仓 |
| `Tank/TankerController/Tank.Collider.cs` | 坦克碰撞 |
| `Tank/TankerController/Tank.ECSFunction.cs` | ECS 功能 |
| `Tank/TankerController/Tank.Member.cs` | 成员数据 |
| `Tank/FireController/TankFireController.cs` | 坦克开火主控制器 |
| `Tank/FireController/TankFireController.Spawn.cs` | 弹药生成 |
| `Tank/FireController/TankFireController.Reload.cs` | 装填逻辑 |
| `Tank/FireController/TankFireController.Ammo.cs` | 弹药切换 |
| `Tank/FireController/TankFireController.MachineGun.cs` | 机枪控制 |
| `Tank/FireController/TankFireController.Rangefinder.cs` | 测距仪 |
| `Tank/FireController/TankFireController.Direction.cs` | 射击方向 |
| `Tank/FireController/TankFireController.Audio.cs` | 开火音频 |
| `Tank/FireController/TankFireController.Validation.cs` | 参数校验 |
| `Tank/CannonBall/CannonBall.cs` | 炮弹主体 |
| `Tank/CannonBall/CannonBall.HitDetection.cs` | 命中检测 |
| `Tank/CannonBall/CannonBall.InformationTransmission.cs` | 信息上报 |
| `Tank/CannonBall/CannonBall.Validation.cs` | 参数校验 |
| `Tank/Turret/TankWeaponController.cs` | 炮塔/武器主控制器 |
| `Tank/Turret/TankWeaponController.MainGunTurn.cs` | 主炮转动 |
| `Tank/Turret/TankWeaponController.FreeViewpoint.cs` | 自由视角 |
| `Tank/Turret/TankWeaponController.Collision.cs` | 碰撞处理 |
| `Tank/Turret/TankWeaponController.ExtraFunction.cs` | 附加功能 |
| `Tank/Movement/TankMoveController.cs` | 车体移动主控制器 |
| `Tank/Movement/TankMoveController.Input.cs` | 移动输入处理 |
| `Tank/Movement/TankMoveController.Motion.cs` | 运动逻辑 |
| `Tank/Movement/TankMoveController.Power.cs` | 动力输出 |
| `Tank/Movement/TankMoveController.Powertrain.cs` | 动力链处理 |
| `Tank/Movement/TankMoveController.Ground.cs` | 地面检测 |
| `Tank/Movement/TankMoveController.Hsm.cs` | 状态机 |
| `Tank/Movement/TankMoveController.Audio.cs` | 移动音频 |
| `Tank/Movement/TankMoveController.Validation.cs` | 参数校验 |
| `Tank/Movement/TankTrackSideDrivePoint.cs` | 履带侧驱动点 |
| `Tank/Track/TrackController.cs` | 履带控制 |
| `Tank/Track/TrackController.Audio.cs` | 履带音频 |
| `Tank/Suspension/TankSuspensionManager.cs` | 悬挂总控 |
| `Tank/Suspension/TankSuspensionManager.NodeBinding.cs` | 悬挂节点绑定 |
| `Tank/Suspension/TankSuspensionArm.cs` | 悬挂臂 |
| `Tank/Suspension/WheelRotatorManager.cs` | 轮子旋转管理 |
| `Tank/Suspension/WheelVisualSpinSign.cs` | 轮子视觉转向修正 |
| `Tank/Suspension/FixedWheelMarker.cs` | 固定轮标记 |
| `Tank/Suspension/MainSuspensionMarker.cs` | 主悬挂标记 |
| `Tank/Suspension/WheelPivotMarker.cs` | 轮轴枢轴标记 |
| `Tank/Camera/CameraPosition.cs` | 坦克相机基础位置 |
| `Tank/Camera/AimCameraPosition.cs` | 瞄准相机位置 |
| `Tank/Camera/ZoomCameraPosition.cs` | 变焦相机位置 |
| `Tank/Camera/TankCameraBindMarker.cs` | 坦克相机绑定标记 |
| `Tank/Collision/GeneralBarrelAvoidanceCollider.cs` | 炮管规避碰撞 |
| `Tank/Collision/TargetDurone.cs` | 目标模型 |
| `Tank/Collision/WaterTankCollide.cs` | 水域碰撞 |
| `Tank/Collision/GeneralHitSystem/` | 通用命中系统 |
| `Tank/Collision/靶标测试碰撞/` | 靶标测试碰撞目录 |
| `Tank/UI/FcsHudPainter.cs` | FCS HUD 绘制 |
| `Tank/ObjectPool/TankAmmoPoolGroup.cs` | 弹药池组 |
| `Tank/ObjectPool/TankAmmoPoolMarker.cs` | 弹药池标记 |

#### 坦克系统关键文件

| 文件 | 说明 |
|------|------|
| `Tank/Movement/TankMoveController.cs` | 车体移动主控制器 |
| `Tank/Movement/TankMoveController.Input.cs` | 移动输入处理 |
| `Tank/Movement/TankMoveController.Motion.cs` | 运动逻辑 |
| `Tank/Movement/TankMoveController.Power.cs` | 动力输出 |
| `Tank/Movement/TankMoveController.Powertrain.cs` | 动力链处理 |
| `Tank/Movement/TankMoveController.Ground.cs` | 地面检测 |
| `Tank/Movement/TankMoveController.Hsm.cs` | 状态机 |
| `Tank/Movement/TankMoveController.Audio.cs` | 移动音频 |
| `Tank/Movement/TankMoveController.Validation.cs` | 参数校验 |
| `Tank/Movement/TankTrackSideDrivePoint.cs` | 履带侧驱动点 |
| `Tank/FireController/TankFireController.cs` | 坦克开火主控制器 |
| `Tank/FireController/TankFireController.Spawn.cs` | 弹药生成 |
| `Tank/FireController/TankFireController.Reload.cs` | 装填逻辑 |
| `Tank/FireController/TankFireController.Ammo.cs` | 弹药切换 |
| `Tank/FireController/TankFireController.MachineGun.cs` | 机枪控制 |
| `Tank/FireController/TankFireController.Rangefinder.cs` | 测距仪 |
| `Tank/FireController/TankFireController.Direction.cs` | 射击方向 |
| `Tank/FireController/TankFireController.Audio.cs` | 开火音频 |
| `Tank/FireController/TankFireController.Validation.cs` | 参数校验 |
| `Tank/Turret/TankWeaponController.cs` | 炮塔/武器主控制器 |
| `Tank/Turret/TankWeaponController.MainGunTurn.cs` | 主炮转动 |
| `Tank/Turret/TankWeaponController.FreeViewpoint.cs` | 自由视角 |
| `Tank/Turret/TankWeaponController.Collision.cs` | 碰撞处理 |
| `Tank/Turret/TankWeaponController.ExtraFunction.cs` | 附加功能 |
| `Tank/Camera/CameraPosition.cs` | 坦克相机基础位置 |
| `Tank/Camera/AimCameraPosition.cs` | 瞄准相机位置 |
| `Tank/Camera/ZoomCameraPosition.cs` | 变焦相机位置 |
| `Tank/Camera/TankCameraBindMarker.cs` | 坦克相机绑定标记 |
| `Tank/Suspension/TankSuspensionManager.cs` | 悬挂总控 |
| `Tank/Suspension/TankSuspensionManager.NodeBinding.cs` | 悬挂节点绑定 |
| `Tank/Suspension/TankSuspensionArm.cs` | 悬挂臂 |
| `Tank/Suspension/WheelRotatorManager.cs` | 轮子旋转管理 |
| `Tank/Suspension/WheelVisualSpinSign.cs` | 轮子视觉转向修正 |
| `Tank/Suspension/FixedWheelMarker.cs` | 固定轮标记 |
| `Tank/Suspension/MainSuspensionMarker.cs` | 主悬挂标记 |

#### 输入与相机补充文件

| 文件 | 说明 |
|------|------|
| `Tank/Camera/AimCameraPosition.cs` | 瞄准相机位置 |
| `Tank/Camera/CameraPosition.cs` | 基础相机位置 |
| `Tank/Camera/ZoomCameraPosition.cs` | 变焦相机位置 |
| `Tank/Camera/TankCameraBindMarker.cs` | 相机绑定标记 |
| `Tank/Movement/TankTrackSideDrivePoint.cs` | 履带侧驱动点 |

### 4.2 战斗系统 `Scripts/Combat/`

| 子目录 | 说明 |
|--------|------|
| `Combat/Damage/` | 伤害计算与上报 |

#### 战斗系统关键文件

| 文件 | 说明 |
|------|------|
| `Combat/Damage/TargetDamageResolver.cs` | 目标伤害解析 |

### 4.2.1 战斗 HUD 与指示器补充

| 文件 | 说明 |
|------|------|
| `Indicator/IndicatorInterface.cs` | 指示器接口 |
| `Indicator/IndicatorManager.cs` | 指示器管理器 |
| `Indicator/IndicatorObject.cs` | 指示器对象 |
| `Indicator/IndicatorRenderer.cs` | 指示器渲染器 |

### 4.3 AI 系统 `Scripts/AI/`

| 子目录 | 说明 |
|--------|------|
| `AI/Behavior/` | 行为树 / 行为逻辑 |
| `AI/Control/` | AI 行为控制 |
| `AI/Core/` | AI 核心定义 |
| `AI/Perception/` | AI 感知 |
| `AI/Suspension/` | AI 悬挂 |

#### AI 系统关键文件

| 文件 | 说明 |
|------|------|
| `AI/Core/AI_Controller.cs` | AI 顶层控制器 |
| `AI/Behavior/AI_BehaviorTreeSystem.cs` | 行为树系统 |
| `AI/Control/AI_MoveController.cs` | AI 移动控制 |
| `AI/Control/AI_FireController.cs` | AI 开火控制 |
| `AI/Control/AI_MotionDriver.cs` | AI 运动驱动 |
| `AI/Control/AI_WeaponController.cs` | AI 武器控制 |
| `AI/Control/AI_TurretController.cs` | AI 炮塔控制 |
| `AI/Control/AI_TrackVisualDriver.cs` | AI 履带视觉驱动 |
| `AI/Perception/AI_PerceptionManager.cs` | AI 感知管理 |
| `AI/Perception/AI_AwarenessMeterSystem.cs` | AI 警觉度系统 |
| `AI/Suspension/AI_SuspensionManager.cs` | AI 悬挂总控 |
| `AI/Suspension/AI_SuspensionManager.NodeBinding.cs` | 悬挂节点绑定 |
| `AI/Suspension/AI_SuspensionArm.cs` | AI 悬挂臂 |
| `AI/Suspension/AI_SuspensionController.cs` | AI 悬挂控制 |
| `AI/Suspension/AI_WheelVisualDriver.cs` | AI 轮子视觉驱动 |

### 4.4 地图系统 `Scripts/MapSystem/`

| 文件 | 说明 |
|------|------|
| `MapSystem/MapRenderingEngine.cs` | 地图渲染引擎 (UI Toolkit) |
| `MapSystem/MapCameraPosition.cs` | 地图摄像机位置 |
| `MapSystem/MapConfigSO.cs` | 地图配置 SO |
| `MapSystem/MapSnapshot.cs` | 地图快照数据结构 |

#### 地图系统补充说明

| 文件 | 说明 |
|------|------|
| `MapSystem/MapUIController.cs` | 地图 UI 控制 |

### 4.5 任务系统 `Scripts/Mission/`

| 子目录 | 说明 |
|--------|------|
| `Mission/` | 任务流程、叙事和关卡任务运行时 |

#### 任务系统关键文件

| 文件 | 说明 |
|------|------|
| `Mission/GameLevelManager.cs` | 关卡与任务总管理 |
| `Mission/LevelMissionMarker.cs` | 关卡任务标记 |
| `Mission/MissionHandler.cs` | 任务处理器 |
| `Mission/MissionNarrativeRuntime.cs` | 叙事运行时 |
| `Mission/MissionRegistrySystem.cs` | 任务注册系统 |
| `Mission/PatrolTarget.cs` | 巡逻目标 |
| `Mission/TaskDistributionSystem.cs` | 任务分发 |
| `Mission/TaskVerificationSystem.cs` | 任务验证 |

### 4.6 玩家系统 `Scripts/Player/`

| 子目录 | 说明 |
|--------|------|
| `Player/` | 玩家相关运行时逻辑 |

#### 玩家与输入关键文件

| 文件 | 说明 |
|------|------|
| `Core/GameManager/GameManager.cs` | 游戏主管理器 |
| `Core/GameManager/MIddleInputingController.cs` | 中间输入控制器 |
| `Core/Input/GameInputingSystem.cs` | 输入系统资产脚本 |
| `Core/Input/GameInputingSystem.inputactions` | 输入动作配置 |
| `Core/Input/KeyBindingManager.cs` | 按键绑定管理 |
| `Core/Input/KeyBindingSaveManager.cs` | 按键绑定保存管理 |
| `Core/Engine/ColorThemeSO.cs` | 颜色主题配置 |
| `Core/Engine/CursorEngine.cs` | 鼠标指针引擎 |
| `Core/Engine/FCSEngine.cs` | FCS 引擎 |
| `Core/Engine/LevelStreamingEngine.cs` | 关卡流式加载引擎 |
| `Core/Scene/LoadingController.cs` | 加载流程控制 |
| `Core/Scene/SceneCatalog.cs` | 场景目录 |
| `Core/Scene/SceneInitializer.cs` | 场景初始化 |
| `Core/Scene/StaticStringMatchAndSceneLoadSystem.cs` | 字符串匹配场景加载系统 |
| `Core/Scene/GameSceneSwitchResources/` | 场景切换资源目录 |
| `Core/Time/TimeManager.cs` | 时间管理器 |
| `Core/Time/TimeSystem.cs` | 时间系统 |
| `Core/Persistence/SaveManager.cs` | 存档管理器 |
| `Core/Registry/FCSRegistrySystem.cs` | FCS 注册系统 |
| `Core/Registry/LevelRegistrySystem.cs` | 关卡注册系统 |
| `Core/CG/CgClip.cs` | CG 片段 |
| `Core/CG/CgPlaybackSystem.cs` | CG 播放系统 |
| `Core/CG/CgClip.asset` | CG 片段资源 |
| `UI/SettingManager/KeyBinding/KeyBindingPanel.cs` | 按键面板 |

#### 玩家系统补充文件

| 文件 | 说明 |
|------|------|
| `Player/CameraSystem.cs` | 玩家摄像机系统 |
| `Player/PlayerSpawnSystem.cs` | 玩家出生系统 |

### 4.7 框架基础设施 `Scripts/Core/`

| 子目录 | 说明 |
|--------|------|
| `Core/GameManager/` | 游戏主管理器 |
| `Core/Input/` | 输入系统 |
| `Core/Time/` | 时间管理 |
| `Core/Scene/` | 场景切换 |
| `Core/Engine/` | 全局引擎服务 |
| `Core/Registry/` | 中央注册表 |
| `Core/Subtitle/` | 自动字幕引擎 |
| `Core/ObjectPool/` | 通用对象池 |
| `Core/CG/` | CG 系统 |
| `Core/Constants/` | 全局常量 |
| `Core/Persistence/` | 持久化 |
| `Core/Anime/` | 动画辅助 |
| `Core/Marker/` | 运行时标记组件 |

#### Core 子目录关键文件

| 文件 | 说明 |
|------|------|
| `Core/Input/GameInputingSystem.cs` | 输入系统资产脚本 |
| `Core/Input/GameInputingSystem.inputactions` | 输入动作配置 |
| `Core/Input/KeyBindingManager.cs` | 按键绑定管理 |
| `Core/Input/KeyBindingSaveManager.cs` | 按键绑定保存管理 |
| `Core/Scene/LoadingController.cs` | 加载流程控制 |
| `Core/Scene/SceneCatalog.cs` | 场景目录 |
| `Core/Scene/SceneInitializer.cs` | 场景初始化 |
| `Core/Scene/StaticStringMatchAndSceneLoadSystem.cs` | 字符串匹配场景加载系统 |
| `Core/Scene/GameSceneSwitchResources/` | 场景切换资源目录 |
| `Core/Engine/ColorThemeSO.cs` | 颜色主题配置 |
| `Core/Engine/CursorEngine.cs` | 鼠标指针引擎 |
| `Core/Engine/FCSEngine.cs` | FCS 引擎 |
| `Core/Engine/LevelStreamingEngine.cs` | 关卡流式加载引擎 |
| `Core/GameManager/GameManager.cs` | 游戏主管理器 |
| `Core/GameManager/GameManager.Validation.cs` | 游戏主管理器校验 |
| `Core/GameManager/MIddleInputingController.cs` | 中间输入控制器 |
| `Core/CG/CgClip.cs` | CG 片段 |
| `Core/CG/CgPlaybackSystem.cs` | CG 播放系统 |
| `Core/Constants/ActionDisplayNameMapping.cs` | 动作显示名映射 |
| `Core/Constants/AIConstants.cs` | AI 常量 |
| `Core/Constants/KeyBindingConstants.cs` | 按键绑定常量 |
| `Core/Constants/SceneAssetPaths.cs` | 场景资源路径常量 |
| `Core/Constants/SettingConstants.cs` | 设置常量 |
| `Core/Constants/SubtitleRenderScope.cs` | 字幕渲染范围 |
| `Core/Constants/UIStandardTexts.cs` | UI 标准文本 |
| `Core/Constants/UIStyleClassNames.cs` | UI 样式类名 |
| `Core/Persistence/SaveManager.cs` | 存档管理器 |
| `Core/Registry/FCSRegistrySystem.cs` | FCS 注册系统 |
| `Core/Registry/LevelRegistrySystem.cs` | 关卡注册系统 |
| `Core/Subtitle/AutomatedSubtitleEngineSystem.cs` | 自动字幕系统旧入口 |
| `Core/Subtitle/GlobalSubtitleEngine.cs` | 全局字幕引擎 |
| `Core/Subtitle/GlobalSubtitleEngine.Intelligence.cs` | 字幕智能逻辑 |
| `Core/Subtitle/GlobalSubtitleEngine.Overlay.cs` | 字幕覆盖层 |
| `Core/Subtitle/GlobalSubtitleEngine.PanelNarrativeBuffer.cs` | 面板叙事缓冲 |
| `Core/Subtitle/GlobalSubtitleEngine.TaskText.cs` | 任务字幕文本 |
| `Core/Subtitle/SubtitleChannel.cs` | 字幕通道 |
| `Core/Subtitle/SubtitleColorRenderEngine.cs` | 字幕颜色渲染 |
| `Core/Subtitle/SubtitlePackage.cs` | 字幕包数据 |
| `Core/ObjectPool/DontDestroyOnLoadMarker.cs` | 常驻标记 |
| `Core/ObjectPool/Objectpooler.cs` | 对象池器 |
| `Core/ObjectPool/ObjectPoolSystem.cs` | 对象池系统 |
| `Core/ObjectPool/PooledObject.cs` | 池化对象 |
| `Core/Time/TimeManager.cs` | 时间管理器 |
| `Core/Time/TimeSystem.cs` | 时间系统 |
| `Core/Marker/EnemyMarker.cs` | 敌方标记 |
| `Core/Marker/GameLevelMarker.cs` | 关卡标记 |
| `Core/Marker/PlayerMarker.cs` | 玩家标记 |

### 4.8 全局 UI `Scripts/UI/`

| 子目录 | 说明 |
|--------|------|
| `UI/HUD/` | 战斗 HUD 与瞄准/任务/字幕面板 |
| `UI/NewUIFramework/Adapter/` | UI 适配器层 |
| `UI/NewUIFramework/Core/` | UI 管理核心层 |
| `UI/Overlay/` | 暂停等覆盖 UI |
| `UI/SettingManager/` | 设置管理 |

#### UI NewUIFramework 关键文件

| 文件 | 说明 |
|------|------|
| `UI/NewUIFramework/Adapter/IUIController.cs` | UI 控制器接口 |
| `UI/NewUIFramework/Adapter/IUIViewAdapter.cs` | 视图适配器接口 |
| `UI/NewUIFramework/Adapter/UGUIViewAdapter.cs` | UGUI 适配器 |
| `UI/NewUIFramework/Adapter/UIToolkitViewAdapter.cs` | UI Toolkit 适配器 |
| `UI/NewUIFramework/Core/NewUIManager.cs` | 新 UI 管理器 |
| `UI/NewUIFramework/Core/NewUIManager.Core.cs` | 核心逻辑 |
| `UI/NewUIFramework/Core/NewUIManager.Input.cs` | 输入处理 |
| `UI/NewUIFramework/Core/NewUIManager.Map.cs` | 地图 UI |
| `UI/NewUIFramework/Core/NewUIManager.Mission.cs` | 任务 UI |
| `UI/NewUIFramework/Core/NewUIManager.Overlay.cs` | 覆盖层 UI |
| `UI/NewUIFramework/Core/NewUIManager.Pause.cs` | 暂停 UI |
| `UI/NewUIFramework/Core/NewUIManager.State.cs` | UI 状态 |
| `UI/NewUIFramework/Core/UIStackManager.cs` | UI 栈管理 |
| `UI/NewUIFramework/Core/EUIContextType.cs` | UI 上下文类型 |
| `UI/NewUIFramework/Core/EUIIdentity.cs` | UI 身份标识 |
| `UI/NewUIFramework/Core/EUIPushBehavior.cs` | UI 压栈行为 |
| `UI/NewUIFramework/Core/IUIRegistry.cs` | UI 注册接口 |

`UI/SettingManager/KeyBinding/` 的实际文件：`KeyBindingController.cs`、`KeyBindingController.Vehicle.cs`、`KeyBindingItemWidget.cs`、`KeyBindingPanel.cs`。

`UI/SettingManager/` 的实际子目录：`Audio/`、`Core/`、`General/`、`KeyBinding/`、`Prompt/`、`Visual/`。

#### UI Overlay 关键文件

| 文件 | 说明 |
|------|------|
| `UI/Overlay/PauseUIController.cs` | 暂停界面 |
| `UI/Overlay/GameOverUIController.cs` | 游戏结束界面 |
| `UI/Overlay/MainMenuUIController.cs` | 主菜单界面 |

#### UI SettingManager 关键文件

| 文件 | 说明 |
|------|------|
| `UI/SettingManager/Core/SettingManager.cs` | 设置总管理 |
| `UI/SettingManager/Core/SettingManager.AutoConfig.cs` | 自动配置 |
| `UI/SettingManager/Core/SettingManager.InteractionRouter.cs` | 交互路由 |
| `UI/SettingManager/Core/Setting.Ensure.cs` | 保障逻辑 |
| `UI/SettingManager/Core/SettingTabContent.cs` | 设置页内容 |
| `UI/SettingManager/Core/ISettingTabController.cs` | 设置页接口 |
| `UI/SettingManager/Core/SettingActionResult.cs` | 设置动作结果 |
| `UI/SettingManager/Core/SettingPromptRouter.cs` | 提示路由器 |
| `UI/SettingManager/Prompt/SettingPromptData.cs` | 提示数据 |
| `UI/SettingManager/Prompt/SettingPromptService.cs` | 提示服务 |
| `UI/SettingManager/General/GeneralSettingController.cs` | 通用设置 |
| `UI/SettingManager/Visual/VisualSettingController.cs` | 画面设置 |
| `UI/SettingManager/Audio/AudioSettingController.cs` | 音频设置 |
| `UI/SettingManager/Audio/AudioSettingController.Persistence.cs` | 音频持久化 |
| `UI/SettingManager/Audio/AudioCategoryVolumeItem.cs` | 分类音量条目 |
| `UI/SettingManager/KeyBinding/KeyBindingController.cs` | 按键绑定控制 |
| `UI/SettingManager/KeyBinding/KeyBindingController.Vehicle.cs` | 车辆按键绑定 |
| `UI/SettingManager/KeyBinding/KeyBindingItemWidget.cs` | 按键项控件 |
| `UI/SettingManager/KeyBinding/KeyBindingPanel.cs` | 按键面板 |

### 4.9 音频管理 `Scripts/Audio/`

| 子目录 | 说明 |
|--------|------|
| `Audio/Manager/` | 音频管理运行时逻辑 |

#### 音频与表现关键文件

| 文件 | 说明 |
|------|------|
| `Audio/Manager/AudioManager.cs` | 音频管理器 |
| `Audio/Manager/AudioManager.API.cs` | 音频 API 适配 |
| `Audio/Manager/AudioManager.Code.cs` | 音频逻辑 |
| `Audio/Manager/AudioVolumeCategory.cs` | 音量分类 |

### 4.10 天气系统 `Scripts/Weather/`

| 子目录 | 说明 |
|--------|------|
| `Weather/` | 天气控制与表现 |

#### 其他系统关键文件

| 文件 | 说明 |
|------|------|
| `Debug/FPSShow.cs` | 调试 FPS 显示 |
| `Debug/TrackPathVisualizer.cs` | 履带轨迹可视化 |
| `Weather/WeatherController.cs` | 天气控制 |

### 4.11 指示器系统 `Scripts/Indicator/`

| 文件 | 说明 |
|------|------|
| `Indicator/IndicatorInterface.cs` | 指示器接口 |
| `Indicator/IndicatorManager.cs` | 指示器管理器 |
| `Indicator/IndicatorObject.cs` | 指示器对象 |
| `Indicator/IndicatorRenderer.cs` | 指示器渲染器 |

### 4.12 玩家状态快照 `Assets/GamePlayer/`

| 目录 | 内容 |
|------|------|
| `GamePlayer/PlayerStateSnapshot/` | 玩家状态快照数据 |

#### 玩家快照关键文件

| 文件 | 说明 |
|------|------|
| `GamePlayer/PlayerStateSnapshot/FCSSnapshot.cs` | FCS 状态快照 |

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
| `Audio/Bank/` | FMOD Bank 文件 |
| `Audio/*.mp3` | 背景音乐 |

---

## 七、预制体 `Assets/prefabs/`

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

#### UI 预制体细目

| 文件 / 目录 | 说明 |
|------|------|
| `prefabs/UI/Buttons/` | 按钮相关预制体目录 |
| `prefabs/UI/CGManager.prefab` | CG 管理预制体 |
| `prefabs/UI/KeyBingPrefab.prefab` | 按键绑定预制体 |
| `prefabs/UI/PrefbSetVolumeSlider.prefab` | 音量滑条预制体 |

---

## 八、其他目录

| 目录 | 说明 |
|------|------|
| `PhysicMaterials/` | 物理材质 |
| `Plugins/FMOD/` | FMOD 音频插件 |
| `Resources/` | Resources 资源 |
| `Editor/` | 编辑器工具 |
| `GamePlayer/` | 玩家状态快照 |
| `UI Toolkit/` | UI Toolkit 资源与主题 |
| `UI Toolkit/SettingPrompt/` | Setting 提示框 UXML / USS |
| `UI Toolkit/UnityThemes/` | Unity 主题资源 |
| `Miscellaneous Management System/` | 其他管理系统资源 |
| `Unity Asset Management System/` | Unity 资源管理系统资源 |
| `_Recovery/` | 恢复/回收目录 |

> 说明：`Assets/.git/` 和 `Assets/.vscode/` 是工作区/工具目录，不纳入运行时内容索引。

---

## 九、快速定位建议

| 需求 | 优先查看 |
|------|----------|
| 按键输入与映射 | `Core/Input/`、`UI/SettingManager/KeyBinding/` |
| 坦克移动 | `Tank/Movement/` |
| 坦克开火 | `Tank/FireController/` |
| 炮塔与瞄准 | `Tank/Turret/`、`Tank/Camera/`、`UI/HUD/` |
| 悬挂与轮子 | `Tank/Suspension/` |
| AI | `AI/Control/`、`AI/Perception/`、`AI/Suspension/` |
| 地图 | `MapSystem/`、`UI/HUD/MapUIController.cs` |
| 任务与字幕 | `Mission/`、`Core/Subtitle/`、`UI/HUD/MissionPannelUIController.cs` |
    