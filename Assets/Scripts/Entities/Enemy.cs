using UnityEngine;

public enum EnemyType { Goblin, Orc, Skeleton }

public class Enemy : MonoBehaviour
{
    public EnemyType Type { get; set; }
    public int Health { get; set; } = 20;
    public float Speed = 100f;
    public float DetectionRange = 200f;
    public float AttackRange = 50f;
    public float AttackCooldown = 1f;

    private Rigidbody2D rb;
    private Player player;
    private float attackTimer = 0;
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

        // Randomize enemy type
        Type = (EnemyType)Random.Range(0, 3);
    }

    void Update()
    {
        if (!isAlive || player == null) return;

        attackTimer -= Time.deltaTime;

        Vector3 diff = player.transform.position - transform.position;
        float distance = diff.magnitude;

        if (distance < DetectionRange)
        {
            if (distance < AttackRange && attackTimer <= 0)
            {
                Attack();
                attackTimer = AttackCooldown;
            }
            else
            {
                Chase(diff.normalized, Time.deltaTime);
            }
        }
    }

    private void Chase(Vector2 direction, float deltaTime)
    {
        float velocity = Speed * deltaTime;
        transform.position = (Vector3)((Vector2)transform.position + direction * velocity);
        rb.linearVelocity = Vector2.zero;
    }

    private void Attack()
    {
        Debug.Log($"[Enemy] {Type} attacks!");
        // TODO: Deal damage to player
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        Debug.Log($"[Enemy] {Type} took {damage} damage. Health: {Health}");

        if (Health <= 0)
        {
            isAlive = false;
            gameObject.SetActive(false);
            Debug.Log($"[Enemy] {Type} defeated!");
        }
    }

    public bool IsAlive()
    {
        return isAlive && Health > 0;
    }

    public string GetTypeName()
    {
        return Type.ToString();
    }
}
