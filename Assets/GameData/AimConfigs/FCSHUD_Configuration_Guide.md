# FCS HUD 配置说明

本文档说明当前新 FCS HUD 的配置方式、默认预设含义，以及如何扩展成更接近 M1A2 风格的现代火控界面。

## 架构概览

当前 HUD 不是写死在代码里，而是由 `NewAimConfigData` 驱动：

- `HudLayout` 决定使用哪种默认布局。
- `HudElements` 决定自定义 HUD 元素集合。
- `HudThemeColor`、`LineThickness`、`HudScale` 等负责整体视觉风格。
- `ZoomFovLevels`、`ZoomSmoothSpeed`、`BaseSensitivity` 负责 AIM 模式的视角和操作响应。

HUD 绘制由 `FcsHudPainter` 执行，UI 位置由 `TankAImUIController` 控制，注册和模式切换由 `FCSRegistry` / `UIManager` 负责。

## 预设模式

### `TacticalRing`

TPS 预设，默认画白色圆环样式，适合自由视角和第三人称状态。

适用场景：
- TPS 视角
- 需要弱干扰、轻量提示的模式

### `ModernFireControl`

现代火控默认模板，适合 AIM 视角。

默认包含：
- 大型中心框
- 中央瞄点
- 四角框线
- 少量辅助刻度

适用场景：
- AIM 视角
- 接近 M1A2、现代坦克火控界面的布局

### `Custom`

完全自定义模式。启用后，`HudElements` 列表里的元素会按顺序绘制。

适用场景：
- 需要自己搭一套 M1A2 风格 HUD
- 需要单独放置速度条、状态框、目标框、方位环等元素

### 当前默认中心模板

当前 AIM 默认资产已经改成更接近“中心准星”的紧凑布局，而不是大框模板。它的默认组合更偏向：

- 一个小的中心十字
- 一个小中心点
- 一层轻量框线/标记
- 横向与纵向的短刻度辅助

如果你要做你截图里那种“中间很干净、只有中心火控符号”的样式，优先改这些字段：

- `HudLayout = Custom`
- `HudThemeColor`
- `HudElements`
- `CrosshairLength`
- `CrosshairGap`
- `HudScale`

如果你要更像现代火控面板，再逐步往 `HudElements` 里加 `ReadoutBox`、`TextSlot`、`RectangleFrame`、`CornerTick`。

## 元素类型

`HudElements` 中每个元素都是一个 `HudElementDefinition`。

### 已支持的元素

- `Crosshair`：十字准星
- `Ring`：圆环
- `Graticule`：刻度线
- `CornerBracket`：四角框线
- `CenterDot`：中心点
- `HorizontalScale`：横向刻度条
- `VerticalScale`：纵向刻度条
- `RectangleFrame`：矩形边框
- `ReadoutBox`：读数框
- `CornerTick`：单角标记
- `TextSlot`：文本占位槽

### 预留接口说明

当前 `TextSlot`、`ReadoutBox` 主要用于“显示区域占位”和“结构保留”。

也就是说：
- 你现在可以先把框位摆出来。
- 真正的文字渲染、数值绑定、动态单位显示，后续可以单独接数据接口。

这避免一开始把 HUD 做死，后面难以扩展。

## 元素字段

每个 `HudElementDefinition` 都有这些字段：

- `ElementType`：元素类型
- `Anchor`：锚点位置
- `Offset`：相对锚点偏移
- `Size`：尺寸，通常用于框大小或线段长度
- `Thickness`：线宽
- `Radius`：圆形/中心点半径
- `RepeatCount`：重复数量，适合刻度线
- `RepeatSpacing`：重复间距
- `ScaleTotalLength`：用于 `HorizontalScale` / `VerticalScale` 的总线段长度
- `ScaleDecayPerStep`：每一级向外递减的长度，减到 `0` 或以下时该线段不再渲染
- `Color`：该元素颜色
- `Enabled`：是否启用
- `Filled`：预留给实心框/状态块
- `Text`：预留给未来文本显示

## 锚点说明

`Anchor` 表示元素挂在哪个屏幕区域：

- `Center`：屏幕中心
- `TopLeft`：左上角
- `TopRight`：右上角
- `BottomLeft`：左下角
- `BottomRight`：右下角
- `TopCenter`：上中
- `BottomCenter`：下中
- `LeftCenter`：左中
- `RightCenter`：右中

## 推荐的现代 HUD 组法

如果你想做接近 M1A2 的风格，推荐这样配：

- 中心一组：`RectangleFrame` + `CenterDot` + `CornerBracket`
- 左右信息区：`ReadoutBox` + `VerticalScale`
- 顶部信息区：`TextSlot` 或 `ReadoutBox`
- 底部状态区：`ReadoutBox` + `HorizontalScale`

## 典型参数建议

- `HudScale`
  - 1.0：标准大小
  - 1.5~2.0：更明显、更适合现代 HUD
- `HudFovReference`
  - 参考 FOV，默认建议和相机默认 FOV 保持一致，通常是 `60`
- `HudFovScaleExponent`
  - FOV 变小时 HUD 放大的响应强度，建议 `0.25~0.45`
- `HudFovScaleMin` / `HudFovScaleMax`
  - 约束 HUD 缩放范围，避免极端 FOV 下画面失控
- `CrosshairLength`
  - 越大，中心准星越明显
- `CrosshairGap`
  - 越大，中间留白越多，越像现代火控面板
- `GraticuleSpacingMil`
  - 标尺密位间距，数值越大，刻度之间越疏
- `GraticuleLineHalfWidth`
  - 每条刻度线的半宽，越大线越长
- `GraticuleStartOffsetMil`
  - 刻度从中心向外的起始偏移，用来留出中心净空
- `ScaleTotalLength`
  - `VerticalScale` / `HorizontalScale` 的线段总长。它决定中心那一级刻度有多长
- `ScaleDecayPerStep`
  - 每向外一级减少多少长度。长度减到 `0` 或以下时，这一级不再绘制，形成阶梯式递减
- `TpsRingRadius`
  - TPS 模式白圆环半径
- `TpsRingThickness`
  - TPS 圆环粗细

## 当前默认值

- AIM 资产：`HudLayout = Custom`
- TPS 资产：`HudLayout = TacticalRing`

AIM 默认模板已经预留出：
- 中心小准星
- 中心点
- 轻量边框/角标
- 横纵向短刻度
- 读数框和文本槽接口

## 扩展建议

如果后面你要继续做更现代的火控界面，建议按下面顺序扩展：

1. 先把布局摆出来
2. 再把数据区接到 HUD 元素上
3. 最后再做文字、数值和动态报警

这样不会把绘制逻辑和数据逻辑缠死。
