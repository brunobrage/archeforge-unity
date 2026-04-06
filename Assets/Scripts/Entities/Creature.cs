using UnityEngine;

public class Creature : MonoBehaviour
{
    public float Speed = 120f;
    public float FollowDistance = 32f;
    public float TaskDuration = 1000f;

    private Rigidbody2D rb;
    private GridSystem gridSystem;
    private InventorySystem inventorySystem;
    private Player player;

    private Vector2Int? taskTile = null;
    private float taskProgress = 0;
    private int resourceCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        gridSystem = FindObjectOfType<GridSystem>();
        inventorySystem = FindObjectOfType<InventorySystem>();
        player = FindObjectOfType<Player>();
    }

    void Update()
    {
        if (gridSystem == null || player == null) return;

        // Find new task if current one is invalid
        if (taskTile == null || !gridSystem.IsSolid(taskTile.Value.x, taskTile.Value.y))
        {
            taskTile = FindNearestSolidTile();
            taskProgress = 0;
        }

        if (taskTile.HasValue)
        {
            PerformTask(Time.deltaTime);
        }
        else
        {
            FollowPlayer(Time.deltaTime);
        }
    }

    private void FollowPlayer(float deltaTime)
    {
        Vector3 diff = player.transform.position - transform.position;
        float distance = diff.magnitude;

        if (distance <= FollowDistance)
            return;

        Vector2 direction = diff.normalized;
        float velocity = Speed * deltaTime;
        transform.position = (Vector3)((Vector2)transform.position + direction * velocity);
        rb.velocity = Vector2.zero;
    }

    private void PerformTask(float deltaTime)
    {
        Vector3 targetPos = new Vector3(
            taskTile.Value.x * gridSystem.TileSize + gridSystem.TileSize / 2f,
            taskTile.Value.y * gridSystem.TileSize + gridSystem.TileSize / 2f,
            0
        );

        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance > 8)
        {
            MoveTo(targetPos, deltaTime);
            return;
        }

        taskProgress += deltaTime;
        if (taskProgress >= TaskDuration / 1000f)
        {
            gridSystem.BreakTile(taskTile.Value.x, taskTile.Value.y);

            // Add resources to inventory
            if (inventorySystem != null)
            {
                if (Random.value < 0.3f)
                {
                    inventorySystem.AddItem("iron_ore", "Iron Ore", "item_iron_ore", 1);
                }
                else
                {
                    inventorySystem.AddItem("wood", "Wood", "item_wood", 1);
                }
            }

            resourceCount++;
            taskTile = null;
            taskProgress = 0;
        }
    }

    private void MoveTo(Vector3 target, float deltaTime)
    {
        Vector3 diff = target - transform.position;
        Vector2 direction = diff.normalized;
        float velocity = Speed * deltaTime;
        transform.position = (Vector3)((Vector2)transform.position + direction * velocity);
        rb.velocity = Vector2.zero;
    }

    private Vector2Int? FindNearestSolidTile()
    {
        Vector2Int? best = null;
        float bestDistance = float.MaxValue;

        for (int y = 0; y < gridSystem.Height; y++)
        {
            for (int x = 0; x < gridSystem.Width; x++)
            {
                if (!gridSystem.IsSolid(x, y))
                    continue;

                float dx = x * gridSystem.TileSize + gridSystem.TileSize / 2f - transform.position.x;
                float dy = y * gridSystem.TileSize + gridSystem.TileSize / 2f - transform.position.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new Vector2Int(x, y);
                }
            }
        }

        return best;
    }

    public string GetTaskStatus()
    {
        if (taskTile == null)
            return "Idle";

        float percent = Mathf.Min(100, (taskProgress / (TaskDuration / 1000f)) * 100);
        return $"Mining {taskTile.Value.x},{taskTile.Value.y} ({Mathf.FloorToInt(percent)}%)";
    }
}
