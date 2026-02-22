namespace Game.Unit
{
    /// <summary>
    /// AnimationEvent -> gameplay bridge.
    /// Animator only emits timing events, gameplay stays code-driven.
    /// </summary>
    public interface IAnimEventReceiver
    {
        void OnFireAttackHit();
        void OnAttackWindupStart();
        void OnAttackRecoverEnd();
    }
}
