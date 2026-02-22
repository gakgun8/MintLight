using System.Collections.Generic;
using UnityEngine;

namespace Game.Unit
{
    /// <summary>
    /// Single AnimationEvent receiver for both player/enemy.
    /// Prevents scattered Animator callback logic across multiple classes.
    /// </summary>
    public sealed class AnimationEventsRelay : MonoBehaviour
    {
        private readonly List<IAnimEventReceiver> _receivers = new List<IAnimEventReceiver>(4);

        private void Awake()
        {
            RefreshReceivers();
        }

        public void RegisterReceiver(IAnimEventReceiver receiver)
        {
            if (receiver == null || _receivers.Contains(receiver))
                return;

            _receivers.Add(receiver);
        }

        public void UnregisterReceiver(IAnimEventReceiver receiver)
        {
            if (receiver == null)
                return;

            _receivers.Remove(receiver);
        }

        public void RefreshReceivers()
        {
            _receivers.Clear();
            var behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IAnimEventReceiver receiver)
                    _receivers.Add(receiver);
            }
        }

        public void FireAttackHit()
        {
            for (var i = 0; i < _receivers.Count; i++)
                _receivers[i].OnFireAttackHit();
        }

        public void AttackWindupStart()
        {
            for (var i = 0; i < _receivers.Count; i++)
                _receivers[i].OnAttackWindupStart();
        }

        public void AttackRecoverEnd()
        {
            for (var i = 0; i < _receivers.Count; i++)
                _receivers[i].OnAttackRecoverEnd();
        }
    }
}
