using UnityEngine;
using UnityEngine.InputSystem;

namespace TurnBasedTactics.Camera
{
    /// <summary>
    /// Thin input reader for TacticalCamera.
    /// Reads from the TacticalCamera action map and forwards values to the camera.
    /// Keeps input handling decoupled from camera logic per three-layer architecture.
    /// </summary>
    [RequireComponent(typeof(TacticalCamera))]
    public class TacticalCameraInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;

        private TacticalCamera _camera;
        private InputActionMap _cameraMap;

        // Cached action references
        private InputAction _panAction;
        private InputAction _rotateAction;
        private InputAction _zoomAction;
        private InputAction _middleMouseDrag;
        private InputAction _middleMouseHeld;

        private void Awake()
        {
            _camera = GetComponent<TacticalCamera>();
        }

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[TacticalCameraInputHandler] InputActionAsset not assigned!");
                return;
            }

            _cameraMap = _inputActions.FindActionMap("TacticalCamera");
            if (_cameraMap == null)
            {
                Debug.LogError("[TacticalCameraInputHandler] TacticalCamera action map not found!");
                return;
            }

            _panAction = _cameraMap.FindAction("Pan");
            _rotateAction = _cameraMap.FindAction("Rotate");
            _zoomAction = _cameraMap.FindAction("Zoom");
            _middleMouseDrag = _cameraMap.FindAction("MiddleMouseDrag");
            _middleMouseHeld = _cameraMap.FindAction("MiddleMouseHeld");

            _cameraMap.Enable();
        }

        private void OnDisable()
        {
            _cameraMap?.Disable();
        }

        private void Update()
        {
            if (_cameraMap == null || !_cameraMap.enabled) return;

            // Keyboard pan (WASD)
            var panInput = _panAction.ReadValue<Vector2>();
            _camera.ApplyPanInput(panInput);

            // Orbit rotation (Q/E)
            var rotateInput = _rotateAction.ReadValue<float>();
            if (Mathf.Abs(rotateInput) > 0.01f)
            {
                _camera.ApplyRotationInput(rotateInput);
            }

            // Zoom (scroll wheel)
            var scrollInput = _zoomAction.ReadValue<Vector2>();
            if (Mathf.Abs(scrollInput.y) > 0.01f)
            {
                _camera.ApplyZoomInput(scrollInput.y);
            }

            // Middle mouse drag pan
            if (_middleMouseHeld.IsPressed())
            {
                var dragDelta = _middleMouseDrag.ReadValue<Vector2>();
                _camera.ApplyDragPan(dragDelta);
            }
        }
    }
}
