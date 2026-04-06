using UnityEngine;

public class Collector : MonoBehaviour
{
    public float Speed = 150f;
    public float FollowDistance = 50f;
    public float CollectionRange = 64f;

    private Rigidbody2D rb;
    private GridSystem gridSystem;
    private InventorySystem inventorySystem;
    private Player player;

    public int CollectedCount { get; set; } = 0;
    private float collectionCooldown = 0;

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

        collectionCooldown -= Time.deltaTime;

        // Find and collect nearby resources
        if (collectionCooldown <= 0)
        {
            Vector2Int playerTile = new Vector2Int(
                Mathf.FloorToInt(player.transform.position.x / gridSystem.TileSize),
                Mathf.FloorToInt(player.transform.position.y / gridSystem.TileSize)
            );

            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = playerTile.x + dx;
                    int y = playerTile.y + dy;

                    if (gridSystem.CollectResource(x, y))
                    {
                        CollectedCount++;
                        collectionCooldown = 0.5f;
                        break;
                    }
                }
            }
        }

        FollowPlayer(Time.deltaTime);
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

    public int Update(Player player, GridSystem gridSystem, float deltaTime, InventorySystem inventory)
    {
        int collected = 0;
        collectionCooldown -= deltaTime;

        if (collectionCooldown <= 0)
        {
            Vector2Int playerTile = new Vector2Int(
                Mathf.FloorToInt(player.transform.position.x / gridSystem.TileSize),
                Mathf.FloorToInt(player.transform.position.y / gridSystem.TileSize)
            );

            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = playerTile.x + dx;
                    int y = playerTile.y + dy;

                    if (gridSystem.CollectResource(x, y))
                    {
                        collected++;
                        collectionCooldown = 0.5f;
                    }
                }
            }
        }

        FollowPlayer(deltaTime);
        return collected;
    }

    public string GetTaskStatus()
    {
        return $"Collected: {CollectedCount}";
    }
}
