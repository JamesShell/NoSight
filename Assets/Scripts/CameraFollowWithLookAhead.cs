using UnityEngine;

public class CameraFollowWithLookAhead : MonoBehaviour
{
    public Transform target;            
    public float smoothSpeed = 8f;      
    public Vector3 baseOffset = new Vector3(0, 0, -10f);

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmooth = 4f;

    private Camera cam;
    private Vector3 lookAheadOffset;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Mouse world position
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // Direction from player to mouse
        Vector3 dir = (mouseWorld - target.position);
        dir.z = 0f;

        // Smoothly move look-ahead
        Vector3 targetLookAhead = dir.normalized * lookAheadDistance;
        lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSmooth * Time.deltaTime);

        // Final desired position
        Vector3 desired = target.position + baseOffset + lookAheadOffset;

        // Smooth the camera movement
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
