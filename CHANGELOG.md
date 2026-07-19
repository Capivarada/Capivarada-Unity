# Arena das Almas - MVP Changelog

## Version 0.1.0 - MVP Foundation (2026-07-19)

### ✨ Features Added

#### Player System
- **PlayerMovement**: WASD movement, rotation, gravity, configurable dash
- **PlayerStats**: Health/souls tracking with events
- **PlayerCombat**: Garra Espiritual attack, Defesa Espiritual shield, Funil Espiritual placeholder
- **PlayerConfig**: Centralized attribute management with buff support

#### Gameplay Mechanics
- **Garra Espiritual** (Left Click): Soul extraction attack with visual connection
- **Defesa Espiritual** (Right Click): Shield generation with configurable duration
- **Funil Espiritual** (Q): Placeholder for future ability
- **Dash** (Space): High-speed forward movement with cooldown

#### Systems
- **BuffController**: Generic buff system with stacking and duration
- **EnemyDummy**: Test enemy with health and soul system
- **Event System**: Comprehensive event broadcasting for all state changes

#### Data & Configuration
- **SoulData**: ScriptableObject for soul configuration
- **AttackData**: ScriptableObject for attack configuration
- **BuffData**: ScriptableObject for buff configuration
- **BuffType Enum**: All modifiable attributes enumerated
- **PlayerState Enum**: Player state management

#### Editor Tools
- **PrefabFactory**: Menu commands to create all prefabs
- **ProjectSetup**: Menu commands to configure layers and tags
- **SceneSetup**: Menu commands to populate test scene
- **ExampleConfigurations**: Sample ScriptableObjects for reference

#### Documentation
- **README_MVP.md**: Quick start and feature overview
- **SETUP_GUIDE.md**: Step-by-step setup and tuning instructions
- **ARCHITECTURE.md**: Design patterns and extension points
- **CHANGELOG.md**: Version history and future roadmap

### 📁 Project Structure
```
Assets/
├── Scripts/
│   ├── Data/                    # Enums and types
│   ├── Systems/                 # Core systems
│   ├── Player/                  # Player components
│   └── Enemy/                   # Enemy components
├── Prefabs/                     # Reusable prefabs
├── ScriptableObjects/           # Configuration assets
│   ├── Souls/
│   ├── Attacks/
│   └── Buffs/
└── Scenes/                      # Game scenes
    └── MVPTestScene.unity
```

### 🎮 Controls
| Input | Action |
|-------|--------|
| W/A/S/D | Move |
| Space | Dash |
| Left Click | Attack (Garra Espiritual) |
| Right Click | Defend (Defesa Espiritual) |
| Q | Soul Funnel (Placeholder) |
| ESC | Pause |

### 🏗️ Architecture Highlights
- ✅ Event-driven architecture
- ✅ Configuration-driven design
- ✅ Component-based systems
- ✅ Zero hardcoded values
- ✅ Multiplayer-ready structure
- ✅ Generic buff system
- ✅ ScriptableObject configuration

### 📊 Performance Targets
- FPS: 60+ on mid-range devices
- Memory: < 100MB
- Load Time: < 2 seconds

### 🔄 Next Steps
- [ ] AI System (Replace EnemyDummy with intelligent bots)
- [ ] Soul System (Brazilian folklore souls with abilities)
- [ ] Visual Polish (VFX, UI, sound)
- [ ] Progression (Levels, items, upgrades)
- [ ] Multiplayer (Network synchronization)

### 🐛 Known Issues
- Shield doesn't have physics collision (visual only)
- No VFX/particles yet (placeholder effects)
- No sound effects
- No AI in dummy enemy
- Garra Espiritual has fixed 2-second extraction time

### 📝 Testing Checklist
- [x] Movement works with WASD
- [x] Dash has cooldown
- [x] Attack connects with enemies
- [x] Shield appears and disappears
- [x] Enemy loses souls
- [x] Enemy dies when out of souls
- [x] No console errors

### 👥 Contributors
- MVP Architecture: Copilot
- Project Setup: Arena das Almas Team

### 📄 License
TBD

---

## Future Versions (Roadmap)

### v0.2.0 - AI System
- [ ] Basic pathfinding for enemies
- [ ] Combat behavior tree
- [ ] Enemy ability system

### v0.3.0 - Soul System
- [ ] Curupira soul (speed-based)
- [ ] Iara soul (water-based)
- [ ] Boto soul (deception-based)
- [ ] Passive ability system

### v0.4.0 - Polish & UI
- [ ] Health bar system
- [ ] Soul counter UI
- [ ] Ability cooldown indicators
- [ ] VFX system
- [ ] Sound effects

### v0.5.0 - Progression
- [ ] Level system
- [ ] Item system
- [ ] Upgrade tree
- [ ] Cosmetics

### v1.0.0 - Multiplayer
- [ ] Network synchronization
- [ ] Player lobbies
- [ ] Matchmaking
- [ ] Ranked system

