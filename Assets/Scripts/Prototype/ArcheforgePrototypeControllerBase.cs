using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Archeforge.UnityPort
{
    public abstract class ArcheforgePrototypeControllerBase : MonoBehaviour
    {
        protected readonly Dictionary<WeaponId, WeaponDefinition> weaponDefinitions = new()
        {
            { WeaponId.RustyDagger, new WeaponDefinition { Id = WeaponId.RustyDagger, Name = "Rusty Dagger", BaseDamage = 5, CritChance = 0.18f, CritMultiplier = 2.1f } },
            { WeaponId.WoodSword, new WeaponDefinition { Id = WeaponId.WoodSword, Name = "Wood Sword", BaseDamage = 8, CritChance = 0.10f, CritMultiplier = 1.8f } },
            { WeaponId.IronSword, new WeaponDefinition { Id = WeaponId.IronSword, Name = "Iron Sword", BaseDamage = 12, CritChance = 0.06f, CritMultiplier = 1.65f } }
        };

        protected readonly List<Recipe> recipes = new()
        {
            new Recipe
            {
                Id = "wood_sword",
                Name = "Wood Sword",
                Ingredients = new List<Ingredient> { new Ingredient { ItemId = "wood", Quantity = 3 } },
                ResultItemId = "wood_sword",
                ResultQuantity = 1,
                RequiredAffinity = AffinityType.Craft,
                RequiredAmount = 10
            },
            new Recipe
            {
                Id = "iron_sword",
                Name = "Iron Sword",
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { ItemId = "iron_ore", Quantity = 2 },
                    new Ingredient { ItemId = "wood", Quantity = 1 }
                },
                ResultItemId = "iron_sword",
                ResultQuantity = 1,
                RequiredAffinity = AffinityType.Craft,
                RequiredAmount = 30
            },
            new Recipe
            {
                Id = "fire_staff",
                Name = "Fire Staff",
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { ItemId = "wood", Quantity = 4 },
                    new Ingredient { ItemId = "iron_ore", Quantity = 1 }
                },
                ResultItemId = "fire_staff",
                ResultQuantity = 1,
                RequiredAffinity = AffinityType.Fire,
                RequiredAmount = 20
            }
        };

        protected readonly List<string> logs = new();
        protected readonly List<InventoryItem> inventory = new();
        protected readonly List<EnemyState> enemies = new();
        protected readonly List<SpawnSlot> spawnSlots = new();
        protected readonly List<WorldDrop> worldDrops = new();
        protected readonly string[] hotbarItemIds = new string[8];

        protected TileType[,] grid = null!;
        protected TileBiome[,] biomeGrid = null!;
        protected TileMaterial[,] materialGrid = null!;
        protected Sprite whiteSprite = null!;
        protected Camera mainCamera = null!;
        protected PlayerState player = null!;
        protected CreatureState creature = null!;
        protected CollectorState collector = null!;
        protected ActorView playerView = null!;
        protected ActorView creatureView = null!;
        protected ActorView collectorView = null!;
        protected TileView[,] tileViews = null!;
        protected Transform tileRoot = null!;
        protected Vector2 playerInput;
        protected Sprite[] playerIdleFrames = Array.Empty<Sprite>();
        protected Sprite[] playerWalkFrames = Array.Empty<Sprite>();
        protected float playerAnimationTimer;
        protected int playerAnimationFrame;
        protected bool inventoryOpen;
        protected bool craftingOpen;
        protected Vector2 inventoryScroll;
        protected Vector2 craftingScroll;
        protected float panelFlashTimer;
        protected Color panelFlashColor = Color.cyan;
        protected int selectedHotbarIndex;

        protected const int GridWidth = 100;
        protected const int GridHeight = 100;
        protected const float TileSize = 1f;
        protected const float PlayerSpeed = 6.5f;
        protected const string SaveKey = "archeforge-unity-prototype-v1";

        protected void GenerateGrid()
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    biomeGrid[x, y] = GetBiomeAt(x, y);
                    bool border = x == 0 || y == 0 || x == GridWidth - 1 || y == GridHeight - 1;
                    float noiseA = Mathf.PerlinNoise(x * 0.11f, y * 0.11f);
                    float noiseB = Mathf.PerlinNoise((x + 41f) * 0.07f, (y + 17f) * 0.07f);
                    bool cluster = noiseA > 0.67f || (noiseB > 0.57f && (x + y) % 5 == 0);
                    bool randomRock = UnityEngine.Random.value < 0.02f + GetBiomeRockBias(biomeGrid[x, y]);
                    grid[x, y] = border || cluster || randomRock ? TileType.Solid : TileType.Empty;
                    materialGrid[x, y] = GetDefaultMaterial(biomeGrid[x, y], grid[x, y]);
                }
            }
        }

        protected TileBiome GetBiomeAt(int x, int y)
        {
            float nx = x / (float)GridWidth;
            float ny = y / (float)GridHeight;
            float blend = Mathf.PerlinNoise(x * 0.045f + 12f, y * 0.045f + 18f);

            if (nx < 0.24f + blend * 0.06f) return TileBiome.Forest;
            if (ny > 0.64f - blend * 0.08f) return TileBiome.Ruins;
            if (nx > 0.72f - blend * 0.07f && ny < 0.58f + blend * 0.06f) return TileBiome.Ember;
            if (nx > 0.48f + blend * 0.03f) return TileBiome.IronWastes;
            return TileBiome.Plains;
        }

        protected float GetBiomeRockBias(TileBiome biome)
        {
            return biome switch
            {
                TileBiome.Forest => 0.02f,
                TileBiome.Ruins => 0.04f,
                TileBiome.IronWastes => 0.06f,
                TileBiome.Ember => 0.08f,
                _ => 0.01f
            };
        }

        protected TileMaterial GetDefaultMaterial(TileBiome biome, TileType tileType)
        {
            return tileType switch
            {
                TileType.Solid => biome switch
                {
                    TileBiome.Forest => TileMaterial.ForestWood,
                    TileBiome.Ruins => TileMaterial.RuinsStone,
                    TileBiome.IronWastes => TileMaterial.IronStone,
                    TileBiome.Ember => TileMaterial.EmberStone,
                    _ => TileMaterial.PlainsStone
                },
                TileType.Resource => biome switch
                {
                    TileBiome.Forest => TileMaterial.ForestResource,
                    TileBiome.Ruins => TileMaterial.RuinsResource,
                    TileBiome.IronWastes => TileMaterial.IronResource,
                    TileBiome.Ember => TileMaterial.EmberResource,
                    _ => TileMaterial.PlainsResource
                },
                _ => biome switch
                {
                    TileBiome.Forest => TileMaterial.ForestGround,
                    TileBiome.Ruins => TileMaterial.RuinsGround,
                    TileBiome.IronWastes => TileMaterial.IronGround,
                    TileBiome.Ember => TileMaterial.EmberGround,
                    _ => TileMaterial.PlainsGround
                }
            };
        }

        protected void CreateTileMapView()
        {
            tileRoot = new GameObject("WorldTiles").transform;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    var tileObject = new GameObject($"Tile_{x}_{y}");
                    tileObject.transform.SetParent(tileRoot, false);
                    tileObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, 2f);
                    tileObject.transform.localScale = Vector3.one;

                    var baseRenderer = tileObject.AddComponent<SpriteRenderer>();
                    baseRenderer.sprite = whiteSprite;
                    baseRenderer.sortingOrder = -10;

                    var accentObject = new GameObject("Accent");
                    accentObject.transform.SetParent(tileObject.transform, false);
                    accentObject.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                    accentObject.transform.localScale = new Vector3(0.94f, 0.22f, 1f);
                    var accentRenderer = accentObject.AddComponent<SpriteRenderer>();
                    accentRenderer.sprite = whiteSprite;
                    accentRenderer.sortingOrder = -9;

                    var shadowObject = new GameObject("Shadow");
                    shadowObject.transform.SetParent(tileObject.transform, false);
                    shadowObject.transform.localPosition = new Vector3(0f, -0.32f, 0f);
                    shadowObject.transform.localScale = new Vector3(0.96f, 0.16f, 1f);
                    var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                    shadowRenderer.sprite = whiteSprite;
                    shadowRenderer.sortingOrder = -9;

                    var collider = tileObject.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(1f, 1f);

                    tileViews[x, y] = new TileView
                    {
                        GameObject = tileObject,
                        BaseRenderer = baseRenderer,
                        AccentRenderer = accentRenderer,
                        ShadowRenderer = shadowRenderer,
                        Collider = collider
                    };

                    RefreshTileVisual(x, y);
                }
            }
        }

        protected void RefreshTileVisual(int x, int y)
        {
            TileView tile = tileViews[x, y];
            if (tile == null) return;

            TileMaterial material = materialGrid[x, y];
            switch (grid[x, y])
            {
                case TileType.Solid:
                    tile.BaseRenderer.color = GetWallColor(material);
                    tile.AccentRenderer.color = GetWallAccentColor(material);
                    tile.ShadowRenderer.color = GetWallShadowColor(material);
                    tile.AccentRenderer.enabled = true;
                    tile.ShadowRenderer.enabled = true;
                    tile.Collider.enabled = true;
                    break;
                case TileType.Resource:
                    tile.BaseRenderer.color = GetResourceColor(material);
                    tile.AccentRenderer.color = GetResourceAccentColor(material);
                    tile.ShadowRenderer.color = GetResourceShadowColor(material);
                    tile.AccentRenderer.enabled = true;
                    tile.ShadowRenderer.enabled = true;
                    tile.Collider.enabled = false;
                    break;
                default:
                    tile.BaseRenderer.color = GetFloorColor(material);
                    tile.AccentRenderer.enabled = false;
                    tile.ShadowRenderer.enabled = false;
                    tile.Collider.enabled = false;
                    break;
            }
        }

        protected Color GetFloorColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestGround => new Color(0.14f, 0.21f, 0.15f, 1f),
                TileMaterial.RuinsGround => new Color(0.22f, 0.20f, 0.18f, 1f),
                TileMaterial.IronGround => new Color(0.17f, 0.18f, 0.20f, 1f),
                TileMaterial.EmberGround => new Color(0.24f, 0.13f, 0.10f, 1f),
                _ => new Color(0.18f, 0.19f, 0.15f, 1f)
            };
        }

        protected Color GetWallColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestWood => new Color(0.45f, 0.30f, 0.18f, 1f),
                TileMaterial.RuinsStone => new Color(0.46f, 0.41f, 0.36f, 1f),
                TileMaterial.IronStone => new Color(0.37f, 0.41f, 0.47f, 1f),
                TileMaterial.EmberStone => new Color(0.46f, 0.23f, 0.17f, 1f),
                TileMaterial.WoodBlock => new Color(0.57f, 0.38f, 0.19f, 1f),
                TileMaterial.StoneBlock => new Color(0.53f, 0.54f, 0.57f, 1f),
                TileMaterial.IronBlock => new Color(0.44f, 0.52f, 0.62f, 1f),
                _ => new Color(0.48f, 0.46f, 0.34f, 1f)
            };
        }

        protected Color GetWallAccentColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestWood => new Color(0.73f, 0.55f, 0.30f, 1f),
                TileMaterial.RuinsStone => new Color(0.67f, 0.61f, 0.53f, 1f),
                TileMaterial.IronStone => new Color(0.62f, 0.67f, 0.73f, 1f),
                TileMaterial.EmberStone => new Color(0.79f, 0.44f, 0.28f, 1f),
                TileMaterial.WoodBlock => new Color(0.82f, 0.63f, 0.33f, 1f),
                TileMaterial.StoneBlock => new Color(0.73f, 0.75f, 0.78f, 1f),
                TileMaterial.IronBlock => new Color(0.73f, 0.84f, 0.94f, 1f),
                _ => new Color(0.69f, 0.67f, 0.45f, 1f)
            };
        }

        protected Color GetWallShadowColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestWood => new Color(0.19f, 0.11f, 0.06f, 0.95f),
                TileMaterial.RuinsStone => new Color(0.19f, 0.16f, 0.14f, 0.95f),
                TileMaterial.IronStone => new Color(0.18f, 0.21f, 0.25f, 0.95f),
                TileMaterial.EmberStone => new Color(0.20f, 0.09f, 0.07f, 0.95f),
                TileMaterial.WoodBlock => new Color(0.24f, 0.14f, 0.07f, 0.95f),
                TileMaterial.StoneBlock => new Color(0.24f, 0.25f, 0.28f, 0.95f),
                TileMaterial.IronBlock => new Color(0.20f, 0.25f, 0.30f, 0.95f),
                _ => new Color(0.22f, 0.20f, 0.12f, 0.95f)
            };
        }

        protected Color GetResourceColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestResource => new Color(0.19f, 0.45f, 0.22f, 1f),
                TileMaterial.RuinsResource => new Color(0.50f, 0.50f, 0.36f, 1f),
                TileMaterial.IronResource => new Color(0.31f, 0.45f, 0.55f, 1f),
                TileMaterial.EmberResource => new Color(0.52f, 0.30f, 0.14f, 1f),
                _ => new Color(0.36f, 0.47f, 0.23f, 1f)
            };
        }

        protected Color GetResourceAccentColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestResource => new Color(0.46f, 0.79f, 0.41f, 1f),
                TileMaterial.RuinsResource => new Color(0.80f, 0.80f, 0.63f, 1f),
                TileMaterial.IronResource => new Color(0.61f, 0.81f, 0.96f, 1f),
                TileMaterial.EmberResource => new Color(0.93f, 0.58f, 0.21f, 1f),
                _ => new Color(0.56f, 0.76f, 0.36f, 1f)
            };
        }

        protected Color GetResourceShadowColor(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.ForestResource => new Color(0.08f, 0.14f, 0.08f, 0.75f),
                TileMaterial.RuinsResource => new Color(0.18f, 0.17f, 0.12f, 0.75f),
                TileMaterial.IronResource => new Color(0.11f, 0.16f, 0.20f, 0.75f),
                TileMaterial.EmberResource => new Color(0.20f, 0.10f, 0.05f, 0.75f),
                _ => new Color(0.12f, 0.14f, 0.07f, 0.75f)
            };
        }

        protected Color GetMinimapTileColor(int x, int y)
        {
            return grid[x, y] switch
            {
                TileType.Solid => GetWallAccentColor(materialGrid[x, y]),
                TileType.Resource => GetResourceAccentColor(materialGrid[x, y]),
                _ => Color.clear
            };
        }

        protected void CreateViews()
        {
            playerView = CreatePlayerView();
            creatureView = CreateActorView("Creature", new Color(0.44f, 0.83f, 1f), new Vector2(0.8f, 0.8f));
            collectorView = CreateActorView("Collector", new Color(0.45f, 0.9f, 0.52f), new Vector2(0.7f, 0.7f));
        }

        protected ActorView CreateActorView(string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return new ActorView { GameObject = go, Renderer = renderer, BaseScale = go.transform.localScale };
        }

        protected ActorView CreatePlayerView()
        {
            LoadPlayerSprites();
            if (playerIdleFrames.Length > 0)
            {
                var view = CreateSpriteActorView("Player", playerIdleFrames[0], new Vector3(1.2f, 1.2f, 1f));
                view.Renderer.sortingOrder = 5;
                return view;
            }

            var fallback = CreateActorView("Player", new Color(0.96f, 0.86f, 0.32f), new Vector2(0.9f, 0.9f));
            fallback.Renderer.sortingOrder = 5;
            return fallback;
        }

        protected ActorView CreateSpriteActorView(string name, Sprite sprite, Vector3 scale)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            go.transform.localScale = scale;
            return new ActorView { GameObject = go, Renderer = renderer, BaseScale = scale };
        }

        protected void LoadPlayerSprites()
        {
            if (playerIdleFrames.Length > 0 || playerWalkFrames.Length > 0) return;

            playerIdleFrames = LoadSprites(
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_01.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_02.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_03.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_04.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_05.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/idle_06.png");

            playerWalkFrames = LoadSprites(
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_01.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_02.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_03.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_04.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_05.png",
                "Assets/Dragon Warrior Files/Dragon Warrior PNG/walk_06.png");
        }

        protected Sprite[] LoadSprites(params string[] assetPaths)
        {
            var sprites = new List<Sprite>();
            foreach (string assetPath in assetPaths)
            {
                Sprite sprite = LoadSpriteAsset(assetPath);
                if (sprite != null) sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        protected Sprite LoadSpriteAsset(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        protected GameObject LoadPrefabAsset(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            return null;
#endif
        }

        protected ActorView CreateEnemyView(EnemyKind kind, Color fallbackColor)
        {
            switch (kind)
            {
                case EnemyKind.Chicken:
                {
                    var view = TryCreateSpriteEnemyView(kind.ToString(), "Assets/Pixel Monster Pack/64x64 monsters/rat-brown.png", new Vector3(1.35f, 1.35f, 1f));
                    if (view != null) return view;
                    break;
                }
                case EnemyKind.Santelmo:
                {
                    var view = TryCreateSpriteEnemyView(kind.ToString(), "Assets/Pixel Monster Pack/64x64 monsters/skull-blue.png", new Vector3(1.2f, 1.2f, 1f));
                    if (view != null) return view;
                    break;
                }
                case EnemyKind.Tikbalang:
                {
                    var view = TryCreatePrefabEnemyView(kind.ToString(), "Assets/Dark fantasy - popular enemies- Free Sample/Goblin/Goblin_Thief.prefab", new Vector3(0.75f, 0.75f, 1f));
                    if (view != null) return view;
                    break;
                }
                case EnemyKind.DwendeBlue:
                {
                    var view = TryCreateSpriteEnemyView(kind.ToString(), "Assets/Pixel Monster Pack/64x64 monsters/slime-green.png", new Vector3(1.25f, 1.25f, 1f));
                    if (view != null) return view;
                    break;
                }
                case EnemyKind.Kapre:
                {
                    var view = TryCreatePrefabEnemyView(kind.ToString(), "Assets/Dark fantasy - popular enemies- Free Sample/Skeleton/skeleton.prefab", new Vector3(0.75f, 0.75f, 1f));
                    if (view != null) return view;
                    break;
                }
            }

            return CreateActorView(kind.ToString(), fallbackColor, new Vector2(0.85f, 0.85f));
        }

        protected ActorView TryCreateSpriteEnemyView(string name, string assetPath, Vector3 scale)
        {
            Sprite sprite = LoadSpriteAsset(assetPath);
            if (sprite == null) return null;
            var view = CreateSpriteActorView(name, sprite, scale);
            view.Renderer.sortingOrder = 3;
            return view;
        }

        protected ActorView TryCreatePrefabEnemyView(string name, string assetPath, Vector3 scale)
        {
            GameObject prefab = LoadPrefabAsset(assetPath);
            if (prefab == null) return null;

            GameObject instance = Instantiate(prefab);
            instance.name = name;
            instance.transform.localScale = scale;
            SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                Destroy(instance);
                return null;
            }

            foreach (SpriteRenderer childRenderer in instance.GetComponentsInChildren<SpriteRenderer>())
            {
                childRenderer.sortingOrder = 3;
            }

            return new ActorView { GameObject = instance, Renderer = renderer, BaseScale = scale };
        }

        protected void UpdatePlayerPresentation(float dt)
        {
            Sprite[] frames = playerInput.sqrMagnitude > 0.01f ? playerWalkFrames : playerIdleFrames;
            if (frames.Length > 0)
            {
                float frameDuration = playerInput.sqrMagnitude > 0.01f ? 0.09f : 0.14f;
                playerAnimationTimer += dt;
                if (playerAnimationTimer >= frameDuration)
                {
                    playerAnimationTimer = 0f;
                    playerAnimationFrame = (playerAnimationFrame + 1) % frames.Length;
                }

                playerView.Renderer.sprite = frames[playerAnimationFrame];
                playerView.Renderer.color = Color.white;
            }

            if (Mathf.Abs(playerInput.x) > 0.01f) SetFacing(playerView, playerInput.x);
        }

        protected void SetFacing(ActorView view, float horizontal)
        {
            if (Mathf.Abs(horizontal) <= 0.01f) return;
            Vector3 scale = view.BaseScale;
            scale.x *= horizontal < 0f ? -1f : 1f;
            view.GameObject.transform.localScale = scale;
        }

        protected ActorView CreateDropView(string name, Color color)
        {
            var view = CreateActorView(name, color, new Vector2(0.35f, 0.35f));
            view.Renderer.sortingOrder = 2;
            return view;
        }

        protected ItemDrop GetBiomeMineDrop(int x, int y)
        {
            return biomeGrid[x, y] switch
            {
                TileBiome.Forest => new ItemDrop("wood", "Wood", new Color(0.71f, 0.52f, 0.25f)),
                TileBiome.Ruins => new ItemDrop("stone", "Stone", new Color(0.72f, 0.70f, 0.66f)),
                TileBiome.IronWastes => new ItemDrop("iron_ore", "Iron Ore", new Color(0.75f, 0.79f, 0.86f)),
                TileBiome.Ember => new ItemDrop("iron_ore", "Iron Ore", new Color(0.85f, 0.61f, 0.33f)),
                _ => new ItemDrop("stone", "Stone", new Color(0.69f, 0.66f, 0.60f))
            };
        }

        protected ItemDrop GetBiomeEnemyDrop(Vector2 position)
        {
            Vector2Int tile = WorldToTile(position);
            if (!IsWithin(tile)) return new ItemDrop("wood", "Wood", new Color(0.71f, 0.52f, 0.25f));

            return biomeGrid[tile.x, tile.y] switch
            {
                TileBiome.Forest => new ItemDrop("wood", "Wood", new Color(0.71f, 0.52f, 0.25f)),
                TileBiome.Ruins => new ItemDrop("stone", "Stone", new Color(0.72f, 0.70f, 0.66f)),
                TileBiome.IronWastes => new ItemDrop("iron_ore", "Iron Ore", new Color(0.75f, 0.79f, 0.86f)),
                TileBiome.Ember => new ItemDrop("iron_ore", "Iron Ore", new Color(0.85f, 0.61f, 0.33f)),
                _ => new ItemDrop("stone", "Stone", new Color(0.72f, 0.70f, 0.66f))
            };
        }

        protected TileMaterial GetPlacedMaterial(string itemId)
        {
            return itemId switch
            {
                "wood" => TileMaterial.WoodBlock,
                "stone" => TileMaterial.StoneBlock,
                "iron_ore" => TileMaterial.IronBlock,
                _ => TileMaterial.WoodBlock
            };
        }

        protected bool IsPlacedBlock(TileMaterial material)
        {
            return material == TileMaterial.WoodBlock || material == TileMaterial.StoneBlock || material == TileMaterial.IronBlock;
        }

        protected string GetPlacedBlockItemId(TileMaterial material)
        {
            return material switch
            {
                TileMaterial.WoodBlock => "wood",
                TileMaterial.StoneBlock => "stone",
                TileMaterial.IronBlock => "iron_ore",
                _ => "wood"
            };
        }

        protected void AddItem(string itemId, string name, int quantity, int maxStack = 99)
        {
            int remaining = quantity;
            foreach (var stack in inventory.Where(item => item.Id == itemId))
            {
                if (remaining <= 0) break;
                int free = stack.MaxStack - stack.Quantity;
                if (free <= 0) continue;
                int add = Mathf.Min(remaining, free);
                stack.Quantity += add;
                remaining -= add;
            }

            while (remaining > 0 && inventory.Count < 20)
            {
                int add = Mathf.Min(remaining, maxStack);
                inventory.Add(new InventoryItem { Id = itemId, Name = name, Quantity = add, MaxStack = maxStack });
                remaining -= add;
            }
        }

        protected bool RemoveItem(string itemId, int quantity)
        {
            if (GetItemCount(itemId) < quantity) return false;
            int remaining = quantity;
            for (int i = 0; i < inventory.Count && remaining > 0; i++)
            {
                if (inventory[i].Id != itemId) continue;
                int remove = Mathf.Min(remaining, inventory[i].Quantity);
                inventory[i].Quantity -= remove;
                remaining -= remove;
            }

            inventory.RemoveAll(item => item.Quantity <= 0);
            CleanupHotbarReferences(itemId);
            return true;
        }

        protected int GetItemCount(string itemId)
        {
            return inventory.Where(item => item.Id == itemId).Sum(item => item.Quantity);
        }

        protected bool CanCraft(Recipe recipe)
        {
            if (recipe.RequiredAffinity.HasValue && player.Affinities[recipe.RequiredAffinity.Value] < recipe.RequiredAmount) return false;
            return recipe.Ingredients.All(ingredient => GetItemCount(ingredient.ItemId) >= ingredient.Quantity);
        }

        protected void Craft(Recipe recipe)
        {
            if (!CanCraft(recipe))
            {
                AddLog($"Cannot craft {recipe.Name} yet.");
                return;
            }

            foreach (var ingredient in recipe.Ingredients) RemoveItem(ingredient.ItemId, ingredient.Quantity);
            AddItem(recipe.ResultItemId, recipe.Name, recipe.ResultQuantity);
            TryEquipCraftedWeapon(recipe.ResultItemId);
            GrantAffinity(AffinityType.Craft, 15);
            AddLog($"Crafted {recipe.Name}.");
        }

        protected bool IsWeaponItem(string itemId) => itemId == "wood_sword" || itemId == "iron_sword";

        protected void EquipWeaponFromItem(string itemId)
        {
            if (itemId == "wood_sword") player.EquippedWeapon = WeaponId.WoodSword;
            if (itemId == "iron_sword") player.EquippedWeapon = WeaponId.IronSword;
            SaveState();
        }

        protected void TryEquipCraftedWeapon(string itemId)
        {
            if (itemId == "iron_sword") player.EquippedWeapon = WeaponId.IronSword;
            else if (itemId == "wood_sword" && player.EquippedWeapon == WeaponId.RustyDagger) player.EquippedWeapon = WeaponId.WoodSword;
        }

        protected void DropInventoryStack(InventoryItem item)
        {
            inventory.Remove(item);
            CleanupHotbarReferences(item.Id);
            Color color = item.Id switch
            {
                "iron_ore" => new Color(0.75f, 0.79f, 0.86f),
                "stone" => new Color(0.72f, 0.70f, 0.66f),
                _ => new Color(0.71f, 0.52f, 0.25f)
            };

            worldDrops.Add(new WorldDrop
            {
                ItemId = item.Id,
                Name = item.Name,
                Quantity = item.Quantity,
                Position = player.Position + player.Facing.normalized * 1.2f,
                View = CreateDropView($"Drop_{item.Name}", color)
            });

            SaveState();
        }

        protected void AssignItemToSelectedHotbar(string itemId)
        {
            hotbarItemIds[selectedHotbarIndex] = itemId;
            AddLog($"{GetDisplayName(itemId)} assigned to slot {selectedHotbarIndex + 1}.");
            SaveState();
        }

        protected void CleanupHotbarReferences(string itemId)
        {
            if (GetItemCount(itemId) > 0) return;
            for (int i = 0; i < hotbarItemIds.Length; i++)
            {
                if (hotbarItemIds[i] == itemId) hotbarItemIds[i] = string.Empty;
            }
        }

        protected bool IsPlaceableItem(string itemId) => itemId == "wood" || itemId == "stone" || itemId == "iron_ore";

        protected string GetDisplayName(string itemId)
        {
            InventoryItem stack = inventory.FirstOrDefault(item => item.Id == itemId);
            if (stack != null) return stack.Name;

            return itemId switch
            {
                "wood" => "Wood",
                "stone" => "Stone",
                "iron_ore" => "Iron Ore",
                "wood_sword" => "Wood Sword",
                "iron_sword" => "Iron Sword",
                _ => itemId
            };
        }

        protected WeaponDefinition GetWeapon(WeaponId id) => weaponDefinitions[id];

        protected int GetPlayerDamage()
        {
            WeaponDefinition weapon = GetWeapon(player.EquippedWeapon);
            return weapon.BaseDamage + GetLevelDamageBonus() + GetMeleeAffinityBonus() + GetPassiveMeleeDamageBonus();
        }

        protected int RollPlayerDamage(out bool crit)
        {
            crit = UnityEngine.Random.value < GetPlayerCritChance();
            int damage = GetPlayerDamage();
            if (crit) damage = Mathf.RoundToInt(damage * GetWeapon(player.EquippedWeapon).CritMultiplier);
            return damage;
        }

        protected float GetPlayerCritChance()
        {
            float affinityBonus = player.Affinities[AffinityType.Melee] >= 80 ? 0.08f :
                player.Affinities[AffinityType.Melee] >= 40 ? 0.05f :
                player.Affinities[AffinityType.Melee] >= 20 ? 0.02f : 0f;
            return Mathf.Min(0.45f, GetWeapon(player.EquippedWeapon).CritChance + affinityBonus);
        }

        protected int GetLevelDamageBonus() => player.Level - 1;

        protected int GetMeleeAffinityBonus()
        {
            if (player.Affinities[AffinityType.Melee] >= 80) return 4;
            if (player.Affinities[AffinityType.Melee] >= 40) return 2;
            return 0;
        }

        protected int GetPassiveMeleeDamageBonus()
        {
            return GetArchetype() switch
            {
                "Berserker" => 4,
                "Battle Smith" => 2,
                _ => 0
            };
        }

        protected void GrantAffinity(AffinityType type, int amount) => player.Affinities[type] += amount;

        protected string GetArchetype()
        {
            int craft = player.Affinities[AffinityType.Craft];
            int melee = player.Affinities[AffinityType.Melee];
            int fire = player.Affinities[AffinityType.Fire];
            int nature = player.Affinities[AffinityType.Nature];

            if (melee >= 80) return "Berserker";
            if (melee >= 50 && craft >= 30) return "Battle Smith";
            if (nature >= 60 && melee >= 20) return "Beastmaster";
            if (fire >= 50 && melee >= 20) return "Necromancer";
            if (craft >= 60 && fire >= 20) return "Rune Smith";
            if (craft >= 40) return "Artisan";
            if (fire >= 40) return "Pyromancer";
            if (nature >= 40) return "Naturalist";
            return "Wanderer";
        }

        protected string GetArchetypeAbility()
        {
            return GetArchetype() switch
            {
                "Necromancer" => "Bone Spark",
                "Rune Smith" => "Forge Rune",
                "Battle Smith" => "Forge Strike",
                "Pyromancer" => "Inferno",
                "Berserker" => "Rage Blow",
                "Artisan" => "Master Craft",
                _ => "None"
            };
        }

        protected int GetXpForNextLevel(int level) => level * 25;
        protected int GetCreatureXpForNextLevel() => creature.Level * 18;

        protected void AddLog(string message)
        {
            logs.Insert(0, message);
            while (logs.Count > 4) logs.RemoveAt(logs.Count - 1);
        }

        protected string GetLogText() => string.Join("\n", logs);

        protected void FlashPanel(Color color)
        {
            panelFlashColor = color;
            panelFlashTimer = 0.35f;
        }

        protected void TryMove(ref Vector2 current, Vector2 target)
        {
            Vector2 horizontal = new(target.x, current.y);
            if (IsWalkable(horizontal)) current.x = horizontal.x;
            Vector2 vertical = new(current.x, target.y);
            if (IsWalkable(vertical)) current.y = vertical.y;
        }

        protected bool IsWalkable(Vector2 worldPosition)
        {
            Vector2Int tile = WorldToTile(worldPosition);
            return IsWithin(tile) && grid[tile.x, tile.y] != TileType.Solid;
        }

        protected Vector2Int WorldToTile(Vector2 world) => new(Mathf.FloorToInt(world.x / TileSize), Mathf.FloorToInt(world.y / TileSize));
        protected Vector2 TileToWorld(int x, int y) => new(x + 0.5f, y + 0.5f);

        protected bool IsWithin(Vector2Int tile)
        {
            return tile.x >= 0 && tile.x < GridWidth && tile.y >= 0 && tile.y < GridHeight;
        }

        protected Vector2 GetMouseWorld()
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
            return new Vector2(world.x, world.y);
        }

        protected void ApplyPosition(ActorView view, Vector2 position)
        {
            view.GameObject.transform.position = new Vector3(position.x, position.y, 0f);
        }

        protected void SaveState()
        {
            var save = new SaveData
            {
                PlayerLevel = player.Level,
                PlayerXp = player.Xp,
                PlayerHealth = player.Health,
                Weapon = player.EquippedWeapon.ToString(),
                PlayerX = player.Position.x,
                PlayerY = player.Position.y,
                CreatureLevel = creature.Level,
                CreatureXp = creature.Xp,
                CreatureHealth = creature.Health,
                CreatureX = creature.Position.x,
                CreatureY = creature.Position.y,
                CollectorCount = collector.CollectedCount,
                Affinities = player.Affinities.Select(pair => new AffinityEntry { Key = pair.Key.ToString(), Value = pair.Value }).ToList(),
                Inventory = inventory.Select(item => new InventoryEntry { Id = item.Id, Name = item.Name, Quantity = item.Quantity, MaxStack = item.MaxStack }).ToList(),
                Hotbar = hotbarItemIds.ToList(),
                SelectedHotbarIndex = selectedHotbarIndex
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        protected void LoadState()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            SaveData save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (save == null) return;

            player.Level = Mathf.Max(1, save.PlayerLevel);
            player.Xp = save.PlayerXp;
            player.MaxHealth = 100 + (player.Level - 1) * 12;
            player.Health = Mathf.Clamp(save.PlayerHealth, 1, player.MaxHealth);
            player.Position = new Vector2(save.PlayerX, save.PlayerY);
            if (Enum.TryParse(save.Weapon, out WeaponId weapon)) player.EquippedWeapon = weapon;

            creature.Level = Mathf.Max(1, save.CreatureLevel);
            creature.Xp = save.CreatureXp;
            creature.MaxHealth = 50 + (creature.Level - 1) * 10;
            creature.Health = Mathf.Clamp(save.CreatureHealth, 1, creature.MaxHealth);
            creature.Damage = 5 + (creature.Level - 1) * 2;
            creature.Position = new Vector2(save.CreatureX, save.CreatureY);
            collector.CollectedCount = save.CollectorCount;

            foreach (var affinity in save.Affinities)
            {
                if (Enum.TryParse(affinity.Key, out AffinityType affinityType)) player.Affinities[affinityType] = affinity.Value;
            }

            inventory.Clear();
            foreach (var item in save.Inventory)
            {
                inventory.Add(new InventoryItem { Id = item.Id, Name = item.Name, Quantity = item.Quantity, MaxStack = item.MaxStack });
            }

            for (int i = 0; i < hotbarItemIds.Length; i++)
            {
                hotbarItemIds[i] = i < save.Hotbar.Count ? save.Hotbar[i] : string.Empty;
            }

            selectedHotbarIndex = Mathf.Clamp(save.SelectedHotbarIndex, 0, hotbarItemIds.Length - 1);
        }
    }
}
