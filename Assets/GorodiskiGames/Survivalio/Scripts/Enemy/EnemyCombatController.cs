using Game.Unit;

namespace Game.Enemy
{
    /// <summary>
    /// Placeholder for enemy attack timing with the same AnimationEvent contract as player.
    /// </summary>
    public sealed class EnemyCombatController : IAnimEventReceiver
    {
        public void OnFireAttackHit()
        {
            // Intentionally empty in current template.
            // Enemy attack gameplay can be moved here without changing AnimationEvent plumbing.
        }

        public void OnAttackWindupStart() { }
        public void OnAttackRecoverEnd() { }
    }
}
