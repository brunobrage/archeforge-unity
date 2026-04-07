using UnityEngine;

public class Guardian : MonoBehaviour
{
    public int Health { get; set; } = 50;
    public float PatrolSpeed = 80f;
    public float DetectionRange = 300f;

    private Rigidbody2D rb;
    private Player player;
    private Vector2 patrolDirection = Vector2.right;
    private bool isAlive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        player = FindObjectOfType<Player>();
    }

    void Update()
    {
        if (!isAlive || player == null) return;

        Vector3 diff = player.transform.position - transform.position;
        float distance = diff.magnitude;

        if (distance < DetectionRange)
        {
            Chase(diff.normalized, Time.deltaTime);
        }
        else
        {
            Patrol(Time.deltaTime);
        }
    }

    private void Chase(Vector2 direction, float deltaTime)
    {
        float velocity = PatrolSpeed * 1.5f * deltaTime;
        transform.position = (Vector3)((Vector2)transform.position + direction * velocity);
        rb.linearVelocity = Vector2.zero;
    }

    private void Patrol(float deltaTime)
    {
        float velocity = PatrolSpeed * deltaTime;
        transform.position = (Vector3)((Vector2)transform.position + patrolDirection * velocity);
        rb.linearVelocity = Vector2.zero;

        // Change direction occasionally
        if (Random.value < 0.01f)
        {
            patrolDirection = Random.insideUnitCircle.normalized;
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        Debug.Log($"[Guardian] Took {damage} damage. Health: {Health}");

        if (Health <= 0)
        {
            isAlive = false;
            gameObject.SetActive(false);
            Debug.Log("[Guardian] Defeated!");
        }
    }

    public bool IsAlive()
    {
        return isAlive && Health > 0;
    }
}
