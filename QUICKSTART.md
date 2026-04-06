# Archeforge Unity - Quick Start Guide

## What's Included

This Unity C# port of Archeforge includes:

```
archeforge-unity/
├── Assets/
│   └── Scripts/
│       ├── Entities/
│       │   ├── Player.cs
│       │   ├── Creature.cs
│       │   ├── Collector.cs
│       │   ├── Enemy.cs
│       │   └── Guardian.cs
│       ├── Systems/
│       │   ├── GridSystem.cs
│       │   ├── AffinitySystem.cs
│       │   ├── InventorySystem.cs
│       │   ├── CraftingSystem.cs
│       │   └── PersistenceSystem.cs
│       └── Managers/
│           └── GameManager.cs
├── README.md                    # Full project documentation
├── LICENSE                      # MIT License
├── .gitignore                  # Git ignore rules
├── MIGRATION_NOTES.md          # TypeScript → C# conversion notes
└── PUSH_TO_GITHUB.md           # Pushing to GitHub guide
```

## Next Steps

### 1. Set Up Git & Push to GitHub
```powershell
# Navigate to project directory
cd c:\workspace\archeforge-unity

# Initialize git repo
git init
git add .
git commit -m "Initial commit: Archeforge Unity C# port"

# Create repo on GitHub at https://github.com/new
# Then connect and push:
git remote add origin https://github.com/YOUR-USERNAME/archeforge-unity.git
git branch -M main
git push -u origin main
```

**Or use GitHub CLI for one-command setup:**
```powershell
gh repo create archeforge-unity --source=. --public --remote=origin --push
```

See `PUSH_TO_GITHUB.md` for detailed instructions.

### 2. Open in Unity

1. Open Unity Hub
2. Click "Add Project from disk" → Select `c:\workspace\archeforge-unity`
3. Create a new 2D scene and add GameManager.cs as a component
4. Press Play to run

### 3. Review & Understand Code

- **MIGRATION_NOTES.md**: Learn how the code was converted from TypeScript
- **README.md**: Full documentation and game design
- Review each system in `Assets/Scripts/Systems/`

## Key Differences from Original

| Feature | Phaser/TS | Unity/C# |
|---------|-----------|---------|
| Engine | Phaser 3 | Unity 2022+ |
| Language | TypeScript | C# |
| Rendering | WebGL | Native |
| Platform | Web Browser | Windows/Mac/Linux/Mobile |
| Physics | Optional | Built-in (Rigidbody2D) |
| Input | Phaser Events | Input class |
| Time | Milliseconds | Seconds |

See `MIGRATION_NOTES.md` for detailed differences.

## Common Tasks

### Adding a New Item Type

1. Open `Assets/Scripts/Systems/CraftingSystem.cs`
2. Add to `InitializeRecipes()`:
```csharp
CraftingRecipe diamondPickaxe = new CraftingRecipe("diamond_pickaxe", "Diamond Pickaxe", 3, "craft", 2);
diamondPickaxe.Ingredients.Add(new IngredientRequirement("diamond", 2));
recipes.Add(diamondPickaxe);
```

### Adding a New Enemy Type

1. Open `Assets/Scripts/Entities/Enemy.cs`
2. Add to the `EnemyType` enum:
```csharp
public enum EnemyType { Goblin, Orc, Skeleton, Dragon } // ← Add Dragon
```

3. Update type selection logic if needed

### Creating a Custom Ability

1. Open `Assets/Scripts/Systems/AffinitySystem.cs`
2. Add to `GetArchetypeAbility()` switch statement
3. Implement in `GameManager.HandleArchetypeAbility()`

## Project Statistics

- **Total Lines of Code**: ~2,000+
- **Scripts**: 12 C# classes
- **Systems**: 5 core systems
- **Entities**: 5 entity types
- **Game Features**: Progression, Inventory, Crafting, Persistence

## What Still Needs Implementation

1. **Graphics**
   - Replace colored sprites with actual art
   - Add animations
   - Particle effects

2. **UI**
   - Game menus
   - Inventory display
   - Crafting UI
   - Health bars

3. **Audio**
   - Background music
   - Sound effects
   - UI feedback sounds

4. **Content**
   - More enemy types/bosses
   - Dungeon levels
   - Quest system
   - More items/recipes

## Support & Resources

- **Unity Documentation**: https://docs.unity3d.com
- **C# Guide**: https://docs.microsoft.com/dotnet/csharp
- **Game Design**: See README.md
- **Migration Help**: See MIGRATION_NOTES.md
- **GitHub Help**: See PUSH_TO_GITHUB.md

## License

MIT License - See LICENSE file for details

---

**Ready to push to GitHub?** Follow the instructions in `PUSH_TO_GITHUB.md`

**Questions about the code?** Check `MIGRATION_NOTES.md` for TypeScript → C# conversions

**Need documentation?** See `README.md` for full game design

Happy developing! 🎮
