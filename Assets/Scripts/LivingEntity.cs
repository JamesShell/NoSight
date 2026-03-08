using System.Collections;
using UnityEngine;

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float headshotMultiplier = 2f;   // extra damage on headshot

    [Header("Death Animation")]
    public float animatorFreezeDelay = 1f;  // seconds after death before animator freezes

    [Header("Flags (read-only)")]
    public bool isDead = false;

    public float currentHealth;
    protected Animator anim;

    // Public property to access current health (for UI, etc.)
    public float CurrentHealth => currentHealth;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Generic damage entry. isHeadshot makes this hit do more damage and
    /// is passed through to the death logic if it kills.
    /// </summary>
    public virtual void TakeDamage(float amount, bool isHeadshot = false)
    {
        if (isDead) return;

        float finalDamage = isHeadshot ? amount * headshotMultiplier : amount;
        currentHealth -= finalDamage;

        // Play hit reaction
        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }

        OnDamaged(finalDamage, isHeadshot);

        if (currentHealth <= 0f)
        {
            Die(isHeadshot);
        }
    }

    /// <summary>
    /// Override for per-entity "I got hurt but not dead" behavior.
    /// </summary>
    protected virtual void OnDamaged(float damage, bool isHeadshot) { }

    /// <summary>
    /// Handles the generic death flag and Dead bool, then calls OnDeath for child classes.
    /// </summary>
    protected virtual void Die(bool headshot)
    {
        if (isDead) return;
        isDead = true;

        if (anim != null)
        {
            anim.SetBool("Dead", true);
        }

        OnDeath(headshot);

        // Start coroutine to freeze animator after delay
        if (anim != null && animatorFreezeDelay > 0f)
        {
            StartCoroutine(FreezeAnimatorAfterDelay());
        }
    }

    /// <summary>
    /// Waits for specified delay, then sets animator speed to 0 to freeze the death pose.
    /// </summary>
    protected virtual IEnumerator FreezeAnimatorAfterDelay()
    {
        yield return new WaitForSeconds(animatorFreezeDelay);

        if (anim != null)
        {
            anim.speed = 0f;
        }
    }

    /// <summary>
    /// Implement per-entity death behavior (disable controls, play specific animation, etc.)
    /// headshot == true if the LAST hit that killed was flagged as headshot.
    /// </summary>
    protected abstract void OnDeath(bool headshot);
}
