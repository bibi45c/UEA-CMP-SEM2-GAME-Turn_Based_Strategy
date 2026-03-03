using UnityEngine;

namespace TurnBasedTactics.Camera
{
    /// <summary>
    /// DOS2/BG3-style orbital tactical camera.
    /// Orbits around a focus point with configurable zoom, rotation, and pan.
    /// Input is fed externally by TacticalCameraInputHandler (decoupled).
    /// </summary>
    public class TacticalCamera : MonoBehaviour
    {
        [SerializeField] private TacticalCameraConfig _config = new TacticalCameraConfig();

        private Transform _followTarget;
        private Vector3 _focusPoint;
        private Vector3 _panOffset;
        private float _currentZoom;
        private float _targetZoom;
        private float _currentYaw;
        private float _targetYaw;
        private bool _hasFollowTarget;
        private global::UnityEngine.Camera _cam;
        private CameraShake _cameraShake;

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            _hasFollowTarget = target != null;
            if (_hasFollowTarget)
            {
                _focusPoint = target.position;
                _panOffset = Vector3.zero;
            }
        }

        public void ClearFollowTarget()
        {
            _followTarget = null;
            _hasFollowTarget = false;
        }

        public void ApplyZoomInput(float delta)
        {
            _targetZoom -= delta * _config.ZoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _config.MinZoomDistance, _config.MaxZoomDistance);
        }

        public void ApplyRotationInput(float delta)
        {
            _targetYaw += delta * _config.RotationSpeed * Time.deltaTime;
        }

        public void ApplyPanInput(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f) return;
            var forward = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.right;
            _panOffset += (forward * input.y + right * input.x) * _config.KeyboardPanSpeed * Time.deltaTime;
        }

        public void ApplyDragPan(Vector2 mouseDelta)
        {
            var forward = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.right;
            _panOffset += (-right * mouseDelta.x - forward * mouseDelta.y) * _config.MouseDragPanSpeed * Time.deltaTime;
        }

        public void FocusOnPoint(Vector3 worldPos)
        {
            _focusPoint = worldPos;
            _panOffset = Vector3.zero;
        }

        public Ray GetScreenRay(Vector2 screenPos)
        {
            return _cam != null ? _cam.ScreenPointToRay(screenPos) : default;
        }

        public global::UnityEngine.Camera Cam => _cam;

        private void Awake()
        {
            _cam = GetComponent<global::UnityEngine.Camera>();
            _cameraShake = GetComponent<CameraShake>();
            _currentZoom = _config.DefaultZoomDistance;
            _targetZoom = _config.DefaultZoomDistance;
            _currentYaw = transform.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        private void LateUpdate()
        {
            UpdateFocusPoint();
            UpdateZoom();
            UpdateYaw();
            ResetPanOffset();
            ApplyTransform();
        }

        private void UpdateFocusPoint()
        {
            if (_hasFollowTarget && _followTarget != null)
            {
                _focusPoint = Vector3.Lerp(_focusPoint, _followTarget.position, _config.FollowDamping * Time.deltaTime);
            }
        }

        private void UpdateZoom()
        {
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _config.ZoomDamping * Time.deltaTime);
        }

        private void UpdateYaw()
        {
            _currentYaw = Mathf.Lerp(_currentYaw, _targetYaw, _config.RotationDamping * Time.deltaTime);
        }

        private void ResetPanOffset()
        {
            if (_hasFollowTarget && _panOffset.sqrMagnitude > 0.001f)
            {
                _panOffset = Vector3.Lerp(_panOffset, Vector3.zero, _config.PanResetSpeed * Time.deltaTime);
            }
        }

        private void ApplyTransform()
        {
            var targetFocus = _focusPoint + _panOffset;
            var rotation = Quaternion.Euler(_config.Pitch, _currentYaw, 0f);
            var offset = rotation * (Vector3.back * _currentZoom);
            transform.position = Vector3.Lerp(transform.position, targetFocus + offset, _config.PanDamping * Time.deltaTime);

            // Additive camera shake (computed by CameraShake in its own LateUpdate)
            if (_cameraShake != null)
                transform.position += _cameraShake.ShakeOffset;

            transform.LookAt(targetFocus);
        }
    }
}
