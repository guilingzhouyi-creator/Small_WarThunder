# 目录结构迁移计划（War Thunder 风格）

## 目标结构

Assets/
├── Data/              ← 源数据（CSV）
│   └── MissionsCSV/   ← （从 GameData/MissionsCSV 移入）
├── GameData/           ← 运行时 ScriptableObject
│   ├── TankConfigs/    ← （从 TankSO/ 移入）
│   ├── AimConfigs/     ← （从 SOManager/AimConfigSO 移入）
│   ├── MissionConfigs/ ← （从 SOManager/TextShowSO 移入）
│   ├── TimeConfigs/    ← （从 SOManager/TimeSO 移入）
│   └── LevelConfigs/   ← （从 GameData/LevelsData 移入）
├── Levels/             ← 场景文件
│   └── *.unity         ← （从 GameSwitchSceneList 移入）
├── Art/                ← 美术资产（保持）
├── Audio/              ← 音频资产（扩充）
│   └── Bank/           ← （从 Desktop 移入）
├── Prefabs/            ← 预制体（保持）
├── PhysicMaterials/    ← 物理材质（从 PhysicMaterial 重命名）
├── Plugins/            ← 第三方插件
│   └── FMOD/           ← （从 Miscellaneous Management System/Plugins/FMOD 移入）
├── Resources/          ← Resources 加载（保持）
├── Scripts/            ← 代码（按功能域划分）
│   ├── Tank/
│   │   ├── TankerController/     ← (从 Scripts/TankerController 移入)
│   │   ├── FireController/       ← (从 Scripts/TankFireController 移入)
│   │   ├── CannonBall/           ← (从 Scripts/ConnonBall 移入)
│   │   ├── Turret/               ← (从 Scripts/MoveManager/TankTurretMove 移入)
│   │   ├── Movement/             ← (从 Scripts/MoveManager/TankBodyMove 移入)
│   │   ├── Track/                ← (从 Scripts/MoveManager/TankTrackMove 移入)
│   │   ├── Suspension/           ← (从 Scripts/MoveManager/TankTrackMove/SuspenSystem 移入)
│   │   ├── Camera/               ← (从 Scripts/CameraManager 移入坦克相关摄像机)
│   │   ├── Collision/            ← (从 GameMainAllSystem/CollideSystem 移入)
│   │   ├── UI/                   ← (Aim 相关 UI)
│   │   └── ObjectPool/           ← (Tank 弹药池相关)
│   ├── AI/                       ← (从 Scripts/EnemyManager 移入)
│   ├── MapSystem/                ← (从 GameMainAllSystem/MapSystem + MapCamera 等合并)
│   ├── Mission/                  ← (从 Scripts/GameLevelManager + MissionRegistrySystem 等合并)
│   ├── Combat/                   ← 战斗/伤害系统
│   │   └── Damage/               ← (从 GameMainAllSystem/DamageSystem 移入)
│   ├── Player/                   ← (从 Scripts/PlayerManager + GameMainAllSystem/PlayerSystem 合并)
│   ├── Core/                     ← 框架基础设施
│   │   ├── GameManager/          ← (从 Scripts/GameManager 移入)
│   │   ├── Input/                ← (从 GameMainAllSystem/GameInputSystem 移入)
│   │   ├── Time/                 ← (从 GameMainAllSystem/GameTimeManagementSystem 移入)
│   │   ├── Scene/                ← (从 GameMainAllSystem/GameSceneSwitchSystem 移入)
│   │   ├── Engine/               ← (从 GameMainAllSystem/Global EngineService 移入)
│   │   ├── Registry/             ← (从 GameMainAllSystem/CentralRegistrySystem 移入)
│   │   ├── Subtitle/             ← (从 GameMainAllSystem/AutomatedSubtitleEngineSystem 移入)
│   │   ├── ObjectPool/           ← (通用对象池)
│   │   └── CG/                   ← (从 GameMainAllSystem/CGSystem 移入)
│   ├── UI/                       ← 全局 UI（UIManager + PanelControllers）
│   ├── Audio/                    ← 音频管理
│   │   └── Manager/              ← (从 Scripts/AudioManager 移入)
│   └── Weather/                  ← (从 Scripts/WeatherController 移入)
└── Editor/                       ← 编辑器工具（保持）
