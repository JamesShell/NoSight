using UnityEngine;

public class FreezeOnFirstLoop : StateMachineBehaviour
{
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // normalizedTime: 0..1 first loop, >1 after
        if (stateInfo.normalizedTime >= 1f)
        {
            animator.speed = 0f; // freeze on last frame
        }
    }
}
