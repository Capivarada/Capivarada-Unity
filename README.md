# 🎮 Rivals Arena - Arena das Almas MVP

> An offline MVP for a competitive multiplayer game in Unity 6, validating core gameplay without UI, multiplayer, or complex systems.

## 📋 Table of Contents
- [Quick Start](#quick-start)
- [Features](#features)
- [Project Structure](#project-structure)
- [Controls](#controls)
- [Architecture](#architecture)
- [Next Steps](#next-steps)
- [Documentation](#documentation)

## 🚀 Quick Start

### Prerequisites
- Unity 6 LTS or later
- ~500MB disk space

### Setup (5 minutes)

1. **Configure Project**
   ```
   Menu: ArenadasAlmas → Setup → Configure Project
   ```

2. **Create Prefabs**
   ```
   Menu: ArenadasAlmas → Create Prefabs → All
   ```

3. **Setup Test Scene**
   - Open `Assets/Scenes/MVPTestScene.unity`
   - Menu: `ArenadasAlmas → Setup → Create Test Scene Instances`
   - Save (Ctrl+S)

4. **Play!**
   - Click Play button
   - Move with WASD
   - Dash with Space
   - Attack with Left Click
   - Defend with Right Click

## ✨ Features

### Player Mechanics
- **Movement**: WASD + Dash (Space)
- **Combat**:
  - Garra Espiritual (Left Click) - Soul extraction attack
  - Defesa Espiritual (Right Click) - Shield generation
  - Funil Espiritual (Q) - Placeholder ability
- **Stats**: Health, souls, configurable attributes

### Systems
- **Buff System**: Generic, stackable, duration-based
- **Event System**: Loose coupling via events
- **Configuration**: All values are tunable

### Architecture
- Event-driven
- Component-based
- Multiplayer-ready
- Zero hardcoded values

## 🎮 Controls

| Key | Action |
|-----|--------|
| W/A/S/D | Move |
| Space | Dash |
| Left Click | Attack |
| Right Click | Shield |
| Q | Soul Funnel (placeholder) |
| ESC | Pause |

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Data/                  # Types & enums
│   ├── Systems/               # Core systems
│   ├── Player/                # Player components
│   └── Enemy/                 # Enemy components
├── Prefabs/                   # Reusable prefabs
├── ScriptableObjects/         # Configuration
│   ├── Souls/
│   ├── Attacks/
│   └── Buffs/
└── Scenes/
    └── MVPTestScene.unity
```

## 🏗️ Architecture

### Key Systems

| System | Purpose |
|--------|---------|
| PlayerMovement | Input & movement physics |
| PlayerStats | Health, souls, state |
| PlayerCombat | Attacks & abilities |
| PlayerConfig | Centralized attributes |
| BuffController | Generic buff system |
| EnemyDummy | Test enemy |

### Design Principles
- ✅ Event-driven (loose coupling)
- ✅ Component-based (single responsibility)
- ✅ Configuration-driven (no hardcoded values)
- ✅ Data-driven (ScriptableObjects)
- ✅ Multiplayer-ready (no singletons)

## 📊 Configuration

All values are tunable via Inspector:

**Player Attributes**
- moveSpeed: 5 units/sec
- dashForce: 20 acceleration
- dashCooldown: 1 second
- dashDuration: 0.2 seconds
- clawRange: 15 units
- clawCooldown: 0.5 seconds
- defenseDuration: 3 seconds
- defenseCooldown: 5 seconds

**Enemy Attributes**
- maxHealth: 50
- maxSouls: 3

## 📚 Documentation

### Getting Started
- **[MVP_SUMMARY.txt](MVP_SUMMARY.txt)** - Complete overview
- **[SETUP_GUIDE.md](Assets/SETUP_GUIDE.md)** - Step-by-step setup & troubleshooting

### For Developers
- **[ARCHITECTURE.md](Assets/Scripts/ARCHITECTURE.md)** - Design patterns & extension points
- **[README_MVP.md](Assets/Scripts/README_MVP.md)** - Feature overview

### Project
- **[CHANGELOG.md](CHANGELOG.md)** - Version history & roadmap

## 🔧 Extending the MVP

### Add New Ability
1. Create logic in `PlayerCombat`
2. Add keybind in `Update()`
3. Broadcast event
4. Done!

### Add New Buff
1. Add to `BuffType` enum
2. Add case in `PlayerConfig.ApplyBuffModifier()`
3. Create `BuffData` ScriptableObject
4. Done! (No changes to `BuffController` needed)

### Add AI
1. Create `EnemyAI` component
2. Add pathfinding + combat logic
3. Subscribe to player events
4. Done!

### Add Multiplayer
1. Create `NetworkManager`
2. Subscribe to state events
3. Serialize to network
4. Validate on server
5. Done! (Architecture supports it)

## 📈 Performance

**Targets**
- 60+ FPS on mid-range devices
- < 100MB memory
- < 2 second load time

**Not Yet Optimized** (ready for v0.2)
- Object pooling
- LOD system
- Spatial optimization

## 🧪 Testing

Run through the [Testing Checklist](Assets/SETUP_GUIDE.md#testing-checklist) to validate:
- Movement ✓
- Dash ✓
- Attack ✓
- Shield ✓
- Enemy death ✓

## 🐛 Troubleshooting

**Can't move?**
- Verify `PlayerMovement` component exists
- Check `CharacterController` is present

**Enemy doesn't spawn?**
- Verify `Enemy` layer was created
- Check `EnemyDummy.prefab` exists

**Attack doesn't work?**
- Verify `Enemy` layer is set correctly
- Check `LineRenderer` is in Player prefab

See [Full Troubleshooting](Assets/SETUP_GUIDE.md#troubleshooting)

## 🗺️ Roadmap

- **v0.2**: AI System
- **v0.3**: Soul System (Brazilian folklore)
- **v0.4**: Visual Polish & UI
- **v0.5**: Progression System
- **v1.0**: Multiplayer

See [CHANGELOG.md](CHANGELOG.md) for details.

## 📝 Project Stats

- **16 C# scripts** (~2000 lines)
- **1 test scene**
- **4 prefabs** (auto-generated)
- **5 documentation files** (~1000 lines)
- **0 external dependencies** (Pure Unity)

## ✅ Features Implemented

### Player System
- [x] WASD movement
- [x] Space dash with cooldown
- [x] Rotation system
- [x] Gravity & ground detection
- [x] Health system
- [x] Soul system
- [x] State management

### Combat
- [x] Garra Espiritual attack
- [x] Soul extraction mechanic
- [x] Defesa Espiritual shield
- [x] Ability cooldowns
- [x] Attack range detection

### Systems
- [x] Generic buff system
- [x] Event broadcasting
- [x] Centralized configuration
- [x] ScriptableObject data

### Editor Tools
- [x] Automatic prefab creation
- [x] Layer/tag configuration
- [x] Scene population helper
- [x] Example assets generator

## 🎯 MVP Complete!

This MVP successfully validates:
- ✅ Core gameplay loop
- ✅ Combat mechanics
- ✅ Buff system
- ✅ Event architecture
- ✅ Multiplayer readiness

**Ready for post-MVP development!**

## 📞 Support

For questions:
1. Check [SETUP_GUIDE.md](Assets/SETUP_GUIDE.md)
2. Review [ARCHITECTURE.md](Assets/Scripts/ARCHITECTURE.md)
3. See script comments and inline documentation

## 📄 License

TBD

---

**Made with ❤️ for Arena das Almas**

Built with Unity 6 | MVP v0.1.0 | Ready for Multiplayer
