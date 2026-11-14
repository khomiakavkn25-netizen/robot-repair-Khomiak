using UnityEngine;
public class NonPlayerCharacter : MonoBehaviour
{
    bool bubbleVisible;
    public void SetBubbleVisible(bool visible) { bubbleVisible = visible; }
    void OnGUI()
    {
        if (!bubbleVisible || Camera.main == null) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.8f);
        if (screen.z <= 0) return;
        Rect rect = new Rect(screen.x - 24, Screen.height - screen.y - 22, 48, 36);
        GUI.color = new Color(1f, 0.88f, 0.35f);
        GUI.Box(rect, "X");
        GUI.color = Color.white;
    }
}
