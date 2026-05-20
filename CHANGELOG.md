# 更新日志

所有重要变更将记录在此文件，格式遵循 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)。

---

## [v0.1.003-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.003-beta) — 2026-05-20

### ⚡ 重大改动 — 5.20.12:15

# v0.1.003-beta 版本更新公告

各位玩家，

本次更新集中强化了玩家在战场上的感知能力与单位追踪体系，同时对后台工作流程进行了关键修复，以提升整体稳定性。

**核心更新：三大感知与追踪系统正式实装**

我们完成了玩家侦查与感知系统、敌方高亮系统以及全局单位追踪

## [v0.1.002-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.002-beta) — 2026-05-19

### 改动

- 清理 CHANGELOG 重复条目，统一日志格式规范
- 优化工作流 paths-ignore，增加 .last_processed 过滤避免仓管自身递归触发
- 修复自动化仓管工作流被跳过导致积压提交未处理的问题

---

## [v0.1.001-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.001-beta) — 2026-05-05

### 新增

- AI 总结集成：Deepseek AI 自动生成版本发布说明
- CHANGELOG 按重大改动批次独立处理与版本号递增
- 全局光标锁定引擎（纯静态服务，统一光标管理）
- 全局时间管理器（跨场景时间流速集中化控制）
- 标记组件重命名统一化（PlayerMarker / EnemyMarker / GameLevelMarker）

### 改动

- commit-msg hook 放宽校验，改为任意文本格式兼容
- update_logs.py 版本号递增规则改为 patch+1，major.minor 人工控制
- DEVLOG 条目不再暴露文件路径/类名，仅保留功能级描述
- 光标锁定、时间流速等分散逻辑收敛至全局服务

---

## [v0.1.000-beta](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/tag/v0.1.000-beta) — 2026-04-30

### 新增

#### 坦克移动系统
- 尼基金转向阻力公式实现的转向物理
- 功率预算分配（转向最多消耗 65% 引擎功率）
- 各向异性地面摩擦力
- 转向状态机：Idle → Straight → Pivot/Brake/MovingTurn
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
- FOV 缩放适配

#### 碰撞与伤害
- 装甲区域定义（ArmoredZoneData）
- 穿透/跳弹角度判定
- 伤害结算与命中上报

#### 对象池
- 炮弹通用对象池（Objectpooler + PooledObject）

#### 音频系统
- FMOD Studio 集成
- 引擎状态机：Off → Startup → Idle → Move → Shutdown
- 音量分类控制（Engine / Weapon / Reload / Impact）

#### 悬挂系统
- 悬挂臂物理模拟
- 轮子旋转与视觉同步

#### 天气系统
- 动态天气切换

#### UI 系统
- 暂停界面、设置界面、HUD 与瞄准镜

### 技术基础设施
- Git LFS 管理大文件（模型/音频/插件）
- HDRP 渲染管线 17.3.0
- UI Toolkit 自定义 Painter2D HUD
- FMOD Studio 音频中间件
- Cinemachine 相机系统
- Input System 输入管理
