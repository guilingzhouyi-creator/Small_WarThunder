# 《东七三》Small_WarThunder

> Unity HDRP 坦克载具模拟项目 — 覆盖移动/炮塔/火控/装填/碰撞/悬挂/AI/任务/地图/天气 的完整战斗模拟。

[📋 开发日志](./DEVLOG.md) · [🗺️ 路线图](./ROADMAP.md) · [📝 更新日志](./CHANGELOG.md)

**版本**: `v0.1.002-beta` | **协议**: MIT
[📥 下载最新 Release](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/latest)

**引擎**: Unity 6000.3.11f1 · **渲染管线**: HDRP 17.3.0 · **音频**: FMOD Studio
**仓库**: [github.com/guilingzhouyi-creator/Small_WarThunder](https://github.com/guilingzhouyi-creator/Small_WarThunder)

---

## 项目概况

《东七三》是一个坦克战斗模拟项目，围绕完整的载具体验闭环构建：车体移动/转向、炮塔旋转/俯仰、开火/装填/弹药切换、实时 FCS 火控 HUD、碰撞命中/伤害结算。

架构采用 **ScriptableObject 数据驱动**，UI 基于 **UI Toolkit** 自定义渲染 + UGUI 混合框架，场景通过多层注册与流式加载管理。

### 已实现系统

| 系统 | 状态 | 说明 |
|------|------|------|
| 坦克移动 | ✅ | 尼基金转向模型 + 功率预算分配 + 各向异性地面摩擦 + 状态机 |
| 炮塔控制 | ✅ | TPS/AIM 双模式 + 自由视角(C键) + 炮管碰撞规避 |
| 开火与装填 | ✅ | 弹药切换 + 装填计时 + 激光测距 + 弹道计算 |
| FCS 火控 HUD | ✅ | 自定义布局 + FOV 缩放 + 准星/刻度/读数框 |
| 碰撞与伤害 | ✅ | 装甲区域 + 穿透/跳弹判定 + 伤害结算 |
| 对象池 | ✅ | 弹药池化全生命周期管理 |
| 音频系统 | ✅ | FMOD 引擎状态机 + 一次性音效 + 音量分类 |
| 悬挂系统 | ✅ | 悬挂臂物理 + 轮子旋转视觉同步 |
| UI 系统 | ✅ | 暂停/设置/HUD/瞄准镜 + 栈式导航 + 混合框架 |
| 天气系统 | ✅ | 动态天气切换 |
| AI 敌方坦克 | ✅ | 行为树 + 移动/炮塔/武器控制 + 感知/警觉度 |
| 地图系统 | ✅ | 双层注册 + 流式加载 + UI Toolkit 渲染 |
| 任务系统 | ✅ | 目标/进度/叙事 + 字幕引擎 + CG 播放 |
| 指示器系统 | ✅ | 接口化指示器管理 |
| 设置系统 | ✅ | 图形/音频/按键绑定 + 持久化 |
| 场景管理 | ✅ | 多场景切换 + 加载界面 + 场景目录 |
| 全局服务 | ✅ | 时间管理 + 光标锁定 + 常量库 + 对象池 |
| 数据库架构 | ✅ | 二进制流数据持久化 + 重载读取 + 索引/规则配置 |
| 玩家侦查与感知 | ✅ | 事件总线 + 状态管理 + 缓存 + SO 配置 |
| 敌方高亮系统 | ✅ | 视图适配 + 地图适配 + 追踪桥接 |
| 全局单位追踪 | ✅ | 框架搭建完成 |

### 下一里程碑 — v0.2.000

- 任务系统完善（目标链/条件判定/反馈）
- 多坦克支持（选择/切换/不同数据配置）
- 伤害视觉反馈（破损/起火/爆炸 VFX）
- 小地图与战术标记
- 设置持久化完善
- 数据库架构完善
- 玩家侦查与感知系统完善
- 敌方高亮系统完善
- 全局单位追踪完善

---

## 项目结构

```
Assets/
├── Scripts/                    # 运行时 C# 脚本
│   ├── Tank/                   # 坦克系统（移动/开火/炮塔/悬挂/相机/碰撞/UI）
│   ├── AI/                     # AI 系统（行为/控制/感知/悬挂）
│   ├── Combat/                 # 战斗系统（伤害计算）
│   ├── MapSystem/              # 地图系统（渲染/相机/配置）
│   ├── Mission/                # 任务系统（管理/叙事/验证/分发）
│   ├── Player/                 # 玩家系统（出生/相机）
│   ├── Core/                   # 框架基础设施
│   │   ├── GameManager/        # 游戏主管理器
│   │   ├── Input/              # 输入系统
│   │   ├── Engine/             # 全局引擎（FCS/光标/流式加载）
│   │   ├── Scene/              # 场景切换
│   │   ├── Subtitle/           # 字幕引擎
│   │   ├── Registry/           # 注册表
│   │   ├── Constants/          # 全局常量
│   │   ├── ObjectPool/         # 对象池
│   │   ├── Persistence/        # 持久化
│   │   └── Marker/             # 标记组件
│   ├── UI/                     # 全局 UI
│   │   ├── HUD/                # 战斗 HUD
│   │   ├── NewUIFramework/     # 混合 UI 框架
│   │   └── SettingManager/     # 设置管理
│   ├── Audio/Manager/          # 音频管理
│   ├── Weather/                # 天气系统
│   ├── Indicator/              # 指示器
│   ├── PlayerPerception/       # 玩家侦查感知
│   └── EnemyHighlightSystem/   # 敌方高亮
├── GameData/                   # ScriptableObject 数据配置
│   ├── TankConfigs/            # 坦克参数（移动/武器/炮塔/弹药/装甲/音频）
│   ├── AIConfigs/              # AI 配置
│   ├── AimConfigs/             # 瞄准配置
│   ├── CameraConfigs/          # 摄像机配置
│   ├── InputConfigs/           # 输入/按键配置
│   ├── MapConfigs/             # 地图配置
│   ├── MissionConfigs/         # 任务配置
│   ├── TaskSystemData/         # 任务系统数据
│   ├── IndicatorConfigs/       # 指示器配置
│   ├── DatabaseSystem/         # 数据库结构配置
│   ├── PlayerPerceptionSystem/ # 感知系统配置
│   ├── EnemyHighlightSystem/   # 高亮系统配置
│   └── GlobalUnitTrackingSystem/ # 单位追踪配置
├── Levels/                     # 场景文件
├── Art/                        # 美术资产（模型/贴图/字体）
├── Audio/                      # 音频资产（FMOD Bank / BGM）
├── prefabs/                    # 预制体（坦克/UI/场景/弹药/天气）
├── Editor/                     # 编辑器工具
└── Resources/                  # 动态加载资源
```

完整目录索引见 [MIGRATION_PLAN.md](./Assets/MIGRATION_PLAN.md)。

---

## 数据驱动设计

所有坦克参数通过 ScriptableObject 配置：

| SO 资产 | 参数 |
|----------|------|
| `TankMoveData` | 质量/速度/加速度/功率/调校曲线 |
| `TankTurretData` | 旋转速度/俯仰限制/炮管碰撞 |
| `TankWeaponData` | 武器参数 |
| `ProjectileData` | 弹丸参数 |
| `ArmoredZoneData` | 装甲区域定义 |
| `TankAudioData` | FMOD 事件/引擎状态层 |
| `NewAimConfigData` | HUD 布局/元素/变焦 |

### 设计模式

| 模式 | 用途 |
|------|------|
| 单例 | 全局服务（GameManager/AudioManager/TimeManager等） |
| Partial Class | 大控制器分解（TankMoveController/TankFireController等） |
| 中介者 | MIddleInputingController 输入分发 |
| 观察者 | C# event / Action 委托 |
| 状态机 | 转向/引擎音频 |
| 注册表 | FCSRegistrySystem / LevelRegistrySystem |
| 策略 | 转向策略 |
| 数据驱动 | ScriptableObject 配置 |
| 对象池 | 弹药池化 |
| 事件总线 | PlayerPerceptionEventBus |

---

## 依赖与第三方资产

| 资产 | 用途 | 协议 |
|------|------|------|
| Unity HDRP 17.3.0 | 渲染管线 | Unity Companion License |
| FMOD Studio | 音频中间件 | FMOD EULA |
| 思源黑体 | UI 字体 | SIL OFL-1.1 |
| VolumetricFog2 | 体积雾效 | Asset Store EULA |

---

## 快速开始

### 环境要求

- **Unity 6000.3.11f1** (LTS)
- **Git LFS** (`git lfs --version`)
- **Windows 11** (主要开发平台)

### 克隆与设置

```bash
git clone git@github.com:guilingzhouyi-creator/Small_WarThunder.git
cd Small_WarThunder
git lfs pull
```

在 Unity Hub 中打开项目，等待 HDRP 着色器编译完成。

### 构建资源包

Unity Editor 中: `Tools → 构建资源资产包`
在项目根目录生成 `Small_WarThunder_Assets_v{version}.zip`。

---

## 贡献

欢迎提交 Issue 或 Pull Request。

1. 遵循现有代码规范（`_camelCase` 私有字段、`PascalCase` 公共属性）
2. 遵守对象池生命周期 — 禁止对池化对象单独调用 `SetActive(false)`
3. UI 修改需同时考虑事件订阅与状态刷新
4. 重大改动先在 Issue 中讨论

---

## 协议

本项目采用 **MIT License**。详见 [LICENSE](LICENSE)。

第三方资产保留其原始协议 — 见 [依赖与第三方资产](#依赖与第三方资产)。

---

## 作者

- **guilingzhouyi-creator** — 项目发起人、主开发者、架构设计

> **构建要求**: Unity 6000.3.11f1 + HDRP 17.3.0
