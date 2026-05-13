using UnityEngine;
using UnityEngine.InputSystem;

namespace TurnBasedTactics.Office
{
    /// <summary>
    /// Simple WASD player controller for office exploration.
    /// Uses CharacterController + Input System. Camera-relative movement.
    /// E key interacts with the nearest OfficeInteractable in range.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class OfficePlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed     = 4f;
        [SerializeField] private float _interactRange = 3.0f;

        public static OfficePlayerController Instance { get; private set; }

        private CharacterController  _cc;
        private Animator             _animator;
        private Transform            _cameraTransform;
        private OfficeInteractable   _nearestInteractable;
        private bool                 _inputEnabled = true;
        private float                _verticalVelocity;

        public void DisableInput() => _inputEnabled = false;
        public void EnableInput()  => _inputEnabled = true;
        public void SetCamera(Transform cam) => _cameraTransform = cam;

        private void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            // Capsule bottom = transform.y; small stepOffset avoids tile-seam bouncing
            _cc.center     = new Vector3(0f, 1f, 0f);
            _cc.height     = 2f;
            _cc.radius     = 0.3f;
            _cc.stepOffset = 0.1f;
            // Disable root motion — we drive position via CharacterController
            if (_animator != null) _animator.applyRootMotion = false;
        }

        private void Update()
        {
            // Build full velocity then call Move exactly once per frame
            var velocity = BuildVelocity();
            _cc.Move(velocity * Time.deltaTime);

            if (!_inputEnabled)
            {
                UpdateAnimator(false, 0f);
                return;
            }
            UpdateNearestInteractable();
            HandleInteractKey();
        }

        // Returns the full 3D movement vector for this frame (horizontal + vertical).
        private Vector3 BuildVelocity()
        {
            // Gravity
            if (_cc.isGrounded)
                _verticalVelocity = -1f;          // small constant keeps CC pressed to floor
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            // Horizontal input
            Vector3 horizontal = Vector3.zero;
            var kb = Keyboard.current;
            if (_inputEnabled && kb != null)
            {
                float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

                if (h != 0f || v != 0f)
                {
                    var camFwd   = _cameraTransform != null
                        ? Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized
                        : Vector3.forward;
                    var camRight = Vector3.Cross(Vector3.up, camFwd);
                    horizontal   = (camFwd * v + camRight * h).normalized * _moveSpeed;

                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(horizontal.normalized), 0.18f);
                }
            }

            bool isMoving = horizontal.sqrMagnitude > 0f;
            UpdateAnimator(isMoving, isMoving ? _moveSpeed : 0f);

            return horizontal + Vector3.up * _verticalVelocity;
        }

        private void UpdateAnimator(bool isMoving, float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat("MoveSpeed", speed);
            _animator.SetBool("MovementInputHeld", isMoving);
            _animator.SetBool("IsWalking", isMoving);
            _animator.SetBool("IsStopped", !isMoving);
            _animator.SetBool("IsGrounded", _cc.isGrounded);
            _animator.SetInteger("CurrentGait", isMoving ? 1 : 0);
        }

        private void UpdateNearestInteractable()
        {
            var all = FindObjectsByType<OfficeInteractable>(FindObjectsSortMode.None);
            OfficeInteractable nearest = null;
            float minDist = _interactRange;

            foreach (var obj in all)
            {
                float d = Vector3.Distance(transform.position, obj.transform.position);
                if (d < minDist) { minDist = d; nearest = obj; }
            }

            if (_nearestInteractable != nearest)
            {
                _nearestInteractable?.SetInRange(false);
                nearest?.SetInRange(true);
                _nearestInteractable = nearest;
            }
        }

        private void HandleInteractKey()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;
            _nearestInteractable?.Interact();
        }
    }
}
