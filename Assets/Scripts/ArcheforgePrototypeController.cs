using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archeforge.UnityPort
{
    public class ArcheforgePrototypeController : MonoBehaviour
    {
        private enum TileType { Empty, Solid, Resource }
        private enum AffinityType { Craft, Melee, Fire, Nature }
        private enum WeaponId { RustyDagger, WoodSword, IronSword }
        private enum EnemyKind { Chicken, Santelmo, Tikbalang, DwendeBlue, Kapre }

        [Serializable]
        private sealed class WeaponDefinition
        {
            public WeaponId Id;
            public string Name = string.Empty;
            public int BaseDamage;
            public float CritChance;
            public float CritMultiplier;
        }

        [Serializable]
        private sealed class InventoryItem
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public int Quantity;
            public int MaxStack = 99;
        }

        [Serializable]
        private sealed class Ingredient
        {
            public string ItemId = string.Empty;
            public int Quantity;
        }

        [Serializable]
        private sealed class Recipe
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public List<Ingredient> Ingredients = new();
            public string ResultItemId = string.Empty;
            public int ResultQuantity = 1;
            public AffinityType? RequiredAffinity;
            public int RequiredAmount;
        }

        private sealed class ActorView
        {
            public GameObject GameObject = null!;
            public SpriteRenderer Renderer = null!;
        }

        private sealed class PlayerState
        {
            public Vector2 Position = new(12f, 12f);
            public Vector2 Facing = Vector2.down;
            public int Level = 1;
            public int Xp;
            public int MaxHealth = 100;
            public int Health = 100;
            public WeaponId EquippedWeapon = WeaponId.RustyDagger;
            public readonly Dictionary<AffinityType, int> Affinities = new()
            {
                { AffinityType.Craft, 0 },
                { AffinityType.Melee, 0 },
                { AffinityType.Fire, 0 },
                { AffinityType.Nature, 0 }
            };
        }

        private sealed class CreatureState
        {
            public Vector2 Position = new(9f, 10f);
            public int Level = 1;
            public int Xp;
            public int MaxHealth = 50;
            public int Health = 50;
            public int Damage = 5;
            public float AttackCooldown;
        }

        private sealed class CollectorState
        {
            public Vector2 Position = new(7f, 10f);
            public int CollectedCount;
        }

        private sealed class EnemyState
        {
            public EnemyKind Kind;
            public Vector2 Position;
            public int MaxHealth;
            public int Health;
            public int Damage;
            public float Speed;
            public float DetectionRange;
            public float AttackRange;
            public float AttackCooldown;
            public float AttackTimer;
            public bool Alive = true;
            public int SpawnSlotIndex;
            public ActorView View = null!;
        }

        private sealed class SpawnSlot
        {
            public EnemyKind Kind;
            public Vector2 Center;
            public float Radius;
            public EnemyState? Enemy;
            public float RespawnTimer;
        }

        private sealed class WorldDrop
        {
            public string ItemId = string.Empty;
            public string Name = string.Empty;
            public int Quantity;
            public Vector2 Position;
            public ActorView View = null!;
        }

        [Serializable]
        private sealed class SaveData
        {
            public int PlayerLevel;
            public int PlayerXp;
            public int PlayerHealth;
            public string Weapon = string.Empty;
            public float PlayerX;
            public float PlayerY;
            public int CreatureLevel;
            public int CreatureXp;
            public int CreatureHealth;
            public float CreatureX;
            public float CreatureY;
            public int CollectorCount;
            public List<AffinityEntry> Affinities = new();
            public List<InventoryEntry> Inventory = new();
        }

        [Serializable]
        private sealed class AffinityEntry
        {
            public string Key = string.Empty;
            public int Value;
        }

        [Serializable]
        private sealed class InventoryEntry
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public int Quantity;
            public int MaxStack;
        }

        private readonly Dictionary<WeaponId, WeaponDefinition> weaponDefinitions = new()
        {
            { WeaponId.RustyDagger, new WeaponDefinition { Id = WeaponId.RustyDagger, Name = "Rusty Dagger", BaseDamage = 5, CritChance = 0.18f, CritMultiplier = 2.1f } },
            { WeaponId.WoodSword, new WeaponDefinition { Id = WeaponId.WoodSword, Name = "Wood Sword", BaseDamage = 8, CritChance = 0.10f, CritMultiplier = 1.8f } },
            { WeaponId.IronSword, new WeaponDefinition { Id = WeaponId.IronSword, Name = "Iron Sword", BaseDamage = 12, CritChance = 0.06f, CritMultiplier = 1.65f } }
        };

        private readonly List<Recipe> recipes = new()
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

        private readonly List<string> logs = new();
        private readonly List<InventoryItem> inventory = new();
        private readonly List<EnemyState> enemies = new();
        private readonly List<SpawnSlot> spawnSlots = new();
        private readonly List<WorldDrop> worldDrops = new();

        private TileType[,] grid = null!;
        private Sprite whiteSprite = null!;
        private Camera mainCamera = null!;
        private Canvas runtimeCanvas = null!;
        private PlayerState player = null!;
        private CreatureState creature = null!;
        private CollectorState collector = null!;
        private ActorView playerView = null!;
        private ActorView creatureView = null!;
        private ActorView collectorView = null!;

        private bool inventoryOpen;
        private bool craftingOpen;
        private Vector2 inventoryScroll;
        private Vector2 craftingScroll;
        private float panelFlashTimer;
        private Color panelFlashColor = Color.cyan;

        private const int GridWidth = 100;
        private const int GridHeight = 100;
        private const float TileSize = 1f;
        private const float PlayerSpeed = 6.5f;
        private const string SaveKey = "archeforge-unity-prototype-v1";

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 7.5f;
            mainCamera.backgroundColor = new Color(0.17f, 0.24f, 0.31f);

            EnsureUiBootstrap();

            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            grid = new TileType[GridWidth, GridHeight];
            player = new PlayerState();
            creature = new CreatureState();
            collector = new CollectorState();

            GenerateGrid();
            BuildSpawnSlots();
            CreateViews();
            LoadState();
            SpawnInitialEnemies();
            AddLog("Unity prototype loaded.");
        }

        private void EnsureUiBootstrap()
        {
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                GameObject canvasObject = new("Canvas");
                runtimeCanvas = canvasObject.AddComponent<Canvas>();
                runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }
            else
            {
                runtimeCanvas = existingCanvas;
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObject = new("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            HandleGlobalInput();
            UpdateCamera();
            UpdateSpawnRespawns(dt);
            UpdateEnemies(dt);
            UpdateCreature(dt);
            UpdateCollector(dt);
            UpdateWorldDrops();

            if (!inventoryOpen && !craftingOpen)
            {
                HandleMovement(dt);
                HandleActions();
                HandleMining();
            }
            else
            {
                ApplyPosition(playerView, player.Position);
            }

            ApplyPosition(playerView, player.Position);
            ApplyPosition(creatureView, creature.Position);
            ApplyPosition(collectorView, collector.Position);

            if (panelFlashTimer > 0f)
            {
                panelFlashTimer -= dt;
            }
        }

        private void OnApplicationQuit()
        {
            SaveState();
        }

        private void GenerateGrid()
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    bool border = x == 0 || y == 0 || x == GridWidth - 1 || y == GridHeight - 1;
                    bool cluster = (y % 4 == 0 && x % 5 == 0) || (y % 6 == 2 && x % 7 == 3);
                    bool randomRock = UnityEngine.Random.value < 0.05f;
                    grid[x, y] = border || cluster || randomRock ? TileType.Solid : TileType.Empty;
                }
            }
        }

        private void BuildSpawnSlots()
        {
            spawnSlots.Clear();
            spawnSlots.Add(new SpawnSlot { Kind = EnemyKind.Chicken, Center = new Vector2(16f, 6f), Radius = 3.2f });
            spawnSlots.Add(new SpawnSlot { Kind = EnemyKind.Santelmo, Center = new Vector2(25f, 12f), Radius = 3.8f });
            spawnSlots.Add(new SpawnSlot { Kind = EnemyKind.Tikbalang, Center = new Vector2(37f, 9f), Radius = 4.2f });
            spawnSlots.Add(new SpawnSlot { Kind = EnemyKind.DwendeBlue, Center = new Vector2(18f, 18f), Radius = 3.2f });
            spawnSlots.Add(new SpawnSlot { Kind = EnemyKind.Kapre, Center = new Vector2(46f, 26f), Radius = 4f });
        }

        private void CreateViews()
        {
            playerView = CreateActorView("Player", new Color(0.96f, 0.86f, 0.32f), new Vector2(0.9f, 0.9f));
            creatureView = CreateActorView("Creature", new Color(0.44f, 0.83f, 1f), new Vector2(0.8f, 0.8f));
            collectorView = CreateActorView("Collector", new Color(0.45f, 0.9f, 0.52f), new Vector2(0.7f, 0.7f));
        }

        private ActorView CreateActorView(string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return new ActorView { GameObject = go, Renderer = renderer };
        }

        private ActorView CreateDropView(string name, Color color)
        {
            var view = CreateActorView(name, color, new Vector2(0.35f, 0.35f));
            view.Renderer.sortingOrder = 2;
            return view;
        }

        private void SpawnInitialEnemies()
        {
            for (int i = 0; i < spawnSlots.Count; i++)
            {
                SpawnEnemyAtSlot(i);
            }
        }

        private void SpawnEnemyAtSlot(int slotIndex)
        {
            var slot = spawnSlots[slotIndex];
            if (slot.Enemy != null)
            {
                return;
            }

            if (!TryFindSpawnPosition(slot, out var position))
            {
                slot.RespawnTimer = 1.25f;
                return;
            }

            var enemy = CreateEnemy(slot.Kind, position, slotIndex);
            slot.Enemy = enemy;
            enemies.Add(enemy);
        }

        private EnemyState CreateEnemy(EnemyKind kind, Vector2 position, int spawnSlotIndex)
        {
            float levelFactor = 1f + (player.Level - 1) * 0.14f;
            float damageFactor = 1f + (player.Level - 1) * 0.08f;
            int baseHealth = 18;
            int baseDamage = 6;
            float speed = 2.6f;
            float detection = 7f;
            float attackRange = 1.25f;
            float cooldown = 1.4f;
            Color color = new(0.94f, 0.35f, 0.35f);

            switch (kind)
            {
                case EnemyKind.Chicken:
                    baseHealth = 14; baseDamage = 5; speed = 3.5f; detection = 6f; cooldown = 1.0f; color = new Color(0.98f, 0.82f, 0.42f);
                    break;
                case EnemyKind.Santelmo:
                    baseHealth = 18; baseDamage = 6; speed = 3.1f; detection = 7.5f; cooldown = 0.9f; color = new Color(0.58f, 0.89f, 1f);
                    break;
                case EnemyKind.Tikbalang:
                    baseHealth = 28; baseDamage = 8; speed = 3.9f; detection = 8.5f; cooldown = 1.4f; color = new Color(0.8f, 0.6f, 0.9f);
                    break;
                case EnemyKind.DwendeBlue:
                    baseHealth = 20; baseDamage = 6; speed = 2.8f; detection = 6.8f; cooldown = 1.1f; color = new Color(0.37f, 0.59f, 1f);
                    break;
                case EnemyKind.Kapre:
                    baseHealth = 34; baseDamage = 10; speed = 2.4f; detection = 9f; cooldown = 1.7f; color = new Color(0.49f, 0.3f, 0.16f);
                    break;
            }

            var enemy = new EnemyState
            {
                Kind = kind,
                Position = position,
                MaxHealth = Mathf.RoundToInt(baseHealth * levelFactor),
                Health = Mathf.RoundToInt(baseHealth * levelFactor),
                Damage = Mathf.RoundToInt(baseDamage * damageFactor),
                Speed = speed,
                DetectionRange = detection,
                AttackRange = attackRange,
                AttackCooldown = cooldown,
                SpawnSlotIndex = spawnSlotIndex,
                View = CreateActorView(kind.ToString(), color, new Vector2(0.85f, 0.85f))
            };

            ApplyPosition(enemy.View, enemy.Position);
            return enemy;
        }

        private bool TryFindSpawnPosition(SpawnSlot slot, out Vector2 position)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float distance = UnityEngine.Random.Range(0.4f, slot.Radius);
                var candidate = slot.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (IsWalkable(candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            if (IsWalkable(slot.Center))
            {
                position = slot.Center;
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        private void HandleGlobalInput()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                inventoryOpen = !inventoryOpen;
                if (inventoryOpen) craftingOpen = false;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                craftingOpen = !craftingOpen;
                if (craftingOpen) inventoryOpen = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                inventoryOpen = false;
                craftingOpen = false;
            }
        }

        private void HandleMovement(float dt)
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (input.sqrMagnitude > 1f) input.Normalize();
            if (input.sqrMagnitude > 0f) player.Facing = input;

            Vector2 next = player.Position + input * PlayerSpeed * dt;
            TryMove(ref player.Position, next);
        }

        private void HandleActions()
        {
            if (Input.GetKeyDown(KeyCode.Space)) PerformMeleeAction();
            if (Input.GetKeyDown(KeyCode.F)) PerformFireAction();
            if (Input.GetKeyDown(KeyCode.R)) PerformNatureAction();
            if (Input.GetKeyDown(KeyCode.E)) PerformArchetypeAbility();
            if (Input.GetKeyDown(KeyCode.G)) CollectDropsNear(player.Position, 1.75f, "Player");
        }

        private void HandleMining()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector2Int tile = WorldToTile(GetMouseWorld());
            if (!IsWithin(tile)) return;

            if (grid[tile.x, tile.y] == TileType.Solid)
            {
                grid[tile.x, tile.y] = TileType.Resource;
                GrantAffinity(AffinityType.Craft, 10);
                if (UnityEngine.Random.value < 0.3f)
                {
                    AddItem("iron_ore", "Iron Ore", 1);
                    AddLog("Mined Iron Ore.");
                }
                else
                {
                    AddItem("wood", "Wood", 1);
                    AddLog("Mined Wood.");
                }
            }
        }

        private void UpdateCamera()
        {
            Vector3 target = new(player.Position.x, player.Position.y, -10f);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, target, 8f * Time.deltaTime);
        }

        private void UpdateSpawnRespawns(float dt)
        {
            for (int i = 0; i < spawnSlots.Count; i++)
            {
                var slot = spawnSlots[i];
                if (slot.Enemy != null || slot.RespawnTimer <= 0f) continue;

                slot.RespawnTimer -= dt;
                if (slot.RespawnTimer <= 0f)
                {
                    SpawnEnemyAtSlot(i);
                    AddLog($"{slot.Kind} respawned.");
                }
            }
        }

        private void UpdateEnemies(float dt)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;

                enemy.AttackTimer -= dt;
                float distance = Vector2.Distance(enemy.Position, player.Position);
                if (distance > enemy.DetectionRange)
                {
                    ApplyPosition(enemy.View, enemy.Position);
                    continue;
                }

                if (distance <= enemy.AttackRange)
                {
                    if (enemy.AttackTimer <= 0f)
                    {
                        player.Health = Mathf.Max(0, player.Health - enemy.Damage);
                        enemy.AttackTimer = enemy.AttackCooldown;
                        AddLog($"{enemy.Kind} hit player for {enemy.Damage}.");
                    }
                }
                else
                {
                    Vector2 dir = (player.Position - enemy.Position).normalized;
                    if (enemy.Kind == EnemyKind.Santelmo)
                    {
                        dir += new Vector2(-dir.y, dir.x) * 0.35f;
                        dir.Normalize();
                    }
                    else if (enemy.Kind == EnemyKind.Chicken)
                    {
                        dir *= 0.8f + Mathf.PingPong(Time.time * 1.5f, 0.45f);
                    }

                    Vector2 next = enemy.Position + dir * enemy.Speed * dt;
                    TryMove(ref enemy.Position, next);
                }

                ApplyPosition(enemy.View, enemy.Position);
            }
        }

        private void UpdateCreature(float dt)
        {
            EnemyState? target = enemies.Where(e => e.Alive).OrderBy(e => Vector2.Distance(e.Position, creature.Position)).FirstOrDefault();
            if (target != null && Vector2.Distance(target.Position, creature.Position) < 6.5f)
            {
                Vector2 dir = (target.Position - creature.Position).normalized;
                TryMove(ref creature.Position, creature.Position + dir * 4.4f * dt);
                creature.AttackCooldown -= dt;
                if (Vector2.Distance(target.Position, creature.Position) <= 1.3f && creature.AttackCooldown <= 0f)
                {
                    creature.AttackCooldown = 0.9f;
                    DamageEnemy(target, creature.Damage, true);
                    creature.Xp += 6;
                    while (creature.Xp >= GetCreatureXpForNextLevel())
                    {
                        creature.Xp -= GetCreatureXpForNextLevel();
                        creature.Level += 1;
                        creature.MaxHealth += 10;
                        creature.Health = creature.MaxHealth;
                        creature.Damage += 2;
                        FlashPanel(new Color(0.44f, 0.83f, 1f));
                        AddLog($"Creature reached level {creature.Level}.");
                    }
                }
            }
            else
            {
                Vector2 follow = player.Position + new Vector2(-1.3f, -0.2f);
                Vector2 dir = follow - creature.Position;
                if (dir.sqrMagnitude > 0.04f)
                {
                    TryMove(ref creature.Position, creature.Position + dir.normalized * 3.8f * dt);
                }
            }

            CollectDropsNear(creature.Position, 1.4f, "Creature");
        }

        private void UpdateCollector(float dt)
        {
            Vector2 bestTarget = player.Position;
            bool foundResource = false;
            float bestDistance = float.MaxValue;
            Vector2Int center = WorldToTile(collector.Position);

            for (int y = center.y - 4; y <= center.y + 4; y++)
            {
                for (int x = center.x - 4; x <= center.x + 4; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (!IsWithin(tile) || grid[x, y] != TileType.Resource) continue;

                    Vector2 target = TileToWorld(x, y);
                    float distance = Vector2.Distance(collector.Position, target);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = target;
                        foundResource = true;
                    }
                }
            }

            Vector2 moveTarget = foundResource ? bestTarget : player.Position + new Vector2(-2.4f, 0.2f);
            Vector2 toTarget = moveTarget - collector.Position;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                TryMove(ref collector.Position, collector.Position + toTarget.normalized * 3.3f * dt);
            }

            if (foundResource && Vector2.Distance(collector.Position, bestTarget) <= 0.8f)
            {
                Vector2Int tile = WorldToTile(bestTarget);
                if (grid[tile.x, tile.y] == TileType.Resource)
                {
                    grid[tile.x, tile.y] = TileType.Empty;
                    AddItem("wood", "Wood", 1);
                    collector.CollectedCount += 1;
                    GrantAffinity(AffinityType.Nature, 5);
                }
            }
        }

        private void UpdateWorldDrops()
        {
            for (int i = 0; i < worldDrops.Count; i++)
            {
                WorldDrop drop = worldDrops[i];
                float bob = Mathf.Sin(Time.time * 3f + i) * 0.06f;
                drop.View.GameObject.transform.position = new Vector3(drop.Position.x, drop.Position.y + bob, 0f);
            }
        }

        private void PerformMeleeAction()
        {
            EnemyState? enemy = enemies.Where(e => e.Alive).OrderBy(e => Vector2.Distance(e.Position, player.Position)).FirstOrDefault();
            if (enemy == null || Vector2.Distance(enemy.Position, player.Position) > 1.6f)
            {
                AddLog("No melee target nearby.");
                return;
            }

            int damage = RollPlayerDamage(out bool crit);
            DamageEnemy(enemy, damage, false);
            GrantAffinity(AffinityType.Melee, 8);
            AddLog(crit ? $"Critical hit for {damage}." : $"Hit for {damage}.");
        }

        private void PerformFireAction()
        {
            Vector2Int tile = WorldToTile(GetMouseWorld());
            if (IsWithin(tile) && grid[tile.x, tile.y] == TileType.Solid)
            {
                grid[tile.x, tile.y] = TileType.Empty;
                GrantAffinity(AffinityType.Fire, 12);
                AddLog("Fire action destroyed a block.");
            }
            else
            {
                AddLog("No fire target under cursor.");
            }
        }

        private void PerformNatureAction()
        {
            Vector2Int tile = WorldToTile(GetMouseWorld());
            if (IsWithin(tile) && grid[tile.x, tile.y] == TileType.Empty)
            {
                grid[tile.x, tile.y] = TileType.Resource;
                GrantAffinity(AffinityType.Nature, 10);
                AddLog("Nature action planted a resource node.");
            }
            else
            {
                AddLog("Nature action needs an empty tile.");
            }
        }

        private void PerformArchetypeAbility()
        {
            string archetype = GetArchetype();
            string ability = GetArchetypeAbility();
            if (ability == "None")
            {
                AddLog("No archetype ability unlocked yet.");
                return;
            }

            switch (archetype)
            {
                case "Necromancer":
                case "Berserker":
                    EnemyState? enemy = enemies.Where(e => e.Alive).OrderBy(e => Vector2.Distance(e.Position, player.Position)).FirstOrDefault();
                    if (enemy != null && Vector2.Distance(enemy.Position, player.Position) <= 4f)
                    {
                        DamageEnemy(enemy, archetype == "Necromancer" ? 12 : 10, false);
                        AddLog($"Used {ability}.");
                    }
                    else
                    {
                        AddLog($"{ability} needs an enemy nearby.");
                    }
                    break;
                case "Rune Smith":
                case "Artisan":
                    GrantAffinity(AffinityType.Craft, archetype == "Artisan" ? 10 : 5);
                    AddLog($"Used {ability}.");
                    break;
                case "Pyromancer":
                    GrantAffinity(AffinityType.Fire, 8);
                    AddLog($"Used {ability}.");
                    break;
                default:
                    AddLog($"Used {ability}.");
                    break;
            }
        }

        private void OnGUI()
        {
            Rect hudRect = new(12f, 12f, 340f, 160f);
            Rect worldRect = new(12f, 180f, 340f, 190f);
            GUI.color = panelFlashTimer > 0f ? Color.Lerp(Color.white, panelFlashColor, 0.35f) : Color.white;
            GUI.Box(hudRect, "PLAYER");
            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Lv {player.Level}  HP {player.Health}/{player.MaxHealth}  XP {player.Xp}/{GetXpForNextLevel(player.Level)}");
            GUI.HorizontalScrollbar(new Rect(24f, 66f, 300f, 18f), 0f, Mathf.Max(1f, player.Health), 0f, Mathf.Max(1f, player.MaxHealth));
            GUI.Label(new Rect(24f, 88f, 300f, 22f), $"Weapon {GetWeapon(player.EquippedWeapon).Name}  DMG {GetPlayerDamage()}  Crit {Mathf.RoundToInt(GetPlayerCritChance() * 100f)}%");
            GUI.Label(new Rect(24f, 110f, 300f, 22f), $"Archetype {GetArchetype()}  Ability {GetArchetypeAbility()}");
            GUI.Label(new Rect(24f, 132f, 300f, 22f), $"ME {player.Affinities[AffinityType.Melee]}  CR {player.Affinities[AffinityType.Craft]}  FI {player.Affinities[AffinityType.Fire]}  NA {player.Affinities[AffinityType.Nature]}");

            GUI.Box(worldRect, "WORLD");
            GUI.Label(new Rect(24f, 208f, 320f, 66f), GetLogText());
            GUI.Label(new Rect(24f, 276f, 320f, 22f), $"Creature HP {creature.Health}/{creature.MaxHealth}  XP {creature.Xp}/{GetCreatureXpForNextLevel()}  DMG {creature.Damage}");
            GUI.Label(new Rect(24f, 300f, 320f, 22f), $"Collector gathered {collector.CollectedCount}  Drops {worldDrops.Count}");
            GUI.Label(new Rect(24f, 324f, 320f, 44f), $"Enemies: {string.Join(", ", enemies.Where(e => e.Alive).Select(e => $"{e.Kind} {e.Health}HP"))}");

            GUI.Box(new Rect(12f, Screen.height - 72f, Screen.width - 216f, 60f), "CONTROLS");
            GUI.Label(new Rect(24f, Screen.height - 46f, Screen.width - 240f, 22f), "WASD/Arrows move | Left Click mine | Space attack | F fire | R nature | E ability | G loot | I inventory | C craft");

            DrawMinimap();
            if (inventoryOpen) DrawInventoryWindow();
            if (craftingOpen) DrawCraftingWindow();
        }

        private void DamageEnemy(EnemyState enemy, int damage, bool fromCreature)
        {
            enemy.Health -= damage;
            if (enemy.Health > 0) return;

            enemy.Health = 0;
            enemy.Alive = false;
            enemy.View.GameObject.SetActive(false);
            spawnSlots[enemy.SpawnSlotIndex].Enemy = null;
            spawnSlots[enemy.SpawnSlotIndex].RespawnTimer = 5f;

            int xpReward = 10 + (enemy.Kind == EnemyKind.Kapre ? 4 : 0);
            if (!fromCreature)
            {
                player.Xp += xpReward;
                while (player.Xp >= GetXpForNextLevel(player.Level))
                {
                    player.Xp -= GetXpForNextLevel(player.Level);
                    player.Level += 1;
                    player.MaxHealth += 12;
                    player.Health = Mathf.Min(player.MaxHealth, player.Health + 12);
                    FlashPanel(new Color(0.49f, 0.88f, 1f));
                    AddLog($"Player reached level {player.Level}.");
                }
            }

            if (GetArchetype() == "Necromancer")
            {
                player.Health = Mathf.Min(player.MaxHealth, player.Health + 4);
            }

            SpawnEnemyDrop(enemy);
            AddLog($"{enemy.Kind} defeated. XP +{xpReward}.");
        }

        private void SpawnEnemyDrop(EnemyState enemy)
        {
            string itemId = UnityEngine.Random.value < 0.6f ? "wood" : "iron_ore";
            string name = itemId == "wood" ? "Wood" : "Iron Ore";
            int quantity = enemy.Kind == EnemyKind.Kapre ? 2 : 1;
            Color color = itemId == "wood" ? new Color(0.71f, 0.52f, 0.25f) : new Color(0.75f, 0.79f, 0.86f);
            worldDrops.Add(new WorldDrop
            {
                ItemId = itemId,
                Name = name,
                Quantity = quantity,
                Position = enemy.Position,
                View = CreateDropView($"Drop_{name}", color)
            });
        }

        private void CollectDropsNear(Vector2 actorPosition, float range, string collectorName)
        {
            for (int i = worldDrops.Count - 1; i >= 0; i--)
            {
                var drop = worldDrops[i];
                if (Vector2.Distance(actorPosition, drop.Position) > range) continue;

                AddItem(drop.ItemId, drop.Name, drop.Quantity);
                AddLog($"{collectorName} collected {drop.Name} x{drop.Quantity}.");
                Destroy(drop.View.GameObject);
                worldDrops.RemoveAt(i);
            }
        }

        private void AddItem(string itemId, string name, int quantity, int maxStack = 99)
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

        private bool RemoveItem(string itemId, int quantity)
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
            return true;
        }

        private int GetItemCount(string itemId)
        {
            return inventory.Where(item => item.Id == itemId).Sum(item => item.Quantity);
        }

        private void DrawInventoryWindow()
        {
            Rect window = new(Screen.width * 0.5f - 300f, Screen.height * 0.5f - 180f, 600f, 360f);
            GUILayout.Window(1001, window, _ =>
            {
                GUILayout.Label($"Equipped: {GetWeapon(player.EquippedWeapon).Name}");
                inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(250f));
                foreach (var item in inventory.ToList())
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label($"{item.Name} x{item.Quantity}", GUILayout.Width(220f));
                    if (IsWeaponItem(item.Id) && GUILayout.Button("Equip", GUILayout.Width(80f)))
                    {
                        EquipWeaponFromItem(item.Id);
                    }
                    if (GUILayout.Button("Drop", GUILayout.Width(80f)))
                    {
                        DropInventoryStack(item);
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                if (GUILayout.Button("Close")) inventoryOpen = false;
                GUI.DragWindow();
            }, "Inventory");
        }

        private void DrawCraftingWindow()
        {
            Rect window = new(Screen.width * 0.5f - 280f, Screen.height * 0.5f - 180f, 560f, 360f);
            GUILayout.Window(1002, window, _ =>
            {
                GUILayout.Label("Recipes");
                craftingScroll = GUILayout.BeginScrollView(craftingScroll, GUILayout.Height(270f));
                foreach (var recipe in recipes)
                {
                    bool canCraft = CanCraft(recipe);
                    GUILayout.BeginVertical("box");
                    GUILayout.Label($"{recipe.Name} {(canCraft ? "[READY]" : "[LOCKED]")}");
                    GUILayout.Label($"Ingredients: {string.Join(", ", recipe.Ingredients.Select(i => $"{i.ItemId} {GetItemCount(i.ItemId)}/{i.Quantity}"))}");
                    GUILayout.Label(recipe.RequiredAffinity.HasValue
                        ? $"Requirement: {recipe.RequiredAffinity.Value} {player.Affinities[recipe.RequiredAffinity.Value]}/{recipe.RequiredAmount}"
                        : "Requirement: None");
                    GUI.enabled = canCraft;
                    if (GUILayout.Button("Craft")) Craft(recipe);
                    GUI.enabled = true;
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
                if (GUILayout.Button("Close")) craftingOpen = false;
                GUI.DragWindow();
            }, "Crafting");
        }

        private void DrawMinimap()
        {
            Rect boxRect = new(Screen.width - 192f, Screen.height - 192f, 180f, 180f);
            GUI.Box(boxRect, "MINIMAP");
            Rect mapRect = new(Screen.width - 184f, Screen.height - 164f, 164f, 148f);
            GUI.BeginGroup(mapRect);
            float scaleX = mapRect.width / GridWidth;
            float scaleY = mapRect.height / GridHeight;

            for (int y = 0; y < GridHeight; y += 4)
            {
                for (int x = 0; x < GridWidth; x += 4)
                {
                    if (grid[x, y] == TileType.Empty) continue;
                    Color color = grid[x, y] == TileType.Solid ? new Color(0.56f, 0.61f, 0.65f) : new Color(0.34f, 0.76f, 0.44f);
                    DrawGuiRect(new Rect(x * scaleX, (GridHeight - y) * scaleY, scaleX * 4f, scaleY * 4f), color);
                }
            }

            foreach (var enemy in enemies.Where(e => e.Alive))
            {
                DrawGuiRect(new Rect(enemy.Position.x * scaleX, (GridHeight - enemy.Position.y) * scaleY, 3f, 3f), new Color(1f, 0.42f, 0.42f));
            }

            DrawGuiRect(new Rect(creature.Position.x * scaleX, (GridHeight - creature.Position.y) * scaleY, 4f, 4f), new Color(0.44f, 0.83f, 1f));
            DrawGuiRect(new Rect(player.Position.x * scaleX, (GridHeight - player.Position.y) * scaleY, 5f, 5f), new Color(1f, 0.94f, 0.54f));
            GUI.EndGroup();
        }

        private void DrawGuiRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private bool CanCraft(Recipe recipe)
        {
            if (recipe.RequiredAffinity.HasValue && player.Affinities[recipe.RequiredAffinity.Value] < recipe.RequiredAmount)
            {
                return false;
            }

            return recipe.Ingredients.All(ingredient => GetItemCount(ingredient.ItemId) >= ingredient.Quantity);
        }

        private void Craft(Recipe recipe)
        {
            if (!CanCraft(recipe))
            {
                AddLog($"Cannot craft {recipe.Name} yet.");
                return;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                RemoveItem(ingredient.ItemId, ingredient.Quantity);
            }

            AddItem(recipe.ResultItemId, recipe.Name, recipe.ResultQuantity);
            TryEquipCraftedWeapon(recipe.ResultItemId);
            GrantAffinity(AffinityType.Craft, 15);
            AddLog($"Crafted {recipe.Name}.");
        }

        private bool IsWeaponItem(string itemId)
        {
            return itemId == "wood_sword" || itemId == "iron_sword";
        }

        private void EquipWeaponFromItem(string itemId)
        {
            if (itemId == "wood_sword") player.EquippedWeapon = WeaponId.WoodSword;
            if (itemId == "iron_sword") player.EquippedWeapon = WeaponId.IronSword;
            SaveState();
        }

        private void TryEquipCraftedWeapon(string itemId)
        {
            if (itemId == "iron_sword") player.EquippedWeapon = WeaponId.IronSword;
            else if (itemId == "wood_sword" && player.EquippedWeapon == WeaponId.RustyDagger) player.EquippedWeapon = WeaponId.WoodSword;
        }

        private void DropInventoryStack(InventoryItem item)
        {
            inventory.Remove(item);
            Color color = item.Id == "iron_ore" ? new Color(0.75f, 0.79f, 0.86f) : new Color(0.71f, 0.52f, 0.25f);
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

        private WeaponDefinition GetWeapon(WeaponId id) => weaponDefinitions[id];

        private int GetPlayerDamage()
        {
            WeaponDefinition weapon = GetWeapon(player.EquippedWeapon);
            return weapon.BaseDamage + GetLevelDamageBonus() + GetMeleeAffinityBonus() + GetPassiveMeleeDamageBonus();
        }

        private int RollPlayerDamage(out bool crit)
        {
            crit = UnityEngine.Random.value < GetPlayerCritChance();
            int damage = GetPlayerDamage();
            if (crit)
            {
                damage = Mathf.RoundToInt(damage * GetWeapon(player.EquippedWeapon).CritMultiplier);
            }
            return damage;
        }

        private float GetPlayerCritChance()
        {
            float affinityBonus = player.Affinities[AffinityType.Melee] >= 80 ? 0.08f :
                player.Affinities[AffinityType.Melee] >= 40 ? 0.05f :
                player.Affinities[AffinityType.Melee] >= 20 ? 0.02f : 0f;
            return Mathf.Min(0.45f, GetWeapon(player.EquippedWeapon).CritChance + affinityBonus);
        }

        private int GetLevelDamageBonus() => player.Level - 1;

        private int GetMeleeAffinityBonus()
        {
            if (player.Affinities[AffinityType.Melee] >= 80) return 4;
            if (player.Affinities[AffinityType.Melee] >= 40) return 2;
            return 0;
        }

        private int GetPassiveMeleeDamageBonus()
        {
            return GetArchetype() switch
            {
                "Berserker" => 4,
                "Battle Smith" => 2,
                _ => 0
            };
        }

        private void GrantAffinity(AffinityType type, int amount)
        {
            player.Affinities[type] += amount;
        }

        private string GetArchetype()
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

        private string GetArchetypeAbility()
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

        private int GetXpForNextLevel(int level) => level * 25;
        private int GetCreatureXpForNextLevel() => creature.Level * 18;

        private void AddLog(string message)
        {
            logs.Insert(0, message);
            while (logs.Count > 4) logs.RemoveAt(logs.Count - 1);
        }

        private string GetLogText() => string.Join("\n", logs);

        private void FlashPanel(Color color)
        {
            panelFlashColor = color;
            panelFlashTimer = 0.35f;
        }

        private void TryMove(ref Vector2 current, Vector2 target)
        {
            Vector2 horizontal = new(target.x, current.y);
            if (IsWalkable(horizontal)) current.x = horizontal.x;

            Vector2 vertical = new(current.x, target.y);
            if (IsWalkable(vertical)) current.y = vertical.y;
        }

        private bool IsWalkable(Vector2 worldPosition)
        {
            Vector2Int tile = WorldToTile(worldPosition);
            return IsWithin(tile) && grid[tile.x, tile.y] != TileType.Solid;
        }

        private Vector2Int WorldToTile(Vector2 world) => new(Mathf.FloorToInt(world.x / TileSize), Mathf.FloorToInt(world.y / TileSize));
        private Vector2 TileToWorld(int x, int y) => new(x + 0.5f, y + 0.5f);

        private bool IsWithin(Vector2Int tile)
        {
            return tile.x >= 0 && tile.x < GridWidth && tile.y >= 0 && tile.y < GridHeight;
        }

        private Vector2 GetMouseWorld()
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
            return new Vector2(world.x, world.y);
        }

        private void ApplyPosition(ActorView view, Vector2 position)
        {
            view.GameObject.transform.position = new Vector3(position.x, position.y, 0f);
        }

        private void SaveState()
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
                Inventory = inventory.Select(item => new InventoryEntry { Id = item.Id, Name = item.Name, Quantity = item.Quantity, MaxStack = item.MaxStack }).ToList()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void LoadState()
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
                if (Enum.TryParse(affinity.Key, out AffinityType affinityType))
                {
                    player.Affinities[affinityType] = affinity.Value;
                }
            }

            inventory.Clear();
            foreach (var item in save.Inventory)
            {
                inventory.Add(new InventoryItem { Id = item.Id, Name = item.Name, Quantity = item.Quantity, MaxStack = item.MaxStack });
            }
        }
    }
}
