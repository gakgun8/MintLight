using Game.Unit;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Backward-compatible bridge.
    /// New clips should call AnimationEventsRelay directly.
    /// </summary>
    public sealed class AnimatorEventsView : MonoBehaviour
    {
        [SerializeField] private AnimationEventsRelay _relay;

        private void Awake()
        {
            if (_relay == null)
                _relay = GetComponent<AnimationEventsRelay>();
        }

        public void FireAttackHit()
        {
            if (_relay != null)
                _relay.FireAttackHit();
        }

        public void AttackWindupStart()
        {
            if (_relay != null)
                _relay.AttackWindupStart();
        }

        public void AttackRecoverEnd()
        {
            if (_relay != null)
                _relay.AttackRecoverEnd();
        }

        // Legacy no-op to keep old footstep events from throwing missing-method warnings.
        public void FireFootOnGround() { }
        public void FireAttackDash() { }
    }
}
