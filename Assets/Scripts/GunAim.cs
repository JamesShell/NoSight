using UnityEngine;

public class GunAim : MonoBehaviour
{
    public Transform player;         // assign your player transform
    public float gunDistance = 0.5f; // how far from player center
    public bool flipSprite = true;

    private Camera cam;
    private SpriteRenderer sr;

    void Start()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        // Get mouse world position
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - player.position).normalized;

        // Position gun around the player
        transform.position = player.position + (Vector3)(dir * gunDistance);

        // Rotate gun to look at mouse
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Flip sprite vertically if aiming left (so it doesn’t look upside down)
        if (flipSprite && sr != null)
        {
            sr.flipY = (angle > 90f || angle < -90f);
        }
    }
}
