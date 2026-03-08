using UnityEngine;

// To get gradient fade effect on edges:
// 1. Create a Material in Unity using the RadialGradient shader
// 2. Assign this Material to the SpriteRenderer component
// 3. Adjust "Edge Fade" and "Fade Power" properties in the Material Inspector
[RequireComponent(typeof(SpriteRenderer))]
public class EchoWave : MonoBehaviour
{
    public float maxRadius = 6f;       // world radius at peak
    public float duration = 0.4f;      // total duration of the pulse
    public float expandRatio = 0.6f;   // 0-1: how much of duration is expansion (rest is contraction)
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private float timer = 0f;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Pulse effect: scale up then down
        float scaleT;
        if (t < expandRatio)
        {
            // Expanding phase (0 -> 1)
            scaleT = t / expandRatio;
        }
        else
        {
            // Contracting phase (1 -> 0)
            scaleT = 1f - ((t - expandRatio) / (1f - expandRatio));
        }

        // Apply scale curve for smooth easing
        float curveValue = scaleCurve.Evaluate(scaleT);
        float scale = curveValue * maxRadius * 2f;
        transform.localScale = new Vector3(scale, scale, 1f);

        // Fade alpha using curve
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alphaCurve.Evaluate(t);
            sr.color = c;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
