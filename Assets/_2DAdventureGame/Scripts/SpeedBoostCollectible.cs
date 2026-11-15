using System.Collections;
using UnityEngine;

public class SpeedBoostCollectible : MonoBehaviour
{
    public float boostFactor = 1.5f;
    public float boostTime = 5f;

    bool consumed;
    SpriteRenderer itemRenderer;
    Collider2D itemCollider;

    void Awake()
    {
        itemRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed)
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            StartCoroutine(ApplyBoost(player));
    }

    IEnumerator ApplyBoost(PlayerController player)
    {
        consumed = true;
        if (itemRenderer != null)
            itemRenderer.enabled = false;
        if (itemCollider != null)
            itemCollider.enabled = false;

        float baseSpeed = player.speed;
        player.speed = baseSpeed * Mathf.Max(1f, boostFactor);
        yield return new WaitForSeconds(Mathf.Max(0f, boostTime));

        if (player != null)
            player.speed = baseSpeed;
        Destroy(gameObject);
    }
}
