# Archeforge Unity Setup Guide

## Quick Start

1. **Open Unity Hub**
2. **Click "Open" → "Add project from disk"**
3. **Navigate to:** `C:\workspace\archeforge-unity`
4. **Click "Select Folder"**
5. **Wait for Unity to import the project**
6. **Open the MainScene from Assets/Scenes/**
7. **Press Play to test the game!**

## Project Structure

```
Assets/
├── Scripts/           # All C# game logic
│   ├── Archeforge.asmdef    # Assembly definition
│   ├── Entities/            # Player, enemies, NPCs
│   ├── Systems/             # Game systems (Grid, Inventory, etc.)
│   └── Managers/            # GameManager coordinator
├── Scenes/            # Unity scenes
│   └── MainScene.unity      # Main game scene
└── Resources/         # (Add sprites/sounds here later)
```

## Game Controls

- **Arrow Keys**: Move player
- **Left Click**: Mine resources
- **Space**: Melee attack
- **F**: Fire action
- **R**: Nature action
- **E**: Archetype ability
- **C**: Open crafting menu

## Adding Sprites

1. Create `Assets/Resources/` folder
2. Add your sprite files (PNG/SVG)
3. Update the sprite references in the entity scripts

## Next Steps

1. **Test the game** - Press Play and try the controls
2. **Add sprites** - Replace colored rectangles with actual art
3. **Add UI** - Create menus and HUD elements
4. **Add audio** - Background music and sound effects
5. **Build the game** - Export to Windows/Mac/WebGL

## Troubleshooting

### "Script can't be loaded" errors
- Make sure all scripts are in the Assets/Scripts/ folder
- Check that Archeforge.asmdef exists

### Game doesn't start
- Ensure GameManager component is attached to a GameObject in the scene
- Check the Console for error messages

### No sprites visible
- Add sprite files to Assets/Resources/
- Update sprite loading code in entity scripts

## Documentation

- **Full Game Docs**: See README.md in project root
- **Code Migration**: See MIGRATION_NOTES.md
- **Setup Help**: See QUICKSTART.md

---

**Ready to play?** Open Unity and press Play! 🎮