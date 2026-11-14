using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public float smoothTime = 0.12f;
    Vector3 velocity;
    Camera cameraComponent;

    void Awake() { cameraComponent = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 next = Vector3.SmoothDamp(transform.position,
            new Vector3(target.position.x, target.position.y, transform.position.z),
            ref velocity, smoothTime);
        if (cameraComponent != null && cameraComponent.orthographic)
        {
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;
            float minX = minBounds.x + halfWidth;
            float maxX = maxBounds.x - halfWidth;
            float minY = minBounds.y + halfHeight;
            float maxY = maxBounds.y - halfHeight;
            next.x = minX > maxX ? (minBounds.x + maxBounds.x) * 0.5f : Mathf.Clamp(next.x, minX, maxX);
            next.y = minY > maxY ? (minBounds.y + maxBounds.y) * 0.5f : Mathf.Clamp(next.y, minY, maxY);
        }
        transform.position = next;
    }
}
