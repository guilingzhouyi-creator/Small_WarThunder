# Dong Qi San (东七三) — Small_WarThunder

> A Unity-based tank simulation project inspired by modern military vehicle combat games — featuring full tank movement, fire control, loading, collision detection, and HUD systems.

[📋 Dev Log](./DEVLOG.md) · [🗺️ Roadmap](./ROADMAP.md) · [📝 Changelog](./CHANGELOG.md)

**Version**: `v0.1.000-beta` | **License**: MIT  
[📥 Download Latest Release](https://github.com/guilingzhouyi-creator/Small_WarThunder/releases/latest)

**Engine**: Unity 6000.3.11f1 · **Render Pipeline**: HDRP 17.3.0 · **Audio**: FMOD Studio  
**Repository**: [github.com/guilingzhouyi-creator/Small_WarThunder](https://github.com/guilingzhouyi-creator/Small_WarThunder)

---

## Overview

Dong Qi San is a tank combat simulator focusing on a complete gameplay loop: tank movement / turning, turret rotation / elevation, firing / reloading / ammo switching, and real-time FCS HUD rendering. The architecture is **data-driven via ScriptableObjects**, with UI built on **UI Toolkit**.

### Features (v0.1.000-beta)

| System | Status | Description |
|--------|--------|-------------|
| Tank Movement | ✅ | Nikiforov steering model + power budget + anisotropic ground friction |
| Turret Control | ✅ | TPS / AIM dual mode, freelook (C key), barrel collision avoidance |
| Fire & Reload | ✅ | Ammo switching, reload timer, laser rangefinder, ballistics |
| FCS HUD | ✅ | Custom layout, FOV scaling, reticle / scale / readout boxes, fill support |
| Collision & Damage | ✅ | Armor zones, penetration / ricochet, damage resolution |
| Object Pooling | ✅ | Cannonball pooled lifecycle |
| Audio System | ✅ | FMOD engine state machine + one-shot events |
| Suspension | ✅ | Suspension arm physics + wheel rotation visuals |
| Weather | ✅ | Dynamic weather transitions |
| UI System | ✅ | Pause / Settings / HUD / Scope |

### Next Milestone — v0.2.000

- Mission system (objectives, progress tracking)
- Multiple tank support (selection, switching)
- Damage visual feedback (deformation, fire / explosion VFX)
- AI enemy tanks (patrol / search / engage)
- Mini-map & tactical markers
- Settings persistence (graphics / audio / controls)

---

## Dependencies & Third-Party Assets

| Asset | Usage | License |
|-------|-------|---------|
| Unity HDRP 17.3.0 | Rendering pipeline | Unity Companion License |
| FMOD Studio | Audio middleware | FMOD EULA |
| SourceHanSans | UI font (SIL Open Font License) | OFL-1.1 |
| VolumetricFog2 | Volumetric fog VFX (Built-in/URP) | Asset Store EULA |

---

## Getting Started

### Prerequisites

- **Unity 6000.3.11f1** (LTS)
- **Git LFS** installed (`git lfs --version`)
- **Windows 11** (primary development target)

### Clone & Setup

```bash
git clone git@github.com:guilingzhouyi-creator/Small_WarThunder.git
cd Small_WarThunder
git lfs pull
```

Then open the project in Unity Hub and let the HDRP shaders compile.

### Build Asset Bundle (for Release)

In Unity Editor: `Tools → 构建资源资产包`  
Generates `Small_WarThunder_Assets_v{version}.zip` at the project root.

---

## Architecture

```
GameManager                    ← Global lifecycle & state machine
  └─ UIManager                 ← UI management (pause/mission/HUD/settings)
       └─ TankAImUIController  → FcsHudPainter (FCS reticle HUD)
MIddleInputingController       ← Input mediation (InputSystem → game logic)
TankMoveController             ← Tank movement (multiple partial files)
TankWeaponController           ← Turret & weapon control
TankFireController             ← Fire / reload / rangefinding
TankController                 ← Tank integration
AudioManager                   ← FMOD audio
WeatherController              ← Weather
GeneralHitPosition             ← Hit detection & damage
CannonBall + ObjectPool        ← Projectile pooling
```

### Data-Driven Design

All tank parameters are configured via ScriptableObject assets:

| SO Asset | Parameters |
|----------|------------|
| `TankMoveData` | Mass, speed, acceleration, power, tuning curves |
| `TankTurretData` | Rotation speed, elevation limits, barrel collision |
| `TankAudioData` | FMOD events, engine state layers |
| `NewAimConfigData` | HUD layout, elements, zoom |
| `ProjectileData` | Projectile parameters |
| `ArmoredZoneData` | Armor zone definitions |

### Design Patterns

| Pattern | Usage |
|---------|-------|
| Singleton | Most controllers |
| Partial Class | Large controller decomposition |
| Mediator | MIddleInputingController |
| Observer | C# events |
| State Machine | Steering / engine audio |
| Registry | FCSRegistrySystem |
| Strategy | Steering strategies |
| Data-Driven | ScriptableObject configuration |
| Object Pool | Cannonball pooling |

---

## Contributing

Contributions are welcome! Please open an Issue or Pull Request.

1. Follow the existing code conventions ( `_camelCase` private fields, `PascalCase` public properties).
2. Respect the object pooling lifecycle — never use `SetActive(false)` alone on pooled objects.
3. UI changes must consider both event subscriptions and state refresh.
4. Discuss major changes in an Issue first.

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for the full text.

Third-party assets retain their original licenses — see [Dependencies](#dependencies--third-party-assets) above.

---

## Author

- **guilingzhouyi-creator** — Project initiator, lead developer, architecture design

> **Build Requirements**: Unity 6000.3.11f1 + HDRP 17.3.0  

---

## 资源资产包

项目的大文件资源（模型、音频、贴图、FMOD 插件等）通过 **Git LFS** 管理，同时提供独立的 ZIP 资源包用于 Release 分发。详见上方 Clone & Setup。
