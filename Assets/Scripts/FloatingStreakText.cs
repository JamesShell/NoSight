using UnityEngine;
using TMPro;

public class FloatingStreakText : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;               // usually the player
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    [Header("Timing")]
    public float lifeTime = 1.2f;

    [Header("Animation")]
    public float maxScaleBoost = 0.35f;    // bigger pulse than damage

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

        // Follow target
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        // Scale pulse
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
