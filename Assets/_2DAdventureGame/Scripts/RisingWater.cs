using UnityEngine;

/// <summary>
/// A moving hazard that rises until it reaches a world-space Y limit.
/// Keep the trigger attached to a kinematic Rigidbody2D so Unity updates
/// trigger contacts as the water moves.
/// </summary>
public class RisingWater : MonoBehaviour
{
    public bool active = true;
    public float riseSpeed = 0.2f;
    public float maxHeight = 5f;
    public int damage = 1;

    void Update()
    {
        if (!active || transform.position.y >= maxHeight)
            return;

        Vector3 position = transform.position;
        position.y = Mathf.MoveTowards(position.y, maxHeight, riseSpeed * Time.deltaTime);
        transform.position = position;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!active)
            return;

        PlayerController controller = other.GetComponent<PlayerController>();
        if (controller != null)
            controller.ChangeHealth(-damage);
    }
}
