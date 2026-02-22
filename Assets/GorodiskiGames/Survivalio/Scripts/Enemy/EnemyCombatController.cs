using Game.Unit;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Placeholder for enemy attack timing with the same AnimationEvent contract as player.
    /// </summary>
    public sealed class EnemyCombatController : MonoBehaviour, IAnimEventReceiver
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
