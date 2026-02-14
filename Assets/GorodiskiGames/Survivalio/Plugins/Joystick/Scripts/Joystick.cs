using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Controls
{
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action ON_INPUT;
        public event Action ON_JUMP;

        private const float _maxRadius = 125f;
        private const float _fadeSpeed = 4f;
        [SerializeField] private RectTransform _background, _handle;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private float swipeSpeedThresholdNormalized = 0.8f;
        [SerializeField] private float upwardAngleThreshold = 0.7f;
        [SerializeField] private float inputDeadZone = 0.05f;
        [SerializeField] private bool enableInputDebugLogs;

        [HideInInspector] public bool HasInput;
        [HideInInspector] public float Horizontal, Vertical;

        private Vector2 _inputDirection = Vector2.zero;
        private bool _isPointerDown;
        private float _targetAlpha;
        private bool _visibility;

        private Vector2 _lastPosition;
        private float _lastTime;

        private Vector2 _latestVelocity;
        private float _latestSpeedNormalized;
        private int _activePointerId = -1;
        private float _nextDebugLogTime;

        private void Awake()
        {
            _visibility = true;
            SetCanvasAlpha(0f);
        }

        private void OnDisable()
        {
            HasInput = false;
            _isPointerDown = false;
            _activePointerId = -1;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return; // disable touch joystick on PC/Mac builds
#endif
            _isPointerDown = true;
            _activePointerId = eventData.pointerId;
            _background.position = eventData.position;

            _lastPosition = eventData.position;
            _lastTime = Time.unscaledTime;
            _latestSpeedNormalized = 0;
            _latestVelocity = Vector2.zero;

            SetTargetAlpha(1f);
            FireInput();
            UpdatePointerInput(eventData.position, "OnPointerDown");
        }

        public void OnDrag(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return;
#endif
            if (!_isPointerDown || eventData.pointerId != _activePointerId)
                return;

            UpdatePointerInput(eventData.position, "OnDrag");

            Vector2 currentPosition = eventData.position;
            float currentTime = Time.unscaledTime;

            Vector2 delta = currentPosition - _lastPosition;
            float deltaTime = currentTime - _lastTime;

            if (deltaTime > 0)
            {
                Vector2 velocity = delta / deltaTime;
                _latestVelocity = velocity;
                _latestSpeedNormalized = velocity.magnitude / Screen.height;
            }

            _lastPosition = currentPosition;
            _lastTime = currentTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return;
#endif
            if (eventData.pointerId != _activePointerId)
                return;

            if (_latestSpeedNormalized > swipeSpeedThresholdNormalized && _latestVelocity.y > Mathf.Abs(_latestVelocity.x) && _latestVelocity.normalized.y > upwardAngleThreshold)
            {
                FireJump();
            }

            ResetInputState("OnPointerUp");
            SetTargetAlpha(0f);

            _latestSpeedNormalized = 0;
            _latestVelocity = Vector2.zero;
        }

        private void Update()
        {
            SetCanvasAlpha(Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, Time.deltaTime * _fadeSpeed));

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            Vector2 moveInput = Vector2.zero;

            // Keyboard
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            moveInput = new Vector2(x, y);

            // Gamepad
            if (Gamepad.current != null)
                moveInput += Gamepad.current.leftStick.ReadValue();

            if (moveInput == Vector2.zero && !_isPointerDown)
            {
                HasInput = false;
                _inputDirection = Vector2.zero;
                Horizontal = 0f;
                Vertical = 0f;
                _handle.anchoredPosition = Vector2.zero;
            }
            else
            {
                FireInput();
                HasInput = true;
                _inputDirection = moveInput.magnitude > 1f ? moveInput.normalized : moveInput;
                Horizontal = _inputDirection.x;
                Vertical = _inputDirection.y;
                SetHandlePosition(_inputDirection * _maxRadius);
            }

            if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
    (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
                FireJump();

#else
            if (_isPointerDown)
                UpdatePointerInputFromTouchscreen();

#endif
        }

        private void UpdatePointerInputFromTouchscreen()
        {
            if (Touchscreen.current == null)
                return;

            var touches = Touchscreen.current.touches;
            bool foundTouch = false;

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed)
                    continue;

                if (touch.touchId.ReadValue() != _activePointerId)
                    continue;

                foundTouch = true;
                UpdatePointerInput(touch.position.ReadValue(), "Update.Touchscreen");
                break;
            }

            if (!foundTouch)
                ResetInputState("Update.TouchNotFound");
        }

        private void UpdatePointerInput(Vector2 pointerPosition, string source)
        {
            Vector2 centerPosition = RectTransformUtility.WorldToScreenPoint(null, _background.position);
            Vector2 radius = new Vector2(_maxRadius, _maxRadius);

            _inputDirection = (pointerPosition - centerPosition) / radius;
            _inputDirection = _inputDirection.magnitude > 1f ? _inputDirection.normalized : _inputDirection;

            if (_inputDirection.magnitude < inputDeadZone)
                _inputDirection = Vector2.zero;

            SetHandlePosition(_inputDirection * _maxRadius);

            Horizontal = _inputDirection.x;
            Vertical = _inputDirection.y;
            HasInput = _inputDirection != Vector2.zero;

            LogInputState(source);
        }

        private void ResetInputState(string source)
        {
            HasInput = false;
            _isPointerDown = false;
            _activePointerId = -1;
            _inputDirection = Vector2.zero;
            Horizontal = 0f;
            Vertical = 0f;
            SetHandlePosition(Vector2.zero);

            LogInputState(source);
        }

        private void LogInputState(string source)
        {
            if (!enableInputDebugLogs || Time.unscaledTime < _nextDebugLogTime)
                return;

            _nextDebugLogTime = Time.unscaledTime + 0.15f;
            Debug.Log($"[Joystick] {source} | vec=({_inputDirection.x:F3},{_inputDirection.y:F3}) mag={_inputDirection.magnitude:F3} hasInput={HasInput} pointerDown={_isPointerDown} pointerId={_activePointerId}");
        }

        private void FireJump() => ON_JUMP?.Invoke();

        private void FireInput()
        {
            ON_INPUT?.Invoke();
        }

        private void SetHandlePosition(Vector2 anchoredPosition) => _handle.anchoredPosition = anchoredPosition;

        private void SetCanvasAlpha(float value) => _canvasGroup.alpha = value;

        private void SetTargetAlpha(float value)
        {
            if (!_visibility) value = 0f;
            _targetAlpha = value;
        }

        public void Visibility(bool value) => _visibility = value;
    }
}
