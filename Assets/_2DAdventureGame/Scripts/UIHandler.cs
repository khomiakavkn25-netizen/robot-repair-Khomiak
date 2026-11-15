using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class RuntimeUIEventSystem
{
    public static void EnsurePresent()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Object.Destroy(legacyModule);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance { get; private set; }
    public float displayTime = 4f;
    VisualElement healthBar, dialogue, winScreen, loseScreen, bossCounter, startMenu;
    Button startGameButton;
    bool gameStarted;
    float dialogueTimer = -1f;

    void Awake()
    {
        Time.timeScale = 1f;
        RuntimeUIEventSystem.EnsurePresent();
        instance = this;
    }

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        healthBar = root.Q<VisualElement>("HealthBar");
        dialogue = root.Q<VisualElement>("NPCDialogue");
        winScreen = root.Q<VisualElement>("WinScreenContainer");
        loseScreen = root.Q<VisualElement>("LoseScreenContainer");
        bossCounter = root.Q<VisualElement>("BossCounter");
        startMenu = root.Q<VisualElement>("StartMenu");
        startGameButton = root.Q<Button>("StartGameButton");
        if (startGameButton != null) startGameButton.clicked += BeginGame;
        Button restartWin = root.Q<Button>("RestartWinButton");
        Button restartLose = root.Q<Button>("RestartLoseButton");
        if (restartWin != null) restartWin.clicked += RestartCampaign;
        if (restartLose != null) restartLose.clicked += RestartCampaign;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        SetHealthValue(player != null ? player.health / (float)player.maxHealth : 1f);
        if (SceneManager.GetActiveScene().name == "MyLevel")
        {
            Time.timeScale = 0f;
            if (startMenu != null) startMenu.style.display = DisplayStyle.Flex;
        }
        else if (startMenu != null)
        {
            startMenu.style.display = DisplayStyle.None;
        }
        if (dialogue != null)
            dialogue.style.display = DisplayStyle.None;

        if (SceneManager.GetActiveScene().name == "BossArena")
        {
            if (bossCounter != null)
                bossCounter.style.display = DisplayStyle.Flex;
            BossController boss = FindFirstObjectByType<BossController>();
            if (boss != null)
                SetBossProgress(boss.hitsTaken, boss.requiredHits);
        }
        else if (bossCounter != null)
        {
            bossCounter.style.display = DisplayStyle.None;
        }
    }

    public void SetHealthValue(float percentage)
    {
        if (healthBar != null)
            healthBar.style.width = Length.Percent(Mathf.Clamp01(percentage) * 100f);
    }

    public void SetBossProgress(int hits, int required)
    {
        if (bossCounter is Label label)
        {
            label.text = $"АРЕНА БОСА · Влучання: {hits} / {required}";
            label.style.display = DisplayStyle.Flex;
        }
    }

    void Update()
    {
        if (!gameStarted && startMenu != null && startMenu.resolvedStyle.display != DisplayStyle.None)
        {
            bool startPressed = Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.spaceKey.wasPressedThisFrame);

            if (!startPressed && Mouse.current != null && startGameButton != null
                && Mouse.current.leftButton.wasPressedThisFrame && startMenu.panel != null)
            {
                Vector2 panelPointer = RuntimePanelUtils.ScreenToPanel(
                    startMenu.panel, Mouse.current.position.ReadValue());
                startPressed = startGameButton.worldBound.Contains(panelPointer);
            }

            if (startPressed)
                BeginGame();
            return;
        }

        if (dialogueTimer <= 0)
            return;
        dialogueTimer -= Time.deltaTime;
        if (dialogueTimer <= 0 && dialogue != null)
            dialogue.style.display = DisplayStyle.None;
    }

    public void DisplayDialogue()
    {
        if (dialogue == null)
            return;
        dialogue.style.display = DisplayStyle.Flex;
        dialogueTimer = displayTime;
    }

    public void DisplayWinScreen()
    {
        if (winScreen != null)
            winScreen.style.opacity = 1f;
    }

    void BeginGame()
    {
        if (gameStarted)
            return;
        gameStarted = true;
        Time.timeScale = 1f;
        if (startMenu != null)
            startMenu.style.display = DisplayStyle.None;
    }

    void RestartCampaign()
    {
        if (SceneManager.GetActiveScene().name == "BossArena")
        {
            PlayerController[] travellingPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController travellingPlayer in travellingPlayers)
                if (travellingPlayer.gameObject.scene.name == "DontDestroyOnLoad")
                    Destroy(travellingPlayer.gameObject);
        }
        SceneManager.LoadScene("MyLevel");
    }

    public void DisplayLoseScreen()
    {
        if (loseScreen != null)
            loseScreen.style.opacity = 1f;
    }
}
