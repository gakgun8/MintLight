using UnityEngine;

namespace Game.Player.States
{
    public sealed class PlayerIdleState : PlayerCheckCollisionState
    {
        public override void Initialize()
        {
            base.Initialize();

            _player.View.SetMoveSpeed(0f);
            _player.View.Idle();

            _timer.TICK += OnTick;
        }

        public override void Dispose()
        {
            base.Dispose();

            _timer.TICK -= OnTick;
        }

        private void OnTick()
        {
            if (_isPause)
                return;

            _player.ReportManualInput(Vector2.zero);

            HandleBarsPosition();
            CheckCollisionBullets();

            if (!_gameView.Joystick.HasInput)
                return;

            _player.Walk();
        }
    }
}

