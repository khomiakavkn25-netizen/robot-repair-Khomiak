using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 1.0f;
    public bool vertical;
    public float changeTime = 3.0f;

    Rigidbody2D rigidbody2d;
    Animator animator;
    float timer;
    int direction = 1;
    bool broken = true;

    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        timer = changeTime;
    }

    void Update()
    {
        if (!broken)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0.0f)
        {
            direction = -direction;
            timer = changeTime;
        }
    }

    void FixedUpdate()
    {
        if (!broken)
        {
            return;
        }

        Vector2 position = rigidbody2d.position;
        if (vertical)
        {
            position.y += speed * direction * Time.fixedDeltaTime;
            animator.SetFloat("Move X", 0.0f);
            animator.SetFloat("Move Y", direction);
        }
        else
        {
            position.x += speed * direction * Time.fixedDeltaTime;
            animator.SetFloat("Move X", direction);
            animator.SetFloat("Move Y", 0.0f);
        }

        rigidbody2d.MovePosition(position);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!broken)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ChangeHealth(-1);
        }
    }

    public void Fix()
    {
        if (!broken)
        {
            return;
        }

        broken = false;
        rigidbody2d.simulated = false;
        animator.SetTrigger("Fixed");
    }
}
