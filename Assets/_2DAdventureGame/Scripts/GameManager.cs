using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    PlayerController player;
    EnemyController[] enemies;
    bool gameEnded;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (gameEnded || player == null || UIHandler.instance == null) return;
        if (player.health <= 0) EndGame(false);
        else if (enemies.Length > 0 && AllEnemiesFixed()) EndGame(true);
    }

    bool AllEnemiesFixed()
    {
        foreach (EnemyController enemy in enemies)
            if (enemy.isBroken) return false;
        return true;
    }

    void EndGame(bool won)
    {
        gameEnded = true;
        if (won) UIHandler.instance.DisplayWinScreen();
        else UIHandler.instance.DisplayLoseScreen();
        Invoke(nameof(ReloadScene), 3f);
    }

    void ReloadScene() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
}
