using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    static Sprite fallbackSprite;

    public float speed = 5f;
    public int damage = 1;
    public float lifetime = 7f;
    Rigidbody2D body;

    public static Sprite CreateFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Boss carrot projectile sprite";
        texture.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 color = new Color32(0, 0, 0, 0);
                bool carrotOutline = InTriangle(x, y, 16, 30, 4, 9, 28, 9);
                bool carrotBody = InTriangle(x, y, 16, 27, 7, 10, 25, 10);
                if (carrotOutline)
                    color = new Color32(117, 49, 22, 255);
                if (carrotBody)
                {
                    color = x < 14
                        ? new Color32(255, 164, 45, 255)
                        : new Color32(245, 126, 32, 255);
                    if (y > 14 && y < 25 && x == 12)
                        color = new Color32(255, 190, 66, 255);
                    if ((y == 17 && x >= 13 && x <= 17)
                        || (y == 22 && x >= 11 && x <= 15))
                        color = new Color32(196, 76, 26, 255);
                }

                bool leftLeaf = InTriangle(x, y, 14, 11, 3, 3, 8, 0);
                bool centerLeaf = InTriangle(x, y, 15, 11, 12, 0, 18, 2);
                bool rightLeaf = InTriangle(x, y, 18, 11, 29, 3, 22, 0);
                if (leftLeaf || centerLeaf || rightLeaf)
                {
                    bool edge = x == 3 || x == 29 || y == 0 || y == 1;
                    color = edge
                        ? new Color32(25, 79, 43, 255)
                        : new Color32(48, 143, 61, 255);
                    if ((x + y) % 4 == 0)
                        color = new Color32(111, 198, 74, 255);
                }

                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
        return fallbackSprite;
    }

    static bool InTriangle(float x, float y,
        float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
        float d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
        float d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    void Awake() { body = GetComponent<Rigidbody2D>(); }
    void Start() { Destroy(gameObject, lifetime); }

    public void Launch(Vector2 direction)
    {
        Vector2 normalizedDirection = direction.normalized;
        transform.rotation = Quaternion.FromToRotation(Vector3.down, normalizedDirection);
        if (body != null)
            body.linearVelocity = normalizedDirection * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BossController>() != null)
            return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            player.ChangeHealth(-damage);
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<BossController>() == null)
            Destroy(gameObject);
    }
}
