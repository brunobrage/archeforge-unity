using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
        using UnityEditor;
#endif


namespace Archeforge.UnityPort
{
    public class ArcheforgePrototypeController : ArcheforgePrototypeControllerBase
    {
        [Header("Editor Preview")]
        public bool enablePreview = false;
        private bool previewBuilt = false;
        
        public void BuildEditorPreview()
        {
            ClearPreview();

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 7.5f;

            whiteSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);

            grid = new TileType[GridWidth, GridHeight];
            biomeGrid = new TileBiome[GridWidth, GridHeight];
            materialGrid = new TileMaterial[GridWidth, GridHeight];
            tileViews = new TileView[GridWidth, GridHeight];

            player = new PlayerState();
            creature = new CreatureState();
            collector = new CollectorState();

            // 🔥 REUSANDO SEU FLOW
            GenerateGrid();
            CreateTileMapView();
            BuildSpawnSlots();
            CreateViews();
            SpawnInitialEnemies();

            previewBuilt = true;
        }

        public void ClearPreview()
        {
            foreach (Transform child in transform)
            {
                DestroyImmediate(child.gameObject);
            }

            enemies.Clear();
            worldDrops.Clear();
            spawnSlots.Clear();

            previewBuilt = false;
        }

        private void Update()
        {
#if UNITY_EDITOR
    if (!Application.isPlaying && enablePreview)
    {
        if (!previewBuilt)
        {
            BuildEditorPreview();
        }
    }
#endif

            if (!Application.isPlaying) return;

            // 🔥 seu Update original continua normal
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
                playerInput = Vector2.zero;
                ApplyPosition(playerView, player.Position);
            }

            UpdatePlayerPresentation(dt);
            ApplyPosition(playerView, player.Position);
            ApplyPosition(creatureView, creature.Position);
            ApplyPosition(collectorView, collector.Position);

