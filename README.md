# Archeforge - Unity Edition

A tile-based RPG game built with Unity and C#. Originally a Phaser/TypeScript project, this is the C# port using the Unity game engine.

## Features

- **Tile-based World Generation**: 100x100 procedurally generated world with solid blocks and resources
- **Character Progression**: Affinity system tracking four skill trees (Craft, Melee, Fire, Nature)
- **Inventory System**: Collect and manage resources for crafting
- **Autonomous Agents**: Creature workers that mine resources automatically
- **Enemy AI**: Multiple enemy types that patrol and chase the player
- **Crafting System**: Combine resources to create tools and equipment
- **Persistence**: Save/load game state including inventory and progression

## Project Structure

```
Assets/
├── Scripts/
│   ├── Entities/          # Player, creatures, enemies, NPCs
│   ├── Systems/           # Core game systems (Grid, Inventory, Affinity, etc.)
│   ├── Managers/          # Game manager and control systems
│   ├── UI/               # User interface scripts
│   └── Utilities/        # Helper and utility functions
├── Resources/            # Sprites, sounds, and other assets
├── Prefabs/             # Pre-configured game objects
└── Scenes/              # Game scenes
```

## Controls

- **Arrow Keys**: Move character
- **Left Click**: Mine resources, gain XP
- **Space**: Melee attack nearby enemies
- **F**: Fire action
- **R**: Nature action
- **E**: Archetype ability (unlocks as you level up)
- **C**: Open crafting menu

## Game Systems

### GridSystem
Manages the tile-based world with collision detection and resource placement.

### AffinitySystem
Tracks player progression across four skill trees:
- **Craft**: Resource gathering and item creation
- **Melee**: Physical combat
- **Fire**: Offensive magic and abilities
- **Nature**: Healing and environmental manipulation

### InventorySystem
Handles player inventory with stacking items and quantity management.

### CraftingSystem
Define and manage recipes for combining resources into new items.

### PersistenceSystem
Save game state including position, inventory, progression, and enemy states.

## Building & Running

### Requirements
- Unity 2022.3 LTS or newer
- C# 9.0 or newer

### Setup
1. Clone the repository
2. Open the project in Unity Hub
3. Open the main scene from `Assets/Scenes/Main.unity`
4. Press Play to run

### Building
- Go to `File > Build Settings`
- Select your target platform
- Click `Build`

## Development

### Adding New Enemies
1. Create a new script extending the `Enemy` class
2. Customize behavior in `Update()` and add attack logic
3. Instantiate in `GameManager.cs`

### Adding New Recipes
1. Edit `CraftingSystem.cs` `InitializeRecipes()` method
2. Create a new `CraftingRecipe` and add ingredients
3. Set affinity level requirements

### Extending the Grid
Modify `GridSystem.TileSize`, `Width`, and `Height` to change world size.

## Game Design

### Progression
Players progress by gaining XP through various actions:
- Mining blocks increases Craft XP
- Defeating enemies increases Melee XP
- Using fire abilities increases Fire XP
- Gathering resources increases Nature XP

### Archetypes
Reaching specific affinity levels unlocks character archetypes with unique abilities:
- **Novice** (default)
- **Necromancer** (1+ Fire)
- **Rune Smith** (2+ Craft)
- **Battle Smith** (2+ Melee, <Craft)
- **Pyromancer** (2+ Fire, <Melee)
- **Naturalist** (2+ Nature)

## Performance

- Camera culling for visible tile rendering only
- Spatial partitioning for enemy detection
- Object pooling for frequently spawned particles/effects

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see LICENSE file for details.

## Original Project

Based on Archeforge, originally developed in TypeScript using Phaser 3.

## Roadmap

- [ ] Complete UI system with menus and HUD
- [ ] Sound effects and music
- [ ] Multiple dungeon levels
- [ ] Boss enemies
- [ ] Multiplayer support
- [ ] Mobile port
- [ ] Advanced particle effects
- [ ] Dynamic quest system

## Author

Converted to Unity/C# by GitHub Copilot
Original Phaser version by [Original Author]

---

For more information, visit the project repository!
