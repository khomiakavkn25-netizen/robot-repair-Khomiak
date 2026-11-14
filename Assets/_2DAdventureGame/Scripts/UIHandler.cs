using UnityEngine;
using UnityEngine.UIElements;

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance { get; private set; }
    public float displayTime = 4f;
    VisualElement healthBar, dialogue, winScreen, loseScreen;
    float dialogueTimer = -1f;

    void Awake() { instance = this; }
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        healthBar = root.Q<VisualElement>("HealthBar");
        dialogue = root.Q<VisualElement>("NPCDialogue");
        winScreen = root.Q<VisualElement>("WinScreenContainer");
        loseScreen = root.Q<VisualElement>("LoseScreenContainer");
        SetHealthValue(1f);
        dialogue.style.display = DisplayStyle.None;
    }
    public void SetHealthValue(float percentage)
    {
        if (healthBar != null) healthBar.style.width = Length.Percent(Mathf.Clamp01(percentage) * 100f);
    }
    void Update()
    {
        if (dialogueTimer <= 0) return;
        dialogueTimer -= Time.deltaTime;
        if (dialogueTimer <= 0 && dialogue != null) dialogue.style.display = DisplayStyle.None;
    }
    public void DisplayDialogue()
    {
        if (dialogue == null) return;
        dialogue.style.display = DisplayStyle.Flex;
        dialogueTimer = displayTime;
    }
    public void DisplayWinScreen() { if (winScreen != null) winScreen.style.opacity = 1f; }
    public void DisplayLoseScreen() { if (loseScreen != null) loseScreen.style.opacity = 1f; }
}
