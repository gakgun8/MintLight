using UnityEngine;

namespace Game.Player.States
{
    public sealed class PlayerWalkState : PlayerCheckCollisionState
    {
        private const float StalledMoveDistanceEpsilon = 0.0001f;
        private const float LargeUnscaledDeltaTimeThreshold = 0.05f;
        private const float InputReleaseGraceTime = 0.10f;
        private const float InputDeadzone = 0.10f;
        private const float WalkMaintainMinSpeed = 0.12f;

        private float _walkSpeed;
        private float _rotateSpeed;
        private Vector2 _inputDirection;
        private Vector3 _moveDirection;
        private int _lastMoveAppliedFrame;
        private int _stalledMoveFrameCount;
        private float _lastInputDetectedTime;

        public override void Initialize()
        {
            base.Initialize();

            _rotateSpeed = _player.Model.RotateSpeed;
            _walkSpeed = _player.Model.WalkSpeed;

            _player.View.Walk();
            _player.View.SetMoveSpeed(1f);

            _lastMoveAppliedFrame = Time.frameCount;
            _stalledMoveFrameCount = 0;
            _lastInputDetectedTime = _timer.Time;

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

            var rawInput = new Vector2(_gameView.Joystick.Horizontal, _gameView.Joystick.Vertical);
            var inputMagnitude = rawInput.magnitude;
            var hasEffectiveInput = inputMagnitude > InputDeadzone;

            if (hasEffectiveInput)
                _lastInputDetectedTime = _timer.Time;

            // Brief joystick read dropouts can flip HasInput for a frame and cause Walk->Idle jitter.
            // Keep Walk state for a short grace window before switching back to Idle.
            if (!hasEffectiveInput && (_timer.Time - _lastInputDetectedTime) > InputReleaseGraceTime)
            {
                _player.ReportManualInput(Vector2.zero);
                _player.View.SetMoveSpeed(0f);
                _player.Idle();
                return;
            }

            HandleInput(rawInput);
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

        private void HandleInput(Vector2 rawInput)
        {
            _inputDirection = rawInput;

            var rawMagnitude = _inputDirection.magnitude;
            if (rawMagnitude <= InputDeadzone)
            {
                _inputDirection = Vector2.zero;
                _player.ReportManualInput(Vector2.zero);
                return;
            }

            _inputDirection = _inputDirection.normalized;
            _player.ReportManualInput(_inputDirection);
        }

        private void HandleMovement()
        {
            var beforePosition = _player.View.Position;
            _moveDirection = new Vector3(_inputDirection.x, 0, _inputDirection.y);
            _player.View.Position += _moveDirection * _walkSpeed * Time.deltaTime;
            var afterPosition = _player.View.Position;
            var movedDistance = Vector3.Distance(beforePosition, afterPosition);
            // Use input-driven locomotion speed as the primary signal.
            // Position delta can be 0 intermittently due to collision/frame jitter and causes
            // Speed(0) spikes that kick BlendTree back to Idle for a frame.
            var inputSpeed = Mathf.Clamp01(_inputDirection.magnitude);
            var displacementSpeed = movedDistance / Mathf.Max(0.0001f, _walkSpeed * Time.deltaTime);
            var normalizedSpeed = Mathf.Max(inputSpeed, displacementSpeed);
            if (inputSpeed > 0.01f)
                normalizedSpeed = Mathf.Max(normalizedSpeed, WalkMaintainMinSpeed);

            _player.View.SetMoveSpeed(normalizedSpeed);

            if (normalizedSpeed > 0.05f && !_player.View.IsAttackPlaying)
                _player.View.Walk();
            else if (normalizedSpeed <= 0.01f && !_player.View.IsAttackPlaying)
                _player.View.Idle();

            DebugMoveTrace(beforePosition, afterPosition);
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
