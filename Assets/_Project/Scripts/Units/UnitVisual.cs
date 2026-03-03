using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Presentation layer for a unit: animation, selection indicator,
    /// and smooth path movement interpolation.
    /// Attach to the same GO as UnitBrain.
    /// </summary>
    public class UnitVisual : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("Selection")]
        [SerializeField] private Color _playerColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        [SerializeField] private Color _enemyColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);

        // Runtime references
        private Animator _animator;
        private GameObject _selectionRing;
        private bool _isSelected;

        // Movement interpolation
        private List<Vector3> _worldPath;
        private int _currentWaypointIndex;
        private bool _isMoving;
        private Action _onMoveComplete;

        // Animator hashes
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        public bool IsMoving => _isMoving;

        /// <summary>
        /// Called by UnitSpawner after model is instantiated.
        /// </summary>
        public void Initialize(Animator animator, int teamId)
        {
            _animator = animator;
            CreateSelectionRing(teamId);
        }

        // --- Selection ---

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_selectionRing != null)
                _selectionRing.SetActive(selected);
        }

        public void PlayActionAnimation()
        {
            if (_animator == null)
                return;

            _animator.ResetTrigger(AttackHash);
            _animator.SetTrigger(AttackHash);
        }

        // --- Path Movement ---

        /// <summary>
        /// Start smoothly moving along world-space waypoints.
        /// Calls onComplete when the final waypoint is reached.
        /// </summary>
        public void StartPathMovement(List<Vector3> worldPositions, Action onComplete)
        {
            if (worldPositions == null || worldPositions.Count < 2)
            {
                onComplete?.Invoke();
                return;
            }

            _worldPath = worldPositions;
            _currentWaypointIndex = 1; // Skip index 0 (current position)
            _isMoving = true;
            _onMoveComplete = onComplete;
            UpdateAnimator();
        }

        private void Update()
        {
            if (!_isMoving || _worldPath == null) return;

            Vector3 target = _worldPath[_currentWaypointIndex];
            Vector3 current = transform.position;

            // Move toward waypoint
            Vector3 direction = target - current;
            float distToTarget = direction.magnitude;
            float stepSize = _moveSpeed * Time.deltaTime;

            if (stepSize >= distToTarget)
            {
                // Reached this waypoint
                transform.position = target;
                _currentWaypointIndex++;

                if (_currentWaypointIndex >= _worldPath.Count)
                {
                    // Path complete
                    FinishMovement();
                    return;
                }
            }
            else
            {
                // Continue toward waypoint
                transform.position = current + direction.normalized * stepSize;
            }

            // Face movement direction (horizontal only)
            Vector3 flatDir = new Vector3(direction.x, 0f, direction.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }

            UpdateAnimator();
        }

        private void FinishMovement()
        {
            _isMoving = false;
            _worldPath = null;
            _currentWaypointIndex = 0;
            UpdateAnimator();

            var callback = _onMoveComplete;
            _onMoveComplete = null;
            callback?.Invoke();
        }

        private void UpdateAnimator()
        {
            if (_animator != null)
                _animator.SetBool(IsMovingHash, _isMoving);
        }

        // --- Selection Ring (simple circle indicator) ---

        private void CreateSelectionRing(int teamId)
        {
            // Create a flat cylinder as selection indicator
            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "SelectionRing";
            _selectionRing.transform.SetParent(transform);
            _selectionRing.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _selectionRing.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);

            // Remove collider (don't interfere with unit selection)
            var col = _selectionRing.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            // Set material color
            var renderer = _selectionRing.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color ringColor = teamId == 0 ? _playerColor : _enemyColor;
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = ringColor;
                renderer.material.SetFloat("_Mode", 3); // Transparent
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            _selectionRing.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_selectionRing != null)
                Destroy(_selectionRing);
        }
    }
}