            if (panelFlashTimer > 0f)
            {
                panelFlashTimer -= dt;
            }
        }

        private Canvas runtimeCanvas = null!;

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
            biomeGrid = new TileBiome[GridWidth, GridHeight];
            materialGrid = new TileMaterial[GridWidth, GridHeight];
            tileViews = new TileView[GridWidth, GridHeight];
            player = new PlayerState();
            creature = new CreatureState();
            collector = new CollectorState();

            GenerateGrid();
            CreateTileMapView();
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

        // private void Update()
        // {
        //     float dt = Time.deltaTime;

        //     HandleGlobalInput();
        //     UpdateCamera();
        //     UpdateSpawnRespawns(dt);
        //     UpdateEnemies(dt);
        //     UpdateCreature(dt);
        //     UpdateCollector(dt);
        //     UpdateWorldDrops();

        //     if (!inventoryOpen && !craftingOpen)
        //     {
        //         HandleMovement(dt);
        //         HandleActions();
        //         HandleMining();
        //     }
        //     else
        //     {
        //         playerInput = Vector2.zero;
        //         ApplyPosition(playerView, player.Position);
        //     }

        //     UpdatePlayerPresentation(dt);
        //     ApplyPosition(playerView, player.Position);
        //     ApplyPosition(creatureView, creature.Position);
        //     ApplyPosition(collectorView, collector.Position);

        //     if (panelFlashTimer > 0f)
        //     {
        //         panelFlashTimer -= dt;
        //     }
        // }

        private void OnApplicationQuit()
        {
            SaveState();
        }

        private void BuildSpawnSlots()
        {
            spawnSlots.Clear();
            AddSpawnSlot(new Vector2(12f, 10f), 3.4f);
            AddSpawnSlot(new Vector2(18f, 28f), 3.8f);
            AddSpawnSlot(new Vector2(30f, 72f), 4.1f);
            AddSpawnSlot(new Vector2(56f, 22f), 4.0f);
            AddSpawnSlot(new Vector2(63f, 54f), 4.2f);
            AddSpawnSlot(new Vector2(82f, 18f), 4.4f);
            AddSpawnSlot(new Vector2(88f, 36f), 4.1f);
            AddSpawnSlot(new Vector2(42f, 44f), 3.9f);
        }

        private void AddSpawnSlot(Vector2 center, float radius)
        {
            Vector2Int tile = WorldToTile(center);
            TileBiome biome = IsWithin(tile) ? biomeGrid[tile.x, tile.y] : TileBiome.Plains;
            spawnSlots.Add(new SpawnSlot
            {
                Kind = GetEnemyKindForBiome(biome),
                Center = center,
                Radius = radius
            });
        }

        private EnemyKind GetEnemyKindForBiome(TileBiome biome)
        {
            return biome switch
            {
                TileBiome.Forest => UnityEngine.Random.value < 0.5f ? EnemyKind.Chicken : EnemyKind.DwendeBlue,
                TileBiome.Ruins => UnityEngine.Random.value < 0.55f ? EnemyKind.Kapre : EnemyKind.Santelmo,
                TileBiome.IronWastes => UnityEngine.Random.value < 0.55f ? EnemyKind.Tikbalang : EnemyKind.Kapre,
                TileBiome.Ember => UnityEngine.Random.value < 0.6f ? EnemyKind.Santelmo : EnemyKind.Tikbalang,
                _ => UnityEngine.Random.value < 0.5f ? EnemyKind.Chicken : EnemyKind.Tikbalang
            };
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
                View = CreateEnemyView(kind, color)
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
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedHotbarIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) selectedHotbarIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) selectedHotbarIndex = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) selectedHotbarIndex = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) selectedHotbarIndex = 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) selectedHotbarIndex = 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) selectedHotbarIndex = 6;
            if (Input.GetKeyDown(KeyCode.Alpha8)) selectedHotbarIndex = 7;

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
            playerInput = input;
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
            if (Input.GetMouseButtonDown(1))
            {
                TryPlaceBlockFromHotbar();
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;

            Vector2Int tile = WorldToTile(GetMouseWorld());
            if (!IsWithin(tile)) return;

            if (grid[tile.x, tile.y] == TileType.Solid)
            {
                if (IsPlacedBlock(materialGrid[tile.x, tile.y]))
                {
                    string itemId = GetPlacedBlockItemId(materialGrid[tile.x, tile.y]);
                    AddItem(itemId, GetDisplayName(itemId), 1);
                    grid[tile.x, tile.y] = TileType.Empty;
                    materialGrid[tile.x, tile.y] = GetDefaultMaterial(biomeGrid[tile.x, tile.y], TileType.Empty);
                    RefreshTileVisual(tile.x, tile.y);
                    AddLog($"Recovered {GetDisplayName(itemId)} block.");
                }
                else
                {
                    ItemDrop minedDrop = GetBiomeMineDrop(tile.x, tile.y);
                    grid[tile.x, tile.y] = TileType.Resource;
                    materialGrid[tile.x, tile.y] = GetDefaultMaterial(biomeGrid[tile.x, tile.y], TileType.Resource);
                    RefreshTileVisual(tile.x, tile.y);
                    GrantAffinity(AffinityType.Craft, 10);
                    AddItem(minedDrop.ItemId, minedDrop.Name, 1);
                    AddLog($"Mined {minedDrop.Name}.");
                }
            }
        }

        private void TryPlaceBlockFromHotbar()
        {
            string itemId = hotbarItemIds[selectedHotbarIndex];
            if (string.IsNullOrEmpty(itemId))
            {
                AddLog("Selected hotbar slot is empty.");
                return;
            }

            if (!IsPlaceableItem(itemId))
            {
                AddLog($"{GetDisplayName(itemId)} cannot be placed.");
                return;
            }

            Vector2Int tile = WorldToTile(GetMouseWorld());
            if (!IsWithin(tile))
            {
                return;
            }

            if (grid[tile.x, tile.y] != TileType.Empty)
            {
                AddLog("Can only place a block on an empty tile.");
                return;
            }

            Vector2 tileCenter = TileToWorld(tile.x, tile.y);
            if (Vector2.Distance(tileCenter, player.Position) > 4f)
            {
                AddLog("Too far away to place a block.");
                return;
            }

            if (!RemoveItem(itemId, 1))
            {
                AddLog($"No {GetDisplayName(itemId)} left in inventory.");
                return;
            }

            grid[tile.x, tile.y] = TileType.Solid;
            materialGrid[tile.x, tile.y] = GetPlacedMaterial(itemId);
            RefreshTileVisual(tile.x, tile.y);
            AddLog($"{GetDisplayName(itemId)} block placed.");
            CleanupHotbarReferences(itemId);
            SaveState();
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
                    SetFacing(enemy.View, dir.x);
                }

                ApplyPosition(enemy.View, enemy.Position);
            }
        }

        private void UpdateCreature(float dt)
        {
            creature.AttackCooldown -= dt;
            creature.WorkCooldown -= dt;

            EnemyState? target = enemies
                .Where(e => e.Alive)
                .OrderBy(e => Mathf.Min(Vector2.Distance(e.Position, creature.Position), Vector2.Distance(e.Position, player.Position)))
                .FirstOrDefault();

            bool enemyNearCreature = target != null && Vector2.Distance(target.Position, creature.Position) < 6.5f;
            bool enemyNearPlayer = target != null && Vector2.Distance(target.Position, player.Position) < 5.5f;

            if (target != null && (enemyNearCreature || enemyNearPlayer))
            {
                Vector2 dir = (target.Position - creature.Position).normalized;
                TryMove(ref creature.Position, creature.Position + dir * 4.4f * dt);
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
                TryCreatureMineNearPlayer(dt);
            }

            CollectDropsNear(creature.Position, 1.4f, "Creature");
        }

        private void TryCreatureMineNearPlayer(float dt)
        {
            if (TryFindNearestSolidTileAroundPlayer(6, out Vector2Int mineTile, out Vector2 standPosition))
            {
                Vector2 toTarget = standPosition - creature.Position;
                if (toTarget.sqrMagnitude > 0.06f)
                {
                    TryMove(ref creature.Position, creature.Position + toTarget.normalized * 3.6f * dt);
                    return;
                }

                if (creature.WorkCooldown <= 0f)
                {
                    ItemDrop minedDrop = GetBiomeMineDrop(mineTile.x, mineTile.y);
                    creature.WorkCooldown = 0.65f;
                    grid[mineTile.x, mineTile.y] = TileType.Empty;
                    materialGrid[mineTile.x, mineTile.y] = GetDefaultMaterial(biomeGrid[mineTile.x, mineTile.y], TileType.Empty);
                    RefreshTileVisual(mineTile.x, mineTile.y);
                    GrantAffinity(AffinityType.Craft, 6);
                    AddItem(minedDrop.ItemId, minedDrop.Name, 1);
                    AddLog($"Creature mined {minedDrop.Name}.");
                }

                return;
            }

            Vector2 follow = player.Position + new Vector2(-1.3f, -0.2f);
            Vector2 dir = follow - creature.Position;
            if (dir.sqrMagnitude > 0.04f)
            {
                TryMove(ref creature.Position, creature.Position + dir.normalized * 3.8f * dt);
            }
        }

        private bool TryFindNearestSolidTileAroundPlayer(int radius, out Vector2Int mineTile, out Vector2 standPosition)
        {
            mineTile = default;
            standPosition = default;

            Vector2Int playerTile = WorldToTile(player.Position);
            float bestScore = float.MaxValue;
            bool found = false;

            for (int y = playerTile.y - radius; y <= playerTile.y + radius; y++)
            {
                for (int x = playerTile.x - radius; x <= playerTile.x + radius; x++)
                {
                    Vector2Int tile = new(x, y);
                    if (!IsWithin(tile) || grid[x, y] != TileType.Solid)
                    {
                        continue;
                    }

                    if (!TryGetClosestWalkableAdjacent(tile, out Vector2 candidateStandPosition))
                    {
                        continue;
                    }

                    float playerDistance = Vector2.Distance(TileToWorld(x, y), player.Position);
                    float creatureDistance = Vector2.Distance(candidateStandPosition, creature.Position) * 0.35f;
                    float score = playerDistance + creatureDistance;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        mineTile = tile;
                        standPosition = candidateStandPosition;
                        found = true;
                    }
                }
            }

            return found;
        }

        private bool TryGetClosestWalkableAdjacent(Vector2Int solidTile, out Vector2 standPosition)
        {
            standPosition = default;
            Vector2[] offsets =
            {
                new Vector2(1f, 0f),
                new Vector2(-1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, -1f)
            };

            float bestDistance = float.MaxValue;
            bool found = false;

            foreach (Vector2 offset in offsets)
            {
                Vector2 candidate = TileToWorld(solidTile.x, solidTile.y) + offset;
                if (!IsWalkable(candidate))
                {
                    continue;
                }

                float distance = Vector2.Distance(candidate, creature.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    standPosition = candidate;
                    found = true;
                }
            }

            return found;
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
                    ItemDrop gatheredDrop = GetBiomeMineDrop(tile.x, tile.y);
                    grid[tile.x, tile.y] = TileType.Empty;
                    materialGrid[tile.x, tile.y] = GetDefaultMaterial(biomeGrid[tile.x, tile.y], TileType.Empty);
                    RefreshTileVisual(tile.x, tile.y);
                    AddItem(gatheredDrop.ItemId, gatheredDrop.Name, 1);
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
                materialGrid[tile.x, tile.y] = GetDefaultMaterial(biomeGrid[tile.x, tile.y], TileType.Empty);
                RefreshTileVisual(tile.x, tile.y);
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
                materialGrid[tile.x, tile.y] = GetDefaultMaterial(biomeGrid[tile.x, tile.y], TileType.Resource);
                RefreshTileVisual(tile.x, tile.y);
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
            GUI.Label(new Rect(24f, Screen.height - 46f, Screen.width - 240f, 22f), "WASD/Arrows move | Left Click mine | Right Click place | 1-8 hotbar | Space attack | F fire | R nature | E ability | G loot | I inventory | C craft");

            DrawMinimap();
            DrawHotbar();
            if (inventoryOpen) DrawInventoryWindow();
            if (craftingOpen) DrawCraftingWindow();
        }

        private void DrawHotbar()
        {
            float slotSize = 56f;
            float spacing = 8f;
            float totalWidth = hotbarItemIds.Length * slotSize + (hotbarItemIds.Length - 1) * spacing;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - 146f;

            GUI.Box(new Rect(startX - 12f, y - 28f, totalWidth + 24f, 88f), "HOTBAR");

            for (int i = 0; i < hotbarItemIds.Length; i++)
            {
                float x = startX + i * (slotSize + spacing);
                Rect slotRect = new(x, y, slotSize, slotSize);
                GUI.color = i == selectedHotbarIndex ? new Color(1f, 0.95f, 0.55f) : Color.white;
                GUI.Box(slotRect, string.Empty);
                GUI.color = Color.white;

                string itemId = hotbarItemIds[i];
                if (!string.IsNullOrEmpty(itemId))
                {
                    GUI.Label(new Rect(x + 5f, y + 6f, slotSize - 10f, 22f), GetDisplayName(itemId));
                    GUI.Label(new Rect(x + 5f, y + 28f, slotSize - 10f, 18f), $"x{GetItemCount(itemId)}");
                }

                GUI.Label(new Rect(x + 21f, y + 38f, 20f, 18f), (i + 1).ToString());
            }

            string selectedItemId = hotbarItemIds[selectedHotbarIndex];
            string selectedText = string.IsNullOrEmpty(selectedItemId)
                ? "Empty slot"
                : $"{GetDisplayName(selectedItemId)} {(IsPlaceableItem(selectedItemId) ? "[Placeable]" : "[Item]")}";
            GUI.Label(new Rect(startX, y + slotSize + 2f, totalWidth, 20f), $"Selected: {selectedText}");
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
            ItemDrop biomeDrop = GetBiomeEnemyDrop(enemy.Position);
            int quantity = enemy.Kind == EnemyKind.Kapre ? 2 : 1;
            worldDrops.Add(new WorldDrop
            {
                ItemId = biomeDrop.ItemId,
                Name = biomeDrop.Name,
                Quantity = quantity,
                Position = enemy.Position,
                View = CreateDropView($"Drop_{biomeDrop.Name}", biomeDrop.Color)
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
                    if (GUILayout.Button("Bar", GUILayout.Width(60f)))
                    {
                        AssignItemToSelectedHotbar(item.Id);
                    }
                    if (IsWeaponItem(item.Id) && GUILayout.Button("Equip", GUILayout.Width(80f)))
                    {
                        EquipWeaponFromItem(item.Id);
                    }
                    if (GUILayout.Button("Drop", GUILayout.Width(70f)))
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

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (grid[x, y] == TileType.Empty) continue;
                    DrawGuiRect(new Rect(x * scaleX, (GridHeight - y - 1) * scaleY, scaleX, scaleY), GetMinimapTileColor(x, y));
                }
            }

            foreach (var enemy in enemies.Where(e => e.Alive))
            {
                DrawGuiRect(new Rect(enemy.Position.x * scaleX - 1f, (GridHeight - enemy.Position.y) * scaleY - 1f, 3f, 3f), new Color(1f, 0.42f, 0.42f));
            }

            DrawGuiRect(new Rect(creature.Position.x * scaleX - 1.5f, (GridHeight - creature.Position.y) * scaleY - 1.5f, 4f, 4f), new Color(0.44f, 0.83f, 1f));
            DrawGuiRect(new Rect(player.Position.x * scaleX - 2f, (GridHeight - player.Position.y) * scaleY - 2f, 5f, 5f), new Color(1f, 0.94f, 0.54f));
            GUI.EndGroup();
        }

        private void DrawGuiRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

    }
}
