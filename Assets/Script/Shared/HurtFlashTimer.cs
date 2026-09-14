using UnityEngine;

// Shared "isHurt" animator flash used by both PlayerDamageReceiver and EnemyHealth.
public sealed class HurtFlashTimer
{
    private readonly Animator animator;
    private readonly string boolParameter;
    private readonly int frameDuration;
    private int framesRemaining;

    public HurtFlashTimer(Animator animator, string boolParameter, int frameDuration)
    {
        this.animator = animator;
        this.boolParameter = boolParameter;
        this.frameDuration = frameDuration;
    }

    public void Trigger()
    {
        animator?.SetBool(boolParameter, true); 
        framesRemaining = frameDuration;
    }

    // Call once per frame (e.g. from LateUpdate) to clear the flash after its frame duration elapses.
    public void Tick()
    {
        if (framesRemaining <= 0)
            return;

        framesRemaining--;

        if (framesRemaining == 0)
            animator?.SetBool(boolParameter, false);
    }
}
