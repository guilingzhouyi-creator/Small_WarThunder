**引擎**: Unity 6000.3.11f1 · **渲染**: HDRP 17.3.0 · **音频**: FMOD Studio  
> **基于 Commit**: `83c0334e` (项目基线初始提交)

---

## 目录

- [渲染与依赖](#渲染与依赖)
- [架构总览](#架构总览)
- [核心系统](#核心系统)
- [数据驱动](#数据驱动)
- [设计模式](#设计模式)

---

## 渲染与依赖

| 组件 | 版本 |
|------|------|
| HDRP | 17.3.0 |
| Cinemachine | 3.1.6 |
| Input System | 1.19.0 |
| UI Toolkit | 内置 |
| ProBuilder | 6.0.9 |
| Visual Scripting | 1.9.11 |

## 架构总览

项目采用 **Component + Partial Class 模块化** 架构。核心控制器以 `MonoBehaviour` 为基类，通过 C# `partial class` 按职责拆分到多个文件。

```
GameManager              ← 全局生命周期
  └─ UIManager           ← UI 管理（暂停/任务/HUD/设置）
       └─ TankAImUIController → FcsHudPainter（瞄准 HUD）
MIddleInputingController ← 输入中介层（InputSystem → 游戏逻辑）
TankMoveController       ← 坦克移动 (10 files)
TankWeaponController     ← 炮塔/武器 (5 files)
TankFireController       ← 开火/装填/测距 (9 files)
TankController           ← 坦克整体 (6 files)
AudioManager             ← 音频 (FMOD)
WeatherController        ← 天气
```

### 火控数据流

```
TankWeaponController → FCSRegistrySystem → TankAImUIController → FcsHudPainter
  (数据生产者)         (静态注册中心)       (UI控制器)            (UI Toolkit 绘制)
```

---

## 核心系统

### 坦克移动 — TankMoveController

10 个 partial class 文件按职责划分：

| 文件 | 职责 |
|------|------|
| `.cs` | 单例、引用、物理采样、Gizmos |
| `.Input.cs` | 前进/后退/转向输入 |
| `.Powertrain.cs` | 引擎开关、电力管理、输入锁 |
| `.Hsm.cs` | 转向状态机 (Idle→Straight→Pivot/Brake/MovingTurn) |
| `.Motion.cs` | 物理循环 (SimulatePowerSplit) |
| `.Power.cs` | 功率分配、尼基金公式 |
| `.Ground.cs` | 地面摩擦系数 |
| `.Audio.cs` | 引擎音频状态机 → FMOD |
| `.Validation.cs` | 运行时验证 |

**物理核心**: 尼基金转向阻力公式 + 功率预算分配 + 各向异性摩擦力

```
AvailablePower = EnginePower × Efficiency × PowerCurve
转向消耗 ≤ AvailablePower × 65%（最多吃掉 65% 引擎功率）
剩余功率 = 前进驱动 + 克服滚阻
```

### 坦克武器 — TankWeaponController

5 个 partial class:

| 文件 | 职责 |
|------|------|
| `.cs` | 核心、FCS 注册、瞄准点计算 |
| `.MainGunTurn.cs` | 炮塔旋转 (TPS/AIM 模式) |
| `.FreeViewpoint.cs` | 自由视角 (C 键) |
| `.Collision.cs` | 炮管碰撞规避（SphereCast + 二分搜索） |
| `.ExtraFunction.cs` | 编辑器可视化 |

### 音频系统 — AudioManager + FMOD

```
TankAudioDatabase (SO)
  └─ TankAudioData (每坦克)
       ├─ 引擎状态机: Off→Startup→Idle→Move→Shutdown
       └─ 一次性音效: fire/reload/hit
AudioVolumeCategory 分类: Engine/Weapon/Reload/Impact
```

引擎音频由 RPM、负载、速度实时驱动。

### 其他系统

- **悬挂**: TankSuspensionManager（悬挂臂 + 轮子旋转 + 视觉）
- **碰撞/伤害**: GeneralHitPosition + TargetDamageResolver
- **弹药**: CannonBall + Objectpooler
- **受击检测**: 炮弹命中 → 穿透/跳弹 → 伤害结算

---

## 数据驱动

所有坦克参数通过 ScriptableObject 配置：

| SO | 主要参数 |
|----|----------|
| `TankMoveData` | 质量/速度/加速度/功率/多条调教曲线 |
| `TankTurretData` | 旋转速度/俯仰角/炮管避撞 |
| `TankAudioData` | FMOD 事件/引擎状态层 |
| `NewAimConfigData` | HUD 布局/元素/变焦 |
| `ProjectileData` | 弹丸参数 |
| `ArmoredZoneData` | 装甲区域 |

---

## 设计模式

| 模式 | 位置 |
|------|------|
| **Singleton** | 大部分控制器 (TankMove/Weapon/Fire/UI/Audio) |
| **Partial Class** | 大型控制器拆分为 5~10 个文件 |
| **Mediator** | MIddleInputingController (输入 → 逻辑) |
| **Observer** | C# events (引擎状态/速度/暂停) |
| **State Machine** | 转向状态机、引擎音频状态机 |
| **Registry** | FCSRegistrySystem (火控数据注册) |
| **Strategy** | 不同转向策略 (Pivot/Brake/Arc) |
| **Data-Driven** | ScriptableObject 配置所有坦克参数 |
| **Object Pool** | 炮弹对象池 |
