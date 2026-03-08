using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerEntity : LivingEntity
{
    [Header("Player Death")]
    public string deathTriggerName = "Die";   // optional: or leave empty and use only Dead bool

    protected override void OnDeath(bool headshot)
    {
        // Play death anim
        if (anim != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            anim.SetTrigger(deathTriggerName);
        }

        // Disable player control scripts
        var move = GetComponent<PlayerMovement>();
        if (move != null) move.enabled = false;

        var shooter = GetComponentInChildren<GunShooter>();
        if (shooter != null) shooter.enabled = false;

        // Notify level manager of player death
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnPlayerDeath();
        }
    }
}
