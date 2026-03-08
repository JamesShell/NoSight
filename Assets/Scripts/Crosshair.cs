using UnityEngine;

public class Crosshair : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        Cursor.visible = false; // hide system cursor
    }

    void Update()
    {
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        transform.position = mouseWorld;
    }
}
