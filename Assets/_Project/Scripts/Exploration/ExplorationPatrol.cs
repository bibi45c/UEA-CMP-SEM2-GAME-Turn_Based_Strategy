using UnityEngine;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// Simple periodic patrol for exploration-mode enemies.
    /// Picks random points within a small radius, walks slowly between them,
    /// pauses at each point before choosing the next.
    /// </summary>
    public class ExplorationPatrol : MonoBehaviour
    {
        private Vector3 _patrolCenter;
        private float _patrolRadius;
        private float _moveSpeed;
        private float _rotationSpeed;
        private Animator _animator;

        private Vector3 _targetPos;
        private float _idleTimer;
        private bool _isMoving;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        public void Initialize(Vector3 center, float radius = 2.5f, float speed = 1.2f, float rotSpeed = 5f)
        {
            _patrolCenter = center;
            _patrolRadius = radius;
            _moveSpeed = speed;
            _rotationSpeed = rotSpeed;
            _animator = GetComponentInChildren<Animator>();

            // Start with a random idle delay so enemies don't all move in sync
            _idleTimer = Random.Range(1f, 4f);
        }

        private void Update()
        {
            if (_isMoving)
            {
                MoveToTarget();
            }
            else
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f)
                {
                    PickNewTarget();
                }
            }
        }

        private void PickNewTarget()
        {
            Vector2 offset = Random.insideUnitCircle * _patrolRadius;
            _targetPos = _patrolCenter + new Vector3(offset.x, 0f, offset.y);
            _isMoving = true;
            SetAnimation(true);
        }

        private void MoveToTarget()
        {
            Vector3 toTarget = _targetPos - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance < 0.15f)
            {
                // Arrived — stop and idle
                _isMoving = false;
                SetAnimation(false);
                _idleTimer = Random.Range(2f, 5f);
                return;
            }

            Vector3 direction = toTarget.normalized;

            // Rotate toward target
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }

            // Move
            float step = _moveSpeed * Time.deltaTime;
            if (step > distance) step = distance;
            transform.position += direction * step;

            AdjustHeight();
        }

        private void AdjustHeight()
        {
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
