using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalController : MonoBehaviour
{
    public string destinationScene = "BossArena";
    public Sprite[] animationFrames;
    public float frameRate = 9f;

    static PlayerController travellingPlayer;
    SpriteRenderer portalRenderer;
    Vector3 baseScale;
    bool isLoading;

    public static PlayerController ConsumeTravellingPlayer()
    {
        PlayerController player = travellingPlayer;
        travellingPlayer = null;
        return player;
    }

    void Awake()
    {
        portalRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (portalRenderer != null && animationFrames != null && animationFrames.Length > 0)
        {
            int frame = Mathf.FloorToInt(Time.time * Mathf.Max(1f, frameRate)) % animationFrames.Length;
            if (portalRenderer.sprite != animationFrames[frame])
                portalRenderer.sprite = animationFrames[frame];
        }

        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.035f;
        transform.localScale = baseScale * pulse;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player == null || isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError($"Scene '{destinationScene}' is missing from Build Settings.");
            return;
        }

        isLoading = true;
        travellingPlayer = player;
        DontDestroyOnLoad(player.gameObject);
        SceneManager.LoadScene(destinationScene);
    }
}