using UnityEngine;

public class BossArenaSpawn : MonoBehaviour
{
    public Vector3 entryPosition;

    void Awake()
    {
        PlayerController traveller = PortalController.ConsumeTravellingPlayer();
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (traveller == null)
        {
            foreach (PlayerController player in players)
            {
                if (player.gameObject.scene.name == "BossArena")
                {
                    traveller = player;
                    break;
                }
            }
        }

        if (traveller == null)
            return;

        foreach (PlayerController player in players)
        {
            if (player != null && player != traveller)
            {
                player.gameObject.SetActive(false);
                Destroy(player.gameObject);
            }
        }

        traveller.enabled = true;
        traveller.transform.position = entryPosition;
        Rigidbody2D body = traveller.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;

        Camera camera = Camera.main;
        if (camera != null)
        {
            CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
            if (follow != null)
                follow.target = traveller.transform;
        }
    }

    void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && UIHandler.instance != null)
            UIHandler.instance.SetHealthValue(player.health / (float)player.maxHealth);
    }
}
