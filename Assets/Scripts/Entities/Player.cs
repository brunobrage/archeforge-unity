using UnityEngine;

public class Player : MonoBehaviour
{
    public float Speed = 200f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private GridSystem gridSystem;

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
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveWithCollision();
    }

    private void HandleInput()
    {
        movement = Vector2.zero;

        if (Input.GetKey(KeyCode.LeftArrow))
            movement.x -= 1;
        if (Input.GetKey(KeyCode.RightArrow))
            movement.x += 1;
        if (Input.GetKey(KeyCode.UpArrow))
            movement.y += 1;
        if (Input.GetKey(KeyCode.DownArrow))
            movement.y -= 1;

        movement = movement.normalized;
    }

    private void MoveWithCollision()
    {
        if (gridSystem == null) return;

        float velocity = Speed * Time.fixedDeltaTime;
        Vector3 currentPos = transform.position;
        Vector3 nextPos = currentPos + (Vector3)movement * velocity;

        float halfWidth = GetComponent<SpriteRenderer>().bounds.size.x * 0.5f;
        float halfHeight = GetComponent<SpriteRenderer>().bounds.size.y * 0.5f;

        // Try moving horizontally
        if (gridSystem.IsAreaFree(nextPos.x - halfWidth, currentPos.y - halfHeight, GetComponent<SpriteRenderer>().bounds.size.x, GetComponent<SpriteRenderer>().bounds.size.y))
        {
            currentPos.x = nextPos.x;
        }

        // Try moving vertically
        if (gridSystem.IsAreaFree(currentPos.x - halfWidth, nextPos.y - halfHeight, GetComponent<SpriteRenderer>().bounds.size.x, GetComponent<SpriteRenderer>().bounds.size.y))
        {
            currentPos.y = nextPos.y;
        }

        transform.position = currentPos;
        rb.linearVelocity = Vector2.zero;
    }
}
