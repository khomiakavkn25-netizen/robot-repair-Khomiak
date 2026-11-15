using UnityEngine;

/// <summary>Gently pulses the damage-zone warning light without moving its collider.</summary>
public class DamageZonePulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    [Range(0.25f, 1f)] public float minimumOpacity = 0.65f;
    [Range(0.25f, 1f)] public float maximumOpacity = 1f;

    SpriteRenderer zoneRenderer;
    Color originalColor;

    void Awake()
    {
        zoneRenderer = GetComponent<SpriteRenderer>();
        if (zoneRenderer != null)
            originalColor = zoneRenderer.color;
    }

    void Update()
    {
        if (zoneRenderer == null)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        Color color = originalColor;
        color.a *= Mathf.Lerp(minimumOpacity, maximumOpacity, pulse);
        zoneRenderer.color = color;
    }
}
