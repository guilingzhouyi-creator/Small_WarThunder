# 东七三 — Small_WarThunder

[📋 开发日志](./DEVLOG.md) · [🗺️ 路线图](./ROADMAP.md) · [📝 更新日志](./CHANGELOG.md)

**版本号**: `0.1.000（测试/Beta）` · [📥 下载最新版本](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/latest)
**引擎**: Unity 6000.3.11f1 · **渲染**: HDRP 17.3.0 · **音频**: FMOD Studio  
**仓库**: [github.com/guilingzhouyi-creator/Small_WarThunder](https://github.com/guilingzhouyi-creator/Small_WarThunder)

---

## 项目简介

《东七三》是一个基于 Unity 的坦克载具模拟项目，以现代战争雷霆类游戏为参考，实现了完整的坦克移动、火控、装填、碰撞检测与 HUD 系统。项目采用 **Component + Partial Class 模块化架构**，所有坦克参数通过 ScriptableObject 数据驱动，UI 侧以 UI Toolkit 为主。

当前阶段聚焦于**核心玩法闭环**：坦克可移动/转向、炮塔可旋转/俯仰、可开火/装填/切换弹药、HUD 准星与火控信息实时同步。

---

## 当前版本说明 — v0.1.000（测试/Beta）

### 已实现的核心系统

| 系统 | 状态 | 说明 |
|------|------|------|
| 坦克移动 | ✅ 完成 | 尼基金转向公式 + 功率分配 + 各向异性摩擦力，10 个 partial 文件 |
| 炮塔控制 | ✅ 完成 | TPS/AIM 双模式旋转、自由视角（C 键）、炮管碰撞规避 |
| 开火与装填 | ✅ 完成 | 弹药切换、装填计时、测距、弹道计算 |
| 火控 HUD | ✅ 完成 | 自定义布局、FOV 缩放、准星/刻度/读数框/填充支持 |
| 碰撞与伤害 | ✅ 完成 | 装甲区域、穿透/跳弹判定、伤害结算 |
| 对象池 | ✅ 完成 | 炮弹池化回收 |
| 音频系统 | ✅ 完成 | FMOD 引擎状态机 + 一次性音效 |
| 悬挂系统 | ✅ 完成 | 悬挂臂 + 轮子旋转 + 视觉 |
| 天气系统 | ✅ 完成 | 动态天气切换 |
| UI 系统 | ✅ 完成 | 暂停/设置/HUD/瞄准镜 |

### 下一个版本计划 — v0.2.000

- **任务系统**：任务目标、进度追踪、完成判定
- **多坦克支持**：选择不同坦克、切换载具
- **伤害视觉反馈**：车体损伤模型、起火/爆炸特效
- **AI 敌方坦克**：基础 AI 巡逻/索敌/开火
- **地图系统**：小地图、标记、战术视图
- **设置持久化**：图形/音频/控制设置保存与加载

### 待评估/远期规划

- 网络联机（Photon/Netcode）
- 完整战役模式
- 坦克自定义改装
- 回放系统

---

## 资源资产包

项目的大文件资源（模型、音频、贴图、FMOD 插件等）通过 **Git LFS** 管理，同时提供独立的 ZIP 资源包用于 Release 分发。

### 资源包内容

| 类别 | 内容 | 路径 |
|------|------|------|
| 坦克模型 | T90A 测试车型模型 (.fbx) | `Assets/Art/TankModel/` |
| 字体 | SourceHanSans 字体系列 | `Assets/Art/Font/` |
| 贴图 | 测试图片、UI 贴图 | `Assets/Art/Pictures/` |
| 音频 | OST、FMOD Bank | `Assets/Audio/`、`Assets/Desktop/` |
| FMOD 插件 | 多平台原生库 | `Assets/Miscellaneous Management System/Plugins/FMOD/` |
| 雾效资源包 | VolumetricFog2 (Builtin/URP) | `Assets/Unity Asset Management System/VolumetricFogBundle/` |

### 生成资源包

在 Unity Editor 中执行菜单 `Tools / 构建资源资产包`，将在项目根目录生成 `Small_WarThunder_Assets_v{version}.zip`。

---

## 项目框架总括

```
GameManager                    ← 全局生命周期
  └─ UIManager                 ← UI 管理（暂停/任务/HUD/设置）
       └─ TankAImUIController  → FcsHudPainter（瞄准 HUD）
MIddleInputingController       ← 输入中介层（InputSystem → 游戏逻辑）
TankMoveController             ← 坦克移动（10 partial files）
TankWeaponController           ← 炮塔/武器（5 partial files）
TankFireController             ← 开火/装填/测距（9 partial files）
TankController                 ← 坦克整体（6 partial files）
AudioManager                   ← 音频（FMOD）
WeatherController              ← 天气
TankSuspensionManager          ← 悬挂
GeneralHitPosition             ← 碰撞/伤害
CannonBall + Objectpooler      ← 弹药/对象池
```

### 数据驱动架构

所有坦克参数通过 ScriptableObject 配置：

| SO | 主要参数 |
|----|----------|
| `TankMoveData` | 质量/速度/加速度/功率/调教曲线 |
| `TankTurretData` | 旋转速度/俯仰角/炮管避撞 |
| `TankAudioData` | FMOD 事件/引擎状态层 |
| `NewAimConfigData` | HUD 布局/元素/变焦 |
| `ProjectileData` | 弹丸参数 |
| `ArmoredZoneData` | 装甲区域 |

### 设计模式

| 模式 | 位置 |
|------|------|
| Singleton | 大部分控制器 |
| Partial Class | 大型控制器拆分 |
| Mediator | MIddleInputingController |
| Observer | C# events |
| State Machine | 转向/引擎音频 |
| Registry | FCSRegistrySystem |
| Strategy | 转向策略 |
| Data-Driven | ScriptableObject |
| Object Pool | 炮弹池 |

---

## 贡献与合作

### 开发人员

- **guilingzhouyi-creator** — 项目发起人、主程、架构设计

### 贡献方式

欢迎提交 Issue 和 Pull Request。请遵循以下原则：

1. 代码风格遵循项目既有约定（`_camelCase` 私有字段、PascalCase 公共属性）
2. 修改前先阅读 `ARCHITECTURE.md` 了解架构
3. 涉及对象池时，尊重池化回收流程
4. 涉及 UI 时，同时考虑事件订阅和状态刷新
5. 大改动建议先开 Issue 讨论

### 第三方资源

- FMOD Studio — 音频中间件
- SourceHanSans — 思源黑体字体（SIL Open Font License）
- VolumetricFog2 — 体积雾效果（Unity Asset Store）

---

> **构建要求**: Unity 6000.3.11f1 + HDRP 17.3.0  
> **首次克隆后**: 确保已安装 Git LFS，执行 `git lfs pull` 拉取资源文件
