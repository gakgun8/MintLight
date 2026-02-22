using UnityEngine;

namespace Game.Player.States
{
    public sealed class PlayerWalkState : PlayerCheckCollisionState
    {
        private const float StalledMoveDistanceEpsilon = 0.0001f;
        private const float LargeUnscaledDeltaTimeThreshold = 0.05f;

        private float _walkSpeed;
        private float _rotateSpeed;
        private Vector2 _inputDirection;
        private Vector3 _moveDirection;
        private int _lastMoveAppliedFrame;
        private int _stalledMoveFrameCount;

        public override void Initialize()
        {
            base.Initialize();

            _rotateSpeed = _player.Model.RotateSpeed;
            _walkSpeed = _player.Model.WalkSpeed;

            _player.View.Walk();
            _player.View.SetMoveSpeed(1f);

            _lastMoveAppliedFrame = Time.frameCount;
            _stalledMoveFrameCount = 0;

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
                _player.ReportManualInput(Vector2.zero);
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
            {
                _player.View.SetMoveSpeed(0f);
                _player.View.Idle();
            }
            else
            {
                _player.View.SetMoveSpeed(1f);
                _player.View.Walk();
            }
        }

        private void HandleInput()
        {
            _inputDirection.x = _gameView.Joystick.Horizontal;
            _inputDirection.y = _gameView.Joystick.Vertical;

            _inputDirection = _inputDirection.normalized;
            _player.ReportManualInput(_inputDirection);
            _player.View.SetMoveSpeed(_inputDirection.magnitude);
        }

        private void HandleMovement()
        {
            var beforePosition = _player.View.Position;
            _moveDirection = new Vector3(_inputDirection.x, 0, _inputDirection.y);
            _player.View.Position += _moveDirection * _walkSpeed * Time.deltaTime;
            DebugMoveTrace(beforePosition, _player.View.Position);
        }

        private void DebugMoveTrace(Vector3 beforePosition, Vector3 afterPosition)
        {
            var dPos = afterPosition - beforePosition;
            var dPosMagnitude = dPos.magnitude;
            var frameGap = Time.frameCount - _lastMoveAppliedFrame;
            var dtUnscaled = Time.unscaledDeltaTime;
            var hasInput = _gameView.Joystick.HasInput;
            var hasLargeDtSpike = dtUnscaled >= LargeUnscaledDeltaTimeThreshold;
            var hasLargeFrameGap = frameGap > 1;

            if (hasInput && dPosMagnitude <= StalledMoveDistanceEpsilon)
                _stalledMoveFrameCount++;
            else
                _stalledMoveFrameCount = 0;

            var traceLevel = (hasLargeFrameGap || hasLargeDtSpike || _stalledMoveFrameCount >= 3)
                ? "WARN"
                : "TRACE";

            Debug.Log($"[MoveTrace][{traceLevel}] frame={Time.frameCount} (+{frameGap}) dt={Time.deltaTime:F4} dtU={dtUnscaled:F4} " +
                      $"hasInput={hasInput} inputMag={_inputDirection.magnitude:F3} moveDirMag={_moveDirection.magnitude:F3} " +
                      $"dPos={dPos} dPosMag={dPosMagnitude:F6} speed={_walkSpeed:F3} pause={_isPause}");

            if (_stalledMoveFrameCount >= 3)
            {
                var rootTransform = _player.View.transform;
                var rootCollider = rootTransform.GetComponent<Collider>();
                var characterController = rootTransform.GetComponent<CharacterController>();
                var rigidbody = rootTransform.GetComponent<Rigidbody>();

                Debug.LogWarning(
                    "[MoveTrace][STALL] hasInput=True but dPos≈0 repeats. " +
                     $"stalledFrames={_stalledMoveFrameCount} " +
                    $"rootColliderEnabled={(rootCollider != null && rootCollider.enabled)} " +
                    $"characterControllerEnabled={(characterController != null && characterController.enabled)} " +
                    $"rigidbodyDetected={(rigidbody != null)} rigidbodyIsKinematic={(rigidbody != null && rigidbody.isKinematic)} " +
                    $"rigidbodyConstraints={(rigidbody != null ? rigidbody.constraints.ToString() : "None")}");
            }

            _lastMoveAppliedFrame = Time.frameCount;
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
