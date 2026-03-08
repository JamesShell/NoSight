using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a health bar UI using a Slider component.
/// Attach this to a GameObject with a Slider component.
/// </summary>
[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public LivingEntity targetEntity;  // The entity to track health for

    [Header("Settings")]
    public bool hideWhenFull = false;   // Hide health bar when at full health
    public bool smoothTransition = true;
    public float smoothSpeed = 5f;

    private Slider slider;
    private CanvasGroup canvasGroup;
    private float targetValue;

    void Awake()
    {
        slider = GetComponent<Slider>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // Auto-find player if no target set
        if (targetEntity == null)
        {
            targetEntity = FindObjectOfType<PlayerEntity>();
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
        }

        if (hideWhenFull && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        if (targetEntity == null || slider == null)
            return;

        // Calculate health percentage
        float healthPercent = Mathf.Clamp01(targetEntity.CurrentHealth / targetEntity.maxHealth);
        targetValue = healthPercent;

        // Update slider value
        if (smoothTransition)
        {
            slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * smoothSpeed);
        }
        else
        {
            slider.value = targetValue;
        }

        // Handle visibility
        if (hideWhenFull && canvasGroup != null)
        {
            bool isFull = healthPercent >= 0.99f;
            float targetAlpha = isFull ? 0f : 1f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * smoothSpeed);
        }
    }
}
