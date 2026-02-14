using UnityEngine;

namespace Game.Player.States
{
    public sealed class PlayerWalkState : PlayerCheckCollisionState
    {
        private const float _inputLogInterval = 0.15f;

        private float _walkSpeed;
        private float _rotateSpeed;
        private Vector2 _inputDirection;
        private Vector3 _moveDirection;
        private float _nextInputLogTime;

        public override void Initialize()
        {
            base.Initialize();

            _rotateSpeed = _player.Model.RotateSpeed;
            _walkSpeed = _player.Model.WalkSpeed;

            _player.View.Walk();

            _timer.TICK += OnTick;
        }

        public override void Dispose()
        {
            base.Dispose();

            _timer.TICK -= OnTick;
        }

        private void OnTick()
        {
            if(_isPause)
                return;

            if (!_gameView.Joystick.HasInput)
            {
                _player.Idle();
                return;
            }

            HandleInput();
            HandleMovement();
            HandleRotation();
            HandleBarsPosition();
            CheckCollisionBullets();
        }

        public override void OnPause(bool value)
        {
            base.OnPause(value);

            if(_isPause)
                _player.View.Idle();
            else
                _player.View.Walk();
        }

        private void HandleInput()
        {
            _inputDirection.x = _gameView.Joystick.Horizontal;
            _inputDirection.y = _gameView.Joystick.Vertical;

            _inputDirection = _inputDirection.normalized;

#if UNITY_EDITOR
            if (Time.unscaledTime >= _nextInputLogTime)
            {
                _nextInputLogTime = Time.unscaledTime + _inputLogInterval;
                Debug.Log($"[PlayerWalkState] final moveInput=({_inputDirection.x:F3},{_inputDirection.y:F3}) mag={_inputDirection.magnitude:F3} hasInput={_gameView.Joystick.HasInput}");
            }
#endif
        }

        private void HandleMovement()
        {
            _moveDirection = new Vector3(_inputDirection.x, 0, _inputDirection.y);
            _player.View.Position += _moveDirection * _walkSpeed * Time.deltaTime;
        }

        private void HandleRotation()
        {
            if (_moveDirection == Vector3.zero)
                return;

            var flatDirection = new Vector3(_moveDirection.x, 0, _moveDirection.z);
            var targetRotation = Quaternion.LookRotation(flatDirection);
            _player.View.Rotation = Quaternion.Lerp(_player.View.Rotation, targetRotation, _rotateSpeed * Time.deltaTime);
        }
    }
}
