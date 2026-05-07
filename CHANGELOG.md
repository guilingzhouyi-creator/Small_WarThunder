# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

# 更新日志

## [v0.1.000-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.000-beta) — 2026-05-07

### ⚡ 重大改动 — 26.5.7

v0.1.000-beta

## [v0.1.001-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.001-beta) — 2026-05-05

### ⚡ 重大改动 — 2026-05-05 17:15

**新增内容：**
自动化仓管系统规范——hook 放宽第4行校验、脚本只递增 patch 版本号

**改动及优化描述：**
清除上次误插入的重复 CHANGELOG 条目；commit-msg hook 改 vX.Y.Z 为任意文本校验；update_logs.py bump_version 改为 patch+1（保留 major.minor 人工控制）

## [v0.1.000-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.000-beta) — 2026-04-30

### Baseline — Core Gameplay Loop

First public beta of Dong Qi San, completing the core tank simulation loop.

这是《东七三》项目的第一个测试版本，完成了坦克载具模拟的核心玩法闭环。

### 新增

#### 坦克移动系统
- 尼基金转向阻力公式实现的转向物理
- 功率预算分配（转向最多消耗 65% 引擎功率）
- 各向异性地面摩擦力
- 转向状态机：Idle → Straight → Pivot/Brake/MovingTurn
- 引擎开关、电力管理、输入锁
- 引擎音频状态机 → FMOD 驱动

#### 炮塔与武器系统
- TPS/AIM 双模式炮塔旋转
- 自由视角（C 键切换）
- 炮管碰撞规避（SphereCast + 二分搜索）
- 俯仰角限制与平滑

#### 开火与装填系统
- 弹药切换（AP/HE/APCR 等）
- 装填计时与进度
- 激光测距与弹道计算
- 开火验证（角度/装填/弹药检查）

#### 火控 HUD（FCS）
- 自定义 HUD 布局系统（锚点/偏移/缩放）
- 准星、刻度线、环、角括号、中心点
- 读数框（支持填充背景）
- 水平和垂直刻度尺
- FOV 缩放适配
- 预设布局：Modern / TacticalRing / Custom

#### 碰撞与伤害
- 装甲区域定义（ArmoredZoneData）
- 穿透/跳弹角度判定
- 伤害结算与命中上报

#### 对象池
- 炮弹通用对象池（Objectpooler + PooledObject）
- 池化回收流程

#### 音频系统
- FMOD Studio 集成
- 引擎状态机：Off → Startup → Idle → Move → Shutdown
- 一次性音效：fire / reload / hit
- 音量分类控制（Engine / Weapon / Reload / Impact）

#### 悬挂系统
- 悬挂臂物理模拟
- 轮子旋转与视觉同步

#### UI 系统
- 暂停界面
- 设置界面（图形/音频/控制）
- HUD 与瞄准镜
- 任务界面框架

#### 天气系统
- 动态天气切换

### 技术基础设施
- Git LFS 管理大文件（模型/音频/插件）
- HDRP 渲染管线 17.3.0
- UI Toolkit 自定义 Painter2D HUD
- FMOD Studio 音频中间件
- Cinemachine 相机系统
- Input System 输入管理

### 项目结构
- 10 个 partial 文件拆分 TankMoveController
- 5 个 partial 文件拆分 TankWeaponController
- 9 个 partial 文件拆分 TankFireController
- 6 个 partial 文件拆分 TankController
- ScriptableObject 数据驱动所有坦克参数
- FCSRegistrySystem 火控数据注册中心
