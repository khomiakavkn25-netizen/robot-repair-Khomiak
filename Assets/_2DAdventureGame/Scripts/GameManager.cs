using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int requiredRepairsBeforePortal = 4;
    public GameObject portalPrefab;
    public GameObject portalInstance;
    public Vector2 portalSpawnPosition;

    PlayerController player;
    EnemyController[] enemies;
    BossController boss;
    bool gameEnded;
    bool portalSpawned;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        boss = FindFirstObjectByType<BossController>();
        if (portalInstance != null)
            portalInstance.SetActive(false);
    }

    void Update()
    {
        if (gameEnded || player == null || UIHandler.instance == null || Time.timeScale == 0f)
            return;

        if (player.health <= 0)
        {
            EndGame(false);
            return;
        }

        if (boss != null)
        {
            if (boss.isDefeated)
            {
                if (!boss.isDefeatAnimationComplete)
                {
                    player.enabled = false;
                    return;
                }
                EndGame(true);
            }
            return;
        }

        if (!portalSpawned && enemies.Length >= requiredRepairsBeforePortal && AllEnemiesFixed())
        {
            if (portalInstance != null)
                portalInstance.SetActive(true);
            else if (portalPrefab != null)
                portalInstance = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
            else
                return;

            portalSpawned = true;
            Debug.Log("All four robots repaired. The portal to the boss arena has appeared.");
        }
    }

    bool AllEnemiesFixed()
    {
        foreach (EnemyController enemy in enemies)
            if (!enemy.isFixed)
                return false;
        return true;
    }

    void EndGame(bool won)
    {
        gameEnded = true;
        if (won)
        {
            UIHandler.instance.DisplayWinScreen();
            if (player != null)
                player.enabled = false;
        }
        else
        {
            UIHandler.instance.DisplayLoseScreen();
        }
    }
}
