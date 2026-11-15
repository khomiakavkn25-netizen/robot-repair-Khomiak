using UnityEngine;

public class BossController : MonoBehaviour
{
    public int requiredHits = 15;
    public GameObject projectilePrefab;
    public float movementSpeed = 1.2f;
    public float preferredDistance = 3.5f;
    public float minimumDistance = 2f;
    public float fireRate = 1.5f;
    public float fireRange = 14f;
    public float strafeSpeed = 0.65f;
    public float strafeChangeInterval = 2.5f;
    public Sprite[] walkingFrames;
    public Sprite attackFrame;
    public float animationFrameRate = 8f;
    public float attackPoseDuration = 0.3f;
    public float defeatAnimationDuration = 1.1f;
    public Vector2 arenaMin;
    public Vector2 arenaMax;

    public int hitsTaken { get; private set; }
    public bool isDefeated { get; private set; }
    public bool isDefeatAnimationComplete { get; private set; }

    Rigidbody2D body;
    Animator animator;
    SpriteRenderer bossRenderer;
    Sprite restingSprite;
    Transform target;
    float fireTimer;
    float walkingAnimationTimer;
    float attackPoseTimer;
    float strafeTimer;
    Vector2 moveDirection;
    bool isMoving;
    float strafeDirection = 1f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator != null && !animator.enabled)
            animator = null;
        bossRenderer = GetComponent<SpriteRenderer>();
        if (bossRenderer != null)
            restingSprite = bossRenderer.sprite;
    }

    void Start()
    {
        FindTarget();
        fireTimer = 0.8f;
        strafeTimer = strafeChangeInterval;
        if (UIHandler.instance != null)
            UIHandler.instance.SetBossProgress(hitsTaken, requiredHits);
    }

    void Update()
    {
        if (isDefeated)
            return;
        if (target == null)
            FindTarget();
        if (target == null)
            return;

        Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
        float distance = offset.magnitude;
        Vector2 aimDirection = distance > 0.01f ? offset / distance : Vector2.up;
        if (animator != null)
        {
            animator.SetFloat("Move X", aimDirection.x);
            animator.SetFloat("Move Y", aimDirection.y);
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f && distance <= fireRange)
        {
            FireAt(aimDirection);
            fireTimer = Mathf.Max(0.2f, fireRate);
        }

        UpdateAppearance(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (isDefeated || target == null || body == null)
            return;

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;
        if (distance < 0.01f)
            moveDirection = Vector2.zero;
        else if (distance > preferredDistance)
            moveDirection = offset / distance;
        else if (distance < minimumDistance)
            moveDirection = -offset / distance;
        else
        {
            strafeTimer -= Time.fixedDeltaTime;
            if (strafeTimer <= 0f)
            {
                strafeDirection *= -1f;
                strafeTimer = Mathf.Max(0.5f, strafeChangeInterval);
            }

            Vector2 tangent = new Vector2(-offset.y, offset.x).normalized;
            moveDirection = tangent * strafeDirection
                * (strafeSpeed / Mathf.Max(0.1f, movementSpeed));
        }

        isMoving = moveDirection.sqrMagnitude > 0.01f;

        Vector2 next = body.position + moveDirection * movementSpeed * Time.fixedDeltaTime;
        if (arenaMax.x > arenaMin.x && arenaMax.y > arenaMin.y)
        {
            next.x = Mathf.Clamp(next.x, arenaMin.x, arenaMax.x);
            next.y = Mathf.Clamp(next.y, arenaMin.y, arenaMax.y);
        }
        body.MovePosition(next);
    }

    void FindTarget()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            target = player.transform;
    }

    void FireAt(Vector2 direction)
    {
        attackPoseTimer = Mathf.Max(0f, attackPoseDuration);
        Vector3 origin = transform.position + (Vector3)(direction * 0.8f);
        GameObject shot;
        if (projectilePrefab != null)
        {
            shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
        }
        else
        {
            // Keep the boss fight playable even when the optional prefab has not been built yet.
            shot = new GameObject("Boss Carrot Projectile");
            shot.transform.position = origin;
            shot.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
            renderer.sprite = BossProjectile.CreateFallbackSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            CircleCollider2D collider = shot.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            Rigidbody2D projectileBody = shot.AddComponent<Rigidbody2D>();
            projectileBody.gravityScale = 0f;
            projectileBody.freezeRotation = true;
            shot.AddComponent<BossProjectile>();
        }

        BossProjectile projectile = shot.GetComponent<BossProjectile>();
        if (projectile != null)
            projectile.Launch(direction);
    }

    void UpdateAppearance(float deltaTime)
    {
        if (bossRenderer == null)
            return;

        if (attackPoseTimer > 0f && attackFrame != null)
        {
            attackPoseTimer -= deltaTime;
            bossRenderer.sprite = attackFrame;
            return;
        }

        if (isMoving && walkingFrames != null && walkingFrames.Length > 0)
        {
            walkingAnimationTimer += deltaTime;
            int frame = Mathf.FloorToInt(walkingAnimationTimer * Mathf.Max(1f, animationFrameRate))
                % walkingFrames.Length;
            bossRenderer.sprite = walkingFrames[frame] != null ? walkingFrames[frame] : restingSprite;
            return;
        }

        walkingAnimationTimer = 0f;
        bossRenderer.sprite = restingSprite;
    }

    public void TakeProjectileHit()
    {
        if (isDefeated)
            return;

        hitsTaken = Mathf.Min(hitsTaken + 1, requiredHits);
        if (UIHandler.instance != null)
            UIHandler.instance.SetBossProgress(hitsTaken, requiredHits);

        if (hitsTaken >= requiredHits)
        {
            isDefeated = true;
            isMoving = false;
            attackPoseTimer = 0f;
            if (bossRenderer != null)
                bossRenderer.sprite = restingSprite;
            if (body != null)
                body.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetTrigger("Fixed");
            StartCoroutine(PlayDefeatAnimation());
        }
    }

    System.Collections.IEnumerator PlayDefeatAnimation()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
            collider.enabled = false;

        if (bossRenderer == null)
        {
            isDefeatAnimationComplete = true;
            yield break;
        }

        if (attackFrame != null)
            bossRenderer.sprite = attackFrame;

        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;
        Color startColor = bossRenderer.color;
        float duration = Mathf.Max(0.2f, defeatAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float wobble = Mathf.Sin(elapsed * 18f) * (1f - progress) * 0.08f;
            transform.localScale = Vector3.Scale(startScale,
                new Vector3(1f + wobble, Mathf.Lerp(1f, 0.18f, progress), 1f));
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, 72f * progress);

            Color tint = startColor;
            tint.a = 1f - progress;
            if (elapsed < 0.18f && Mathf.FloorToInt(elapsed * 30f) % 2 == 0)
                tint = Color.white;
            bossRenderer.color = tint;

            elapsed += Time.deltaTime;
            yield return null;
        }

        bossRenderer.enabled = false;
        isDefeatAnimationComplete = true;
    }
}
