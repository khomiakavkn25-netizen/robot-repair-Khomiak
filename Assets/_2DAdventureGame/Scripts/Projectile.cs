using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rigidbody2d;
    Transform owner;

    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float distanceFromOwner = owner != null
            ? Vector3.Distance(transform.position, owner.position)
            : transform.position.magnitude;

        if (distanceFromOwner > 100.0f)
            Destroy(gameObject);
    }

    public void Launch(Vector2 direction, float force)
    {
        rigidbody2d.AddForce(direction * force);
    }

    public void Launch(Vector2 direction, float force, Transform projectileOwner)
    {
        owner = projectileOwner;
        Launch(direction, force);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        BossController boss = other.GetComponent<BossController>();
        if (boss == null)
            boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            boss.TakeProjectileHit();
            Destroy(gameObject);
            return;
        }

        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyController>();
        if (enemy != null)
            enemy.Fix();

        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}