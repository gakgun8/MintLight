using UnityEngine;
using DG.Tweening;

namespace Game
{
    public sealed class CameraController : MonoBehaviour
    {
        private const int _shakeVibrato = 20;
        private const float _shakeDuration = 0.3f;
        private const float _shakeStrength = 0.1f;

        [SerializeField] private Camera _camera;
        [SerializeField] private float _distance;
        [SerializeField] private float _sensitivity = 10f;
        [Header("Zoom")]
        [SerializeField] private bool _enableZoom = true;
        [SerializeField] private float _minFieldOfView = 20f;
        [SerializeField] private float _maxFieldOfView = 70f;
        [SerializeField] private float _mouseScrollZoomSensitivity = 10f;
        [SerializeField] private float _pinchZoomSensitivity = 0.05f;
        [Header("Zoom Target")]
        [SerializeField] private float _maxTargetYOffset = 1f;
        [SerializeField] private AnimationCurve _zoomTargetYOffsetCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public Camera Camera => _camera;

        private Transform _target;
        private bool _isShaking;

        private void OnDisable()
        {
            DOTween.Kill(this);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = (Vector2)position;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetEnable(bool value)
        {
            enabled = value;
        }

        private void Update()
        {
            if(_target == null)
                return;

            var targetPosition = GetZoomAdjustedTargetPosition();
            var cameraTargetPosition = targetPosition + _target.forward * _distance;

            transform.position = Vector3.Lerp(transform.position, cameraTargetPosition, Time.deltaTime * _sensitivity);

            HandleZoom();
        }

        private void HandleZoom()
        {
            if (!_enableZoom || _camera == null)
                return;

            HandleMouseZoom();
            HandlePinchZoom();
        }

        private void HandleMouseZoom()
        {
            var scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scrollDelta, 0f))
                return;

            var zoomDelta = -scrollDelta * _mouseScrollZoomSensitivity;
            SetFieldOfView(_camera.fieldOfView + zoomDelta);
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount != 2)
                return;

            var firstTouch = Input.GetTouch(0);
            var secondTouch = Input.GetTouch(1);

            var firstTouchPreviousPosition = firstTouch.position - firstTouch.deltaPosition;
            var secondTouchPreviousPosition = secondTouch.position - secondTouch.deltaPosition;

            var previousDistance = Vector2.Distance(firstTouchPreviousPosition, secondTouchPreviousPosition);
            var currentDistance = Vector2.Distance(firstTouch.position, secondTouch.position);
            var pinchDelta = currentDistance - previousDistance;

            var zoomDelta = -pinchDelta * _pinchZoomSensitivity;
            SetFieldOfView(_camera.fieldOfView + zoomDelta);
        }

        private void SetFieldOfView(float value)
        {
            _camera.fieldOfView = Mathf.Clamp(value, _minFieldOfView, _maxFieldOfView);
        }

        private Vector3 GetZoomAdjustedTargetPosition()
        {
            if (_camera == null)
                return _target.position;

            var zoomProgress = Mathf.InverseLerp(_maxFieldOfView, _minFieldOfView, _camera.fieldOfView);
            var yOffset = _zoomTargetYOffsetCurve.Evaluate(zoomProgress) * _maxTargetYOffset;
            return _target.position + Vector3.up * yOffset;
        }

        public void Shake()
        {
            if (_isShaking)
                return;

            _isShaking = true;

            var strengthVector = new Vector3(_shakeStrength, _shakeStrength, _shakeStrength);
            _camera.DOShakePosition(_shakeDuration, strengthVector, _shakeVibrato).OnComplete(OnComplete).SetId(this);
        }

        private void OnComplete()
        {
            DOTween.Kill(this);
            _isShaking = false;
        }
    }
}
