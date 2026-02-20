using UnityEngine;

namespace Game.Player
{
    public sealed class AnimatorEventsView : MonoBehaviour
    {
        [SerializeField] private PlayerView _playerView;

        public void FireFootOnGround()
        {
            if(_playerView == null)
                return;

            _playerView.FireFootOnGround();
        }

        public void FireAttackHit()
        {
            if (_playerView == null)
                return;

            _playerView.FireAttackHit();
        }

        public void FireAttackDash()
        {
            if (_playerView == null)
                return;

            _playerView.FireAttackDash();
        }
    }
}
