using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ZombieEntity : LivingEntity
{
    [Header("Zombie Death Animations")]
    public string normalDeathTrigger = "Die";
    public string headshotDeathTrigger = "DieHeadshot";

    protected override void OnDeath(bool headshot)
    {
        CombatFeedbackManager.Instance?.RegisterKill(headshot);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UnregisterZombie(this);
        }

        if (anim != null)
        {
            // Set Dead bool (for state machine transitions)
            anim.SetBool("Dead", true);
            

            // Choose which death anim to play based on last hit
            string triggerToUse = headshot ? headshotDeathTrigger : normalDeathTrigger;
            if (!string.IsNullOrEmpty(triggerToUse))
            {
                anim.SetTrigger(triggerToUse);
            }

            if (headshot)
            {
                AudioManager.Instance.PlaySfxAtPosition("Headshot", transform.position);
            }

            AudioManager.Instance.PlaySfxAtPosition("Zombie/Death", transform.position);
        }

        // Disable AI / movement
        var hearing = GetComponent<ZombieHearing>();
        if (hearing != null) hearing.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // or rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable colliders later (so corpse stays for a bit)
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        
        // Destroy game object after a delay
        Destroy(gameObject, 5f);
    }

    protected override void OnDamaged(float damage, bool isHeadshot)
    {
        AudioManager.Instance.PlaySfxAtPosition("Zombie/Hit", transform.position);

        // You could add special logic like staggering on headshot here
        // if (isHeadshot) { ... }

        
        float finalDamage = isHeadshot ? damage * 2 : damage;
        currentHealth -= finalDamage;

        // 🔥 Show damage popup
        CombatFeedbackManager.Instance?.ShowDamage(transform.position, finalDamage, isHeadshot);
    }
}
