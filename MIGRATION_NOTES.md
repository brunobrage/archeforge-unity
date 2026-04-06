# Migration Notes: Phaser/TypeScript → Unity/C#

This document explains the changes made when converting Archeforge from Phaser (TypeScript) to Unity (C#).

## Architecture Changes

### Rendering System
**Phaser (TypeScript):**
- Used Phaser's Graphics object for tile rendering
- Camera viewport culling handled by Phaser
- WebGL/Canvas rendering

**Unity (C#):**
- Uses Unity's sprite rendering system
- Gameobject-based tile representation (can be optimized with chunk systems)
- Render pipeline via Unity's rendering engine

### Game Loop
**Phaser:**
```typescript
// Phaser handles the game loop automatically
update(_: number, delta: number) {
    // Called once per frame
}
```

**Unity:**
```csharp
// Unity calls these built-in methods
void Update() {
    // Called once per frame
}

void LateUpdate() {
    // Called after all Update calls
}

void FixedUpdate() {
    // Called at fixed timestep for physics
}
```

## File Structure Comparison

### Project Layout
```
Phaser/TypeScript:
src/
  entities/
  scenes/
  systems/

Unity:
Assets/Scripts/
  Entities/
  Managers/
  Systems/
  UI/
  Utilities/
```

## Class Conversions

### GridSystem
- **Phaser**: Separate class without MonoBehaviour
- **Unity**: Extends MonoBehaviour; manages grid initialization

### Player
- **Phaser**: Extends `Phaser.GameObjects.Sprite`
- **Unity**: Extends `MonoBehaviour` with SpriteRenderer component

### Movement
- **Phaser**: Direct position manipulation
- **Unity**: Uses Rigidbody2D with velocity or transform position

### Input Handling
- **Phaser**: Integrated in scene (`this.input`)
- **Unity**: `Input` class; polling in Update()

## Key System Differences

### Coordinate System
- **Phaser**: Origin top-left, Y increases downward
- **Unity**: Origin center, Y increases upward
  - **Migration**: Negate Y coordinates or adjust world generation

### Time
- **Phaser**: Delta time in milliseconds
- **Unity**: Delta time in seconds (Time.deltaTime)
  - **Migration**: Divide by 1000 or adjust calculations

### 2D Physics
- **Phaser**: Built-in physics engine optional
- **Unity**: Rigidbody2D component required for physics

### Entity Finding
- **Phaser**: Searched manually in scene lists
- **Unity**: `FindObjectOfType<>()` method

## Code Examples

### Finding an Enemy (Phaser vs Unity)

**Phaser TypeScript:**
```typescript
private findClosestEnemyAroundPlayer() {
    let closest: Enemy | null = null;
    let closestDistance = Phaser.Math.Distance.Between(
        this.player.x, this.player.y, closest.x, closest.y
    );
    
    for (const enemy of this.enemies) {
        const distance = Phaser.Math.Distance.Between(
            this.player.x, this.player.y, enemy.x, enemy.y
        );
        if (distance < closestDistance) {
            closest = enemy;
            closestDistance = distance;
        }
    }
    return closest;
}
```

**Unity C#:**
```csharp
private Enemy FindClosestEnemy()
{
    Enemy closest = null;
    float closestDistance = float.MaxValue;

    foreach (Enemy enemy in enemies)
    {
        if (!enemy.IsAlive()) continue;
        
        float distance = Vector3.Distance(
            player.transform.position, 
            enemy.transform.position
        );
        
        if (distance < closestDistance)
        {
            closestDistance = distance;
            closest = enemy;
        }
    }

    return closest;
}
```

### Movement

**Phaser:**
```typescript
this.x += Math.cos(angle) * velocity;
this.y += Math.sin(angle) * velocity;
```

**Unity:**
```csharp
Vector2 direction = diff.normalized;
transform.position = (Vector3)((Vector2)transform.position + direction * velocity);
// Or with Rigidbody2D:
rb.velocity = direction * velocity;
```

## Missing Features (To Implement)

1. **Graphics Rendering**
   - Unity version uses solid-color sprites
   - Original used vector graphics
   - Can be enhanced with:
     - Sprite atlases
     - Tilemap system
     - Mesh-based rendering

2. **UI System**
   - TypeScript used Phaser text objects
   - Unity should use Canvas + Text components
   - Recommend: TextMeshPro for better text rendering

3. **Effect Particles**
   - Phaser: Particle emitters
   - Unity: Particle System component needed

4. **Audio**
   - Not implemented in either yet
   - Unity: Use AudioSource component

## Performance Considerations

### Phaser/TypeScript
- Browser-based optimization
- Hardware acceleration via WebGL
- Asset loading via URL

### Unity/C#
- Native application performance
- Built-in memory management (GC)
- Asset management via AssetDatabase
- **Optimization tips:**
  - Use sprite batching
  - Implement object pooling
  - Use spatial partitioning for enemy detection
  - Chunk-based terrain rendering

## Data Persistence

### Phaser
```typescript
// Used LocalStorage API
localStorage.setItem('gamestate', JSON.stringify(state));
```

### Unity
```csharp
// Uses persistentDataPath
Application.persistentDataPath
JsonUtility.ToJson() / FromJson()
```

## Platform Differences

### Phaser (Web-based)
- Runs in browser
- Cross-platform (Windows, Mac, Linux)
- Deployment: Static file hosting

### Unity (Native)
- Compiled application
- Platform-specific builds (Windows, Mac, Linux)
- Can deploy to: WebGL, Standalone, Mobile, Console

## Enum Usage

### Phaser
```typescript
enum EnemyType {
    Goblin = "goblin",
    Orc = "orc",
    Skeleton = "skeleton"
}
```

### Unity (C#)
```csharp
public enum EnemyType { Goblin, Orc, Skeleton }
// Stored as int; can add attributes for serialization
```

## Future Enhancements

Priority upgrades for Unity version:

1. **Visual Polish**
   - [ ] Animated sprites
   - [ ] Particle effects
   - [ ] Screen shake on impact
   - [ ] Smooth camera transitions

2. **Game Feel**
   - [ ] Sound effects
   - [ ] Background music
   - [ ] UI feedback (buttons, menus)
   - [ ] Controller support

3. **Performance**
   - [ ] Tilemap system instead of individual sprites
   - [ ] Object pooling for enemies
   - [ ] Spatial hashing for collision
   - [ ] Async asset loading

4. **Content**
   - [ ] More enemy types
   - [ ] Boss encounters
   - [ ] Dungeon levels
   - [ ] Quest system

## Debugging

### Phaser
- Browser DevTools (F12)
- Console logging
- Phaser Inspector

### Unity
- Unity Debug panel in editor
- Console window (Ctrl+Shift+C)
- Profiler (Window > Analysis > Profiler)
- VS Code debugger integration

## Learning Resources

- **Unity Manual**: https://docs.unity3d.com/Manual/
- **C# Documentation**: https://docs.microsoft.com/en-us/dotnet/csharp/
- **Unity 2D Games**: https://learn.unity.com/
- **Phaser 3 Docs**: https://photonstorm.github.io/phaser3-docs/ (for reference)

---

**Last Updated**: 2026-04-06
**Version**: 1.0.0 (Initial C# Port)
