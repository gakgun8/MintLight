using System;
using TMPro;
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
        private const float _keyboardInputSmoothSpeed = 3f;

        [SerializeField] private RectTransform _background, _handle;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private float swipeSpeedThresholdNormalized = 0.8f;
        [SerializeField] private float upwardAngleThreshold = 0.7f;

        [HideInInspector] public bool HasInput;
        [HideInInspector] public float Horizontal, Vertical;

        private Vector2 _inputDirection = Vector2.zero;
        private bool _firstTouchTriggered;
        private bool _isPointerDown;
        private float _targetAlpha;
        private bool _visibility;

        private Vector2 _lastPosition;
        private float _lastTime;

        private Vector2 _latestVelocity;
        private float _latestSpeedNormalized;

        private void Awake()
        {
            _visibility = true;
            SetCanvasAlpha(0f);
        }

        private void OnDisable()
        {
            HasInput = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return; // disable touch joystick on PC/Mac builds
#endif
            _isPointerDown = true;
            _background.position = eventData.position;

            _lastPosition = eventData.position;
            _lastTime = Time.unscaledTime;
            _latestSpeedNormalized = 0;
            _latestVelocity = Vector2.zero;

            SetTargetAlpha(1f);
            FireInput();
            OnDrag(eventData);

            HasInput = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return;
#endif
            UpdatePointerInputFromScreenPosition(eventData.position);

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

        private void UpdatePointerInputFromScreenPosition(Vector2 screenPosition)
        {
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, _background.position);
            Vector2 radius = new Vector2(_maxRadius, _maxRadius);

            _inputDirection = (screenPosition - position) / radius;
            _inputDirection = _inputDirection.magnitude > 1f ? _inputDirection.normalized : _inputDirection;

            SetHandlePosition(_inputDirection * _maxRadius);

            Horizontal = _inputDirection.x;
            Vertical = _inputDirection.y;
            HasInput = _inputDirection.sqrMagnitude > 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            return;
#endif
            if (_latestSpeedNormalized > swipeSpeedThresholdNormalized && _latestVelocity.y > Mathf.Abs(_latestVelocity.x) && _latestVelocity.normalized.y > upwardAngleThreshold)
            {
                FireJump();
            }

            HasInput = false;
            _isPointerDown = false;
            _inputDirection = Vector2.zero;
            Horizontal = 0f;
            Vertical = 0f;
            SetHandlePosition(Vector2.zero);
            SetTargetAlpha(0f);

            _latestSpeedNormalized = 0;
            _latestVelocity = Vector2.zero;
        }

        private Vector2 _smoothInput;

        private void Update()
        {
            SetCanvasAlpha(Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, Time.deltaTime * _fadeSpeed));

            if (_isPointerDown)
            {
                // 터치 드래그 중에는 OnDrag에서 받은 포인터 좌표를 그대로 유지한다.
                // 모바일 환경에서 Input.mousePosition을 매 프레임 섞어 쓰면 좌표가 간헐적으로 0 근처로 튀면서
                // HasInput이 false로 떨어지고 Walk 애니메이션이 Idle로 끊기는 현상이 발생할 수 있다.
                FireInput();
            }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            Vector2 moveInput = Vector2.zero;

            // Keyboard
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            moveInput = new Vector2(x, y);

            // Gamepad
            if (Gamepad.current != null)
                moveInput += Gamepad.current.leftStick.ReadValue();

            var hasKeyboardOrGamepadInput = moveInput.sqrMagnitude > 0f;

            if (hasKeyboardOrGamepadInput)
            {
                FireInput();
                HasInput = true;
                _inputDirection = moveInput.magnitude > 1f ? moveInput.normalized : moveInput;
                Horizontal = _inputDirection.x;
                Vertical = _inputDirection.y;
                SetHandlePosition(_inputDirection * _maxRadius);
            }
            else if (!_isPointerDown)
            {
                HasInput = false;
                _inputDirection = Vector2.zero;
                Horizontal = 0f;
                Vertical = 0f;
                _handle.anchoredPosition = Vector2.zero;
            }

            if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
                FireJump();
#endif
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
