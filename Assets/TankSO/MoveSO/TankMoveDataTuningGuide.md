# TankMoveData 调参说明

适用对象：`Assets/TankSO/MoveSO/TankMoveData.cs`

## 1. 单位说明

这套移动系统按 Unity 物理量理解：

| 字段 | 单位 | 说明 |
|---|---:|---|
| `Mass` | kg | 坦克质量 |
| `MoveMaxSpeed` | m/s | 前进最大速度 |
| `BackMoveMaxSpeed` | m/s | 后退最大速度 |
| `MoveAcceleration` | m/s² | 前进加速度 |
| `BackMoveAcceleration` | m/s² | 后退加速度 |
| `MoveTurnMaxSpeed` | deg/s | 行进间转向速度上限 |
| `LocalTurnMaxSpeed` | deg/s | 原地转向速度上限 |
| `TurnAccelerationTime` | s | 转向加速时间，当前主要作为保留项 |
| `MaxClimbAngle` | deg | 最大爬坡角 |
| `EnginePowerKw` | kW | 引擎额定功率 |
| `TransmissionEfficiency` | 0-1 | 传动效率 |
| `MinimumPowerSpeed` | m/s | 最小功率速度阈值 |
| `PivotTurnPowerFactor` | 0-1 | 原地转向功率折减系数 |
| `PivotTurnEfficiency` | 0-1 | 原地转向响应系数 |
| `DefaultGroundFrictionCoefficient` | 无量纲 | 默认地面摩擦系数 |
| `TrackContactLength` | m | 履带接地长度 |
| `TrackCenterDistance` | m | 履带中心距 |

## 2. 怎么理解当前系统

当前 `TankMoveController` 不是“直接改速度”，而是：

1. 读取输入。
2. 计算当前地面摩擦与坡度。
3. 估算可用功率预算。
4. 先扣掉转向需求，再把剩余功率给前进/后退。
5. 最后交给 Rigidbody 做物理积分。

所以速度变化小，不一定只是 `MoveAcceleration` 小，也可能是：

- `EnginePowerKw` 不够
- `Mass` 太大
- `TransmissionEfficiency` 太低
- 地面摩擦系数太高
- 转向需求过重

## 3. 推荐调参顺序

### 3.1 先调“体感最明显”的三个项

- `EnginePowerKw`：决定整车有没有劲。
- `MoveAcceleration` / `BackMoveAcceleration`：决定起步和跟手程度。
- `TransmissionEfficiency`：决定动力损耗大小。

### 3.2 再调“转向手感”的三个项

- `MoveTurnMaxSpeed`
- `LocalTurnMaxSpeed`
- `PivotTurnEfficiency`

### 3.3 最后调“环境适配”的三个项

- `DefaultGroundFrictionCoefficient`
- `TrackContactLength`
- `TrackCenterDistance`

## 4. 建议起始值

### 4.1 均衡型

- `Mass`：35000
- `MoveMaxSpeed`：12
- `BackMoveMaxSpeed`：5
- `MoveAcceleration`：2.8
- `BackMoveAcceleration`：2.0
- `MoveTurnMaxSpeed`：32
- `LocalTurnMaxSpeed`：42
- `EnginePowerKw`：800
- `TransmissionEfficiency`：0.84
- `MinimumPowerSpeed`：0.5
- `PivotTurnPowerFactor`：0.6
- `PivotTurnEfficiency`：0.7
- `DefaultGroundFrictionCoefficient`：1.0
- `TrackContactLength`：4.5
- `TrackCenterDistance`：2.8

### 4.2 机动型

- `Mass`：28000
- `MoveMaxSpeed`：15
- `BackMoveMaxSpeed`：6
- `MoveAcceleration`：4.0
- `BackMoveAcceleration`：2.8
- `MoveTurnMaxSpeed`：38
- `LocalTurnMaxSpeed`：52
- `EnginePowerKw`：950
- `TransmissionEfficiency`：0.88
- `MinimumPowerSpeed`：0.4
- `PivotTurnPowerFactor`：0.5
- `PivotTurnEfficiency`：0.78
- `DefaultGroundFrictionCoefficient`：0.95
- `TrackContactLength`：4.0
- `TrackCenterDistance`：2.6

### 4.3 重型型

- `Mass`：48000
- `MoveMaxSpeed`：9
- `BackMoveMaxSpeed`：4
- `MoveAcceleration`：1.8
- `BackMoveAcceleration`：1.2
- `MoveTurnMaxSpeed`：24
- `LocalTurnMaxSpeed`：32
- `EnginePowerKw`：650
- `TransmissionEfficiency`：0.8
- `MinimumPowerSpeed`：0.6
- `PivotTurnPowerFactor`：0.75
- `PivotTurnEfficiency`：0.6
- `DefaultGroundFrictionCoefficient`：1.1
- `TrackContactLength`：5.0
- `TrackCenterDistance`：3.0

## 5. 曲线怎么填

### `EnginePowerCurve`

横轴是归一化速度，纵轴是功率倍率。

建议：
- 起点接近 `1`
- 中高速可轻微下滑
- 不要让曲线在高速度段掉得太狠，否则会感觉“越跑越没劲”

### `SteeringResistanceCurve`

横轴是 `R / B`，纵轴是尼基金修正系数。

建议：
- 小半径时值更高
- 大半径时逐渐接近 `1`
- 如果你想更强的原地抱死感，就把低区间拉高

## 6. 速度显示注意

如果 UI 显示的是 `Km/h`，需要把物理速度从 `m/s` 转换成 `Km/h`：

`km/h = m/s * 3.6`

如果不做转换，数值会看起来偏小。

## 7. 调参结论

- 想要“更快起步”：先加 `MoveAcceleration`，再看 `EnginePowerKw`
- 想要“更高极速”：调 `MoveMaxSpeed`
- 想要“更灵敏转向”：调 `LocalTurnMaxSpeed` 和 `PivotTurnEfficiency`
- 想要“更重更笨”：加大 `Mass`，降低 `TransmissionEfficiency`
- 想要“地面差异更明显”：调 `DefaultGroundFrictionCoefficient`
