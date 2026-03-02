using UnityEngine;
using UnityEngine.InputSystem;
using TacticalCam = global::TurnBasedTactics.Camera.TacticalCamera;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// TEMPORARY exploration controller for browsing the battlefield.
    /// Right-click to move character to clicked ground position.
    /// Will be replaced by proper grid-based unit movement in Phase 1 Step 5.
    ///
    /// Structure: This component sits on a parent wrapper GO.
    /// The character model (with Animator) is a child object.
    /// This avoids conflict between movement code and animation Y-offset.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ExplorerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 720f;
        [SerializeField] private float _stoppingDistance = 0.15f;

        [Header("Raycast")]
        [SerializeField] private LayerMask _groundLayerMask = ~0;
        [SerializeField] private float _maxRayDistance = 100f;

        [Header("References")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Animator _animator;
        [SerializeField] private TacticalCam _tacticalCamera;

        // Input actions
        private InputActionMap _cameraMap;
        private InputAction _moveCommandAction;
        private InputAction _mousePositionAction;

        // Movement state
        private Vector3 _targetPosition;
        private bool _hasTarget;

        // CharacterController
        private CharacterController _cc;
        private float _gravityVelocity;


        // Animator parameter hash
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");

private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (_cc == null)
            {
                Debug.LogError("[ExplorerController] CharacterController component missing!");
            }
            _targetPosition = transform.position;
        }

        private void Start()
        {
            // Wire camera follow target
            if (_tacticalCamera != null)
            {
                _tacticalCamera.SetFollowTarget(transform);
            }
        }

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[ExplorerController] InputActionAsset not assigned!");
                return;
            }

            _cameraMap = _inputActions.FindActionMap("TacticalCamera");
            if (_cameraMap == null)
            {
                Debug.LogError("[ExplorerController] TacticalCamera action map not found!");
                return;
            }

            _moveCommandAction = _cameraMap.FindAction("MoveCommand");
            _mousePositionAction = _cameraMap.FindAction("MousePosition");

            // Map is enabled by TacticalCameraInputHandler, no need to enable here
        }

private void Update()
        {
            HandleMoveInput();
            HandleMovement();
            ApplyIdleGravity();
            UpdateAnimator();
        }

        private void HandleMoveInput()
        {
            if (_moveCommandAction == null) return;

            // Right-click pressed this frame
            if (_moveCommandAction.WasPressedThisFrame())
            {
                var mousePos = _mousePositionAction.ReadValue<Vector2>();
                var ray = _tacticalCamera != null
                    ? _tacticalCamera.GetScreenRay(mousePos)
                    : UnityEngine.Camera.main != null
                        ? UnityEngine.Camera.main.ScreenPointToRay(mousePos)
                        : default;

                if (Physics.Raycast(ray, out var hit, _maxRayDistance, _groundLayerMask))
                {
                    // Only move to roughly horizontal surfaces
                    if (hit.normal.y > 0.5f)
                    {
                        _targetPosition = hit.point;
                        _hasTarget = true;
                    }
                }
            }
        }

private void HandleMovement()
        {
            if (!_hasTarget || _cc == null) return;

            var direction = _targetPosition - transform.position;
            direction.y = 0f;
            var distance = direction.magnitude;

            if (distance <= _stoppingDistance)
            {
                _hasTarget = false;
                return;
            }

            // Move toward target using CharacterController (respects colliders + step offset)
            var moveStep = direction.normalized * (_moveSpeed * Time.deltaTime);
            if (moveStep.magnitude > distance)
            {
                moveStep = direction.normalized * distance;
            }

            // Apply gravity
            if (!_cc.isGrounded)
            {
                _gravityVelocity += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                _gravityVelocity = -0.5f; // Small downward force to keep grounded
            }

            moveStep.y = _gravityVelocity * Time.deltaTime;
            _cc.Move(moveStep);

            // Rotate toward movement direction
            if (direction.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

private void ApplyIdleGravity()
        {
            if (_hasTarget || _cc == null) return;

            // Keep character grounded even when idle
            if (!_cc.isGrounded)
            {
                _gravityVelocity += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                _gravityVelocity = -0.5f;
            }
            _cc.Move(new Vector3(0f, _gravityVelocity * Time.deltaTime, 0f));
        }


        private void UpdateAnimator()
        {
            if (_animator != null)
            {
                _animator.SetBool(IsMoving, _hasTarget);
            }
        }
    }
}
