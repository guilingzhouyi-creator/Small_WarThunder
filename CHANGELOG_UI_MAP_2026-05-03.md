# UI / Map Change Notes

日期：2026-05-03

这份文档记录本次对地图系统、区域触发和 UI 覆盖关系的改动，方便下次继续迭代。

## 1. 相机切换规则

- 把 `CameraTransitionConfig` 从字符串相机名改成了 `CameraBlendTarget` 枚举。
- 运行时由 `CameraSystem` 把枚举解析成当前场景里真实绑定的相机名，再生成 Cinemachine blend 规则。
- 避免了 `AIMCamera` / `AImCamera` 这种大小写或改名导致的 `Cut` 失效。

涉及文件：

- `Assets/GameData/CameraConfigs/CameraTransitionConfig.cs`
- `Assets/GameData/CameraConfigs/CameraTransitionConfig.asset`
- `Assets/Scripts/Player/CameraSystem.cs`
- `Assets/Scripts/Tank/Camera/CameraPosition.cs`
- `Assets/Scripts/Tank/Camera/ZoomCameraPosition.cs`
- `Assets/Scripts/Tank/Camera/AimCameraPosition.cs`
- `Assets/Scripts/MapSystem/MapCameraPosition.cs`

## 2. 任务区域触发去重与缓冲区

- `GameLevelMaker` 增加了进入/退出缓冲区：
  - `Enter Buffer Meters`
  - `Exit Buffer Meters`
  - `Enter Confirm Seconds`
  - `Exit Confirm Seconds`
- 进入采用“内圈 + 持续时间确认”。
- 退出采用“外圈 + 持续时间确认”。
- `OnTriggerEnter/Stay/Exit` 不再直接驱动叙事，而是只维护玩家碰撞体集合，再由统一判定逻辑驱动进入/离开。
- `GameLevelManager` 增加了同一玩家、同一区域、已在区域内的重复进入保护。
- 给任务区域增加了 Scene Gizmos：
  - 绿色线框：进入缓冲区
  - 橙色线框：退出缓冲区

涉及文件：

- `Assets/Scripts/Mission/GameLevelMaker.cs`
- `Assets/Scripts/Mission/GameLevelManager.cs`

## 3. 地图渲染链路

- 修复了 `UIManager` 输入解绑写反的问题，避免输入事件重复注册。
- `MapRenderingEngine` 现在自己维护 `RenderTexture`，并把它设置到地图相机上。
- 地图 UI 不再只是纯色背景 + Painter2D 覆盖，而是可以显示真实俯拍渲染。
- `MapUIController` 增加了运行时自动补引用：
  - `UIDocument`
  - `MapCameraPosition`
  - 玩家 `Transform`
- 打开大地图时会强制可见，不再被“小地图隐藏逻辑”覆盖。

涉及文件：

- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/DifferentUIController/MapUIController.cs`
- `Assets/Scripts/MapSystem/MapRenderingEngine.cs`

## 4. 地图相机跟随与居中

- `MapCameraPosition` 现在兼容两层结构：
  - `CinemachineCamera`
  - 子级真实渲染 `Camera`
- `LateUpdate()` 中会直接同步真实渲染 Camera 到玩家上方。
- `MapUIController` 拿到玩家引用后会主动调用 `MapCameraPosition.BindTarget(playerTransform)`。
- `MapRenderingEngine` 的 UI 坐标中心优先使用玩家世界坐标，因此玩家在 RT / 小地图上的位置应始终在中心。

涉及文件：

- `Assets/Scripts/MapSystem/MapCameraPosition.cs`
- `Assets/Scripts/UI/DifferentUIController/MapUIController.cs`
- `Assets/Scripts/MapSystem/MapRenderingEngine.cs`

## 5. UI 覆盖关系重构

目标关系：

- `Map` 和 `Tab` 可以互相覆盖。
- 关闭上层后返回下层。
- `ESC(Pause)` 压在 `Map/Tab` 之上。
- `Setting` 压在 `Pause` 之上。

### 已完成

- 新增覆盖栈：
  - `Assets/Scripts/UI/UIOverlayStack.cs`
- 定义覆盖层枚举：
  - `UIOverlayId.None`
  - `UIOverlayId.Map`
  - `UIOverlayId.Tab`
  - `UIOverlayId.Pause`
  - `UIOverlayId.Setting`
- `UIManager` 改成由覆盖栈驱动：
  - 显示谁
  - 输入是否锁定
  - 鼠标是否解锁
  - 关闭后恢复到哪一层

### 新接口

`UIManager` 现在推荐使用：

- `OpenOverlay(UIOverlayId overlay)`
- `CloseOverlay(UIOverlayId overlay)`
- `ToggleOverlay(UIOverlayId overlay)`

旧接口仍保留，但现在只是兼容层：

- `SetPaused`
- `SetMapShown`
- `SetTabed`
- `ShowSettingsUI`
- `ShowPauseUI`

## 6. 外围 UI 调用统一

- `PauseUIController` 已切到新接口：
  - 打开设置：`OpenOverlay(UIOverlayId.Setting)`
  - Resume/Quit 关闭暂停：`CloseOverlay(UIOverlayId.Pause)`
- `SettingManager` 已切到新接口：
  - 显示设置：`OpenOverlay(UIOverlayId.Setting)`
  - 返回暂停：`CloseOverlay(UIOverlayId.Setting)`

涉及文件：

- `Assets/Scripts/UI/DifferentUIController/PauseUIController.cs`
- `Assets/Scripts/UI/SettingManager/SettingManager.cs`

## 7. 下次建议继续检查

- Unity Play 模式下完整验证以下流程：
  - `M -> Tab -> Tab`
  - `Tab -> M -> M`
  - `M/Tab -> ESC -> ESC`
  - `ESC -> Setting -> Back`
- 检查大地图是否真正全屏铺满 `UIDocument`。
- 检查 `MainMenuUIController` 是否也要切到 `OpenOverlay(UIOverlayId.Setting)` 风格，还是保留现状。
- 如果后续还要扩展 UI，建议把旧兼容接口逐步标记为 obsolete。

