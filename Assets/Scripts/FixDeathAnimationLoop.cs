using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FixDeathAnimationsLoop : MonoBehaviour
{
    [Tooltip("Exact names of the death clips you want to stop looping (e.g. 'Zombie_Death', 'Zombie_HeadshotDeath')")]
    public string[] clipNamesToFix;

    void Awake()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null) return;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null || clipNamesToFix == null || clipNamesToFix.Length == 0)
            return;

        // For fast lookup
        HashSet<string> targetNames = new HashSet<string>(clipNamesToFix);

        foreach (var clip in controller.animationClips)
        {
            if (clip == null) continue;

            if (targetNames.Contains(clip.name))
            {
                // Legacy style safety
                clip.wrapMode = WrapMode.Once;

                Debug.Log($"[FixDeathAnimationsLoop] Set '{clip.name}' to not loop.", clip);
            }
        }
    }
}
