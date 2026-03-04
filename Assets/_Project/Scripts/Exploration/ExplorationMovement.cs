using UnityEngine;
using UnityEngine.InputSystem;
using TacticalCam = global::TurnBasedTactics.Camera.TacticalCamera;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// Right-click point-and-click movement for the exploration leader.
    /// Raycasts to ground on right-click, then moves the character toward the target.
    /// No hex grid dependency — uses world-space movement.
    /// </summary>
    public class ExplorationMovement : MonoBehaviour
    {
        private float _moveSpeed = 4f;
        private float _rotationSpeed = 10f;
        private Vector3 _targetPosition;
        private bool _isMoving;
        private Animator _animator;
        private TacticalCam _camera;

        // Animation hashes
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;

        public void Initialize(float moveSpeed, float rotationSpeed)
        {
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _targetPosition = transform.position;
            _animator = GetComponentInChildren<Animator>();

            // Find camera
            _camera = FindAnyObjectByType<TacticalCam>();
        }

        private void Update()
        {
            HandleInput();
            MoveToTarget();
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Right-click to set movement target
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_camera == null)
                    _camera = FindAnyObjectByType<TacticalCam>();

                if (_camera == null) return;

                Vector2 mousePos = mouse.position.ReadValue();
                Ray ray = _camera.GetScreenRay(mousePos);

                // Raycast excluding Units layer and triggers
                int unitsLayer = LayerMask.NameToLayer("Units");
                int mask = unitsLayer >= 0 ? ~(1 << unitsLayer) : ~0;

                if (Physics.Raycast(ray, out RaycastHit hit, 200f, mask, QueryTriggerInteraction.Ignore))
                {
                    _targetPosition = hit.point;
                    _isMoving = true;
                }
            }
        }

        private void MoveToTarget()
        {
            if (!_isMoving) return;

            Vector3 direction = _targetPosition - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance < 0.15f)
            {
                // Arrived
                _isMoving = false;
                SetAnimation(false);
                return;
            }

            // Rotate toward target
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            // Move toward target
            float step = _moveSpeed * Time.deltaTime;
            if (step > distance) step = distance;
            transform.position += direction.normalized * step;

            // Adjust Y to terrain height
            AdjustHeight();

            SetAnimation(true);
        }

        private void AdjustHeight()
        {
            // Raycast down from above to snap to terrain (exclude Units layer)
            int unitsLayer = LayerMask.NameToLayer("Units");
            int mask = unitsLayer >= 0 ? ~(1 << unitsLayer) : ~0;

            Vector3 rayOrigin = transform.position + Vector3.up * 10f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, mask, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }
        }

        private void SetAnimation(bool moving)
        {
            if (_animator == null) return;
            _animator.SetBool(IsMovingHash, moving);
        }
    }
}
