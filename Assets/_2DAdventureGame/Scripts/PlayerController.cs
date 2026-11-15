using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction LaunchAction = new InputAction(
        "Launch", InputActionType.Button, "<Keyboard>/c");
    public GameObject projectilePrefab;
    public AudioClip hitSound;
    public AudioClip healSound;
    public AudioClip shootSound;
    public InputAction TalkAction = new InputAction(
        "Talk", InputActionType.Button, "<Keyboard>/x");
    NonPlayerCharacter lastNonPlayerCharacter;
    Rigidbody2D rigidbody2d;
    Animator animator;
    SpriteRenderer spriteRenderer;
    Color originalSpriteColor;
    Coroutine feedbackRoutine;

    Vector2 move;
    Vector2 moveDirection = new Vector2(1, 0);
    public float speed = 3.0f;
    public float launchCooldown = 0.3f;
    float launchCooldownTimer;

    public int maxHealth = 5;
    public int health { get { return currentHealth; } }
    int currentHealth;

    public float timeInvincible = 2.0f;
    bool isInvincible;
    float damageCooldown;

    void Start()
    {
        MoveAction.Enable();
        LaunchAction.Enable();
        TalkAction.Enable();
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        move = MoveAction.ReadValue<Vector2>();

        if (!Mathf.Approximately(move.x, 0.0f)
            || !Mathf.Approximately(move.y, 0.0f))
        {
            moveDirection.Set(move.x, move.y);
            moveDirection.Normalize();
        }

        animator.SetFloat("Look X", moveDirection.x);
        animator.SetFloat("Look Y", moveDirection.y);
        animator.SetFloat("Speed", move.magnitude);

        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;

            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }

        if (LaunchAction.WasPressedThisFrame())
        {
            if (launchCooldownTimer <= 0.0f)
            {
                Launch();
                launchCooldownTimer = launchCooldown;
            }
        }

        launchCooldownTimer -= Time.deltaTime;

        RaycastHit2D hit = Physics2D.Raycast(
            rigidbody2d.position + Vector2.up * 0.2f,
            moveDirection, 1.5f, LayerMask.GetMask("NPC"));
        if (hit.collider != null)
        {
            NonPlayerCharacter npc = hit.collider.GetComponent<NonPlayerCharacter>();
            if (npc != null)
            {
                npc.SetBubbleVisible(true);
                lastNonPlayerCharacter = npc;
                if (TalkAction.WasPressedThisFrame() && UIHandler.instance != null)
                    UIHandler.instance.DisplayDialogue();
            }
        }
        else if (lastNonPlayerCharacter != null)
        {
            lastNonPlayerCharacter.SetBubbleVisible(false);
            lastNonPlayerCharacter = null;
        }
    }

    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position
            + move * speed * Time.deltaTime;

        rigidbody2d.MovePosition(position);
    }

    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            if (isInvincible)
            {
                return;
            }

            isInvincible = true;
            damageCooldown = timeInvincible;
            animator.SetTrigger("Hit");
            PlayFeedback(hitSound, new Color(1f, 0.38f, 0.32f));
        }
        else if (amount > 0)
        {
            PlayFeedback(healSound, new Color(0.35f, 1f, 0.56f));
        }

        currentHealth = Mathf.Clamp(currentHealth + amount,
            0, maxHealth);

        if (UIHandler.instance != null)
            UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);
    }

    void Launch()
    {
        PlaySound(shootSound);
        GameObject projectileObject = Instantiate(
            projectilePrefab,
            rigidbody2d.position + Vector2.up * 0.5f,
            Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300.0f, transform);
        animator.SetTrigger("Launch");
    }

    void PlayFeedback(AudioClip clip, Color flashColor)
    {
        PlaySound(clip);
        if (spriteRenderer == null)
            return;
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FlashSprite(flashColor));
    }

    System.Collections.IEnumerator FlashSprite(Color flashColor)
    {
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.07f);
            spriteRenderer.color = originalSpriteColor;
            yield return new WaitForSeconds(0.07f);
        }
        feedbackRoutine = null;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
    }
}
