using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Presentation layer for a unit: animation, selection indicator,
    /// death animation, and smooth path movement interpolation.
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

        [Header("Death")]
        [SerializeField] private float _deathFadeDuration = 1.2f;
        [SerializeField] private float _deathSinkDistance = 0.3f;

        // Runtime references
        private Animator _animator;
        private GameObject _selectionRing;
        private bool _isSelected;
        private bool _isDying;

        // Movement interpolation
        private List<Vector3> _worldPath;
        private int _currentWaypointIndex;
        private bool _isMoving;
        private Action _onMoveComplete;

        // Animator hashes
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        public bool IsMoving => _isMoving;
        public bool IsDying => _isDying;

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

        // --- Death Animation ---

        /// <summary>
        /// Play death animation sequence: trigger death anim, fade out, sink, then callback.
        /// </summary>
        public void PlayDeathAnimation(Action onComplete)
        {
            if (_isDying)
            {
                onComplete?.Invoke();
                return;
            }

            _isDying = true;

            // Hide selection ring immediately
            if (_selectionRing != null)
                _selectionRing.SetActive(false);

            // Disable collider so dead unit can't be clicked
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
                capsule.enabled = false;

            StartCoroutine(DeathSequenceCoroutine(onComplete));
        }

        private IEnumerator DeathSequenceCoroutine(Action onComplete)
        {
            // Try to trigger death animation if the Animator has one
            bool hasDeathAnim = false;
            if (_animator != null)
            {
                // Check if the animator has a "Death" parameter
                foreach (var param in _animator.parameters)
                {
                    if (param.nameHash == DeathHash)
                    {
                        hasDeathAnim = true;
                        break;
                    }
                }

                if (hasDeathAnim)
                {
                    _animator.SetTrigger(DeathHash);
                    // Wait a moment for the animation to start playing
                    yield return new WaitForSeconds(0.1f);

                    // Wait for death animation to finish
                    var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    float animLength = stateInfo.length;
                    if (animLength > 0.1f)
                        yield return new WaitForSeconds(animLength * 0.8f);
                }
            }

            // Fade out and sink regardless of whether death anim existed
            yield return StartCoroutine(FadeOutCoroutine());

            onComplete?.Invoke();
        }

        private IEnumerator FadeOutCoroutine()
        {
            // Collect all renderers (excluding the selection ring)
            var renderers = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (_selectionRing != null && r.gameObject == _selectionRing)
                    continue;
                renderers.Add(r);
            }

            // Switch materials to transparent mode for fading
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    SetMaterialTransparent(mat);
                }
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * _deathSinkDistance;
            float elapsed = 0f;

            while (elapsed < _deathFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _deathFadeDuration);

                // Ease out curve for smoother fade
                float alpha = 1f - t * t;

                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    foreach (var mat in r.materials)
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }

                // Sink slightly into the ground
                transform.position = Vector3.Lerp(startPos, endPos, t);

                yield return null;
            }

            // Ensure fully invisible
            foreach (var r in renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    Color c = mat.color;
                    c.a = 0f;
                    mat.color = c;
                }
            }
        }

        private static void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
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
                SetMaterialTransparent(renderer.material);
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
