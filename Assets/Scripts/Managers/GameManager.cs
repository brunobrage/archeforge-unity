using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas uiCanvas;

    private GridSystem gridSystem;
    private AffinitySystem affinitySystem;
    private InventorySystem inventorySystem;
    private CraftingSystem craftingSystem;
    private PersistenceSystem persistenceSystem;

    private Player player;
    private Creature creature;
    private Collector collector;
    private List<Enemy> enemies = new List<Enemy>();

    private Text statusText;
    private Text logText;
    private Text inventoryText;

    private Graphics gridRenderer;

    void Awake()
    {
        Debug.Log("[GameManager] Initializing game systems...");

        // Initialize all systems
        gridSystem = GetComponent<GridSystem>();
        affinitySystem = GetComponent<AffinitySystem>();
        inventorySystem = GetComponent<InventorySystem>();
        craftingSystem = GetComponent<CraftingSystem>();
        persistenceSystem = GetComponent<PersistenceSystem>();

        if (gridSystem == null) gridSystem = gameObject.AddComponent<GridSystem>();
        if (affinitySystem == null) affinitySystem = gameObject.AddComponent<AffinitySystem>();
        if (inventorySystem == null) inventorySystem = gameObject.AddComponent<InventorySystem>();
        if (craftingSystem == null) craftingSystem = gameObject.AddComponent<CraftingSystem>();
        if (persistenceSystem == null) persistenceSystem = gameObject.AddComponent<PersistenceSystem>();

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Start()
    {
        Debug.Log("[GameManager] Starting game...");

        // Create player
        GameObject playerGO = new GameObject("Player");
        playerGO.AddComponent<SpriteRenderer>().color = Color.blue;
        playerGO.transform.position = new Vector3(400, 300, 0);
        player = playerGO.AddComponent<Player>();

        // Create creature
        GameObject creatureGO = new GameObject("Creature");
        creatureGO.AddComponent<SpriteRenderer>().color = Color.cyan;
        creatureGO.transform.position = new Vector3(200, 200, 0);
        creature = creatureGO.AddComponent<Creature>();

        // Create collector
        GameObject collectorGO = new GameObject("Collector");
        collectorGO.AddComponent<SpriteRenderer>().color = Color.green;
        collectorGO.transform.position = new Vector3(100, 200, 0);
        collector = collectorGO.AddComponent<Collector>();

        // Create enemies
        for (int i = 0; i < 5; i++)
        {
            GameObject enemyGO = new GameObject($"Enemy_{i}");
            enemyGO.AddComponent<SpriteRenderer>().color = Color.red;
            enemyGO.transform.position = new Vector3(500 + i * 200, 200 + i * 100, 0);
            Enemy enemy = enemyGO.AddComponent<Enemy>();
            enemies.Add(enemy);
        }

        // Set up camera
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.GetComponent<CameraFollow>().SetTarget(player.transform);

        Debug.Log("[GameManager] Game initialization complete!");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleMeleeAction();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            HandleFireAction();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            HandleNatureAction();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleArchetypeAbility();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ShowCraftingMenu();
        }

        UpdateUI();
    }

    private void HandleLeftClick()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        int tileX = Mathf.FloorToInt(mouseWorldPos.x / gridSystem.TileSize);
        int tileY = Mathf.FloorToInt(mouseWorldPos.y / gridSystem.TileSize);

        if (gridSystem.IsSolid(tileX, tileY))
        {
            gridSystem.BreakTile(tileX, tileY);
            affinitySystem.AddXp("craft", 10);

            if (Random.value < 0.3f)
            {
                inventorySystem.AddItem("iron_ore", "Iron Ore", "item_iron_ore", 1);
                affinitySystem.AddLog("Mined Iron Ore!");
            }
            else
            {
                inventorySystem.AddItem("wood", "Wood", "item_wood", 1);
                affinitySystem.AddLog("Mined Wood!");
            }
        }
    }

    private void HandleMeleeAction()
    {
        Enemy target = FindClosestEnemy();
        if (target != null)
        {
            int damage = affinitySystem.GetMeleeDamage();
            target.TakeDamage(damage);
            affinitySystem.AddXp("melee", 8);
            affinitySystem.AddLog($"Hit {target.Type} for {damage} damage!");
        }
    }

    private void HandleFireAction()
    {
        Debug.Log("[GameManager] Fire action triggered");
        affinitySystem.AddXp("fire", 12);
        affinitySystem.AddLog("Fire action executed!");
    }

    private void HandleNatureAction()
    {
        Debug.Log("[GameManager] Nature action triggered");
        affinitySystem.AddXp("nature", 10);
        affinitySystem.AddLog("Nature action executed!");
    }

    private void HandleArchetypeAbility()
    {
        string ability = affinitySystem.GetArchetypeAbility();
        if (ability != "None")
        {
            Debug.Log($"[GameManager] Using ability: {ability}");
            affinitySystem.AddLog($"Used {ability}!");
        }
    }

    private void ShowCraftingMenu()
    {
        Debug.Log("[GameManager] Crafting menu opened");
    }

    private void UpdateUI()
    {
        // TODO: Implement UI updates
    }

    private Enemy FindClosestEnemy()
    {
        Enemy closest = null;
        float closestDistance = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive()) continue;

            float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }
}

public class CameraFollow : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 pos = target.position;
            pos.z = -10;
            transform.position = pos;
        }
    }
}
