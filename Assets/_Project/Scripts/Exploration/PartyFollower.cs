using UnityEngine;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// Makes a party member follow the leader during exploration.
    /// Uses hysteresis (start/stop thresholds) to prevent constant walking.
    /// Only starts moving when far from leader, stops when close enough.
    /// </summary>
    public class PartyFollower : MonoBehaviour
    {
        private Transform _leader;
        private float _moveSpeed;
        private float _rotationSpeed;
        private float _followDistance;
        private Animator _animator;
        private bool _isMoving;

        // Hysteresis: start moving when > followDistance + buffer, stop when <= followDistance
        private const float StartBuffer = 0.5f;

        // Animation hashes
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        public void Initialize(Transform leader, float moveSpeed, float rotationSpeed, float followDistance)
        {
            _leader = leader;
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _followDistance = followDistance;
            _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (_leader == null) return;

            Vector3 toLeader = _leader.position - transform.position;
            toLeader.y = 0f;
            float distance = toLeader.magnitude;

            // Hysteresis: only START moving when beyond followDistance + buffer
            // STOP moving when within followDistance
            bool shouldMove;
            if (_isMoving)
            {
                // Already moving — keep moving until we're within follow distance
                shouldMove = distance > _followDistance;
            }
            else
            {
                // Not moving — only start if we're far enough away (with buffer)
                shouldMove = distance > _followDistance + StartBuffer;
            }

            if (shouldMove)
            {
                Vector3 direction = toLeader.normalized;

                // Rotate toward leader
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }

                // Move toward leader, stop at follow distance
                float moveDistance = distance - _followDistance;
                float step = _moveSpeed * Time.deltaTime;
                if (step > moveDistance) step = moveDistance;
                transform.position += direction * step;

                // Adjust height to terrain
                AdjustHeight();

                if (!_isMoving)
                {
                    _isMoving = true;
                    SetAnimation(true);
                }
            }
            else
            {
                if (_isMoving)
                {
                    _isMoving = false;
                    SetAnimation(false);
                }
            }
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
