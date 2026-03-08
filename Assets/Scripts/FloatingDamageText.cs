using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    public float lifeTime = 0.6f;
    public float floatSpeed = 1f;
    public Vector3 floatDirection = new Vector3(0f, 1f, 0f);
    public float maxScaleBoost = 0.25f; // pulse amount

    private float timer;
    private TMP_Text tmpText;
    private Vector3 initialScale;

    void Awake()
    {
        tmpText = GetComponentInChildren<TMP_Text>();
        initialScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifeTime);

        // Move upwards
        transform.position += floatDirection * floatSpeed * Time.deltaTime;

        // Scale pulse (0 -> 1 -> 0)
        float pulse = Mathf.Sin(t * Mathf.PI);
        float scaleFactor = 1f + pulse * maxScaleBoost;
        transform.localScale = initialScale * scaleFactor;

        // Fade out
        if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = 1f - t;
            tmpText.color = c;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
