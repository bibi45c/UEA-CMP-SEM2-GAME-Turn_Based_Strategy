using UnityEngine;

namespace TurnBasedTactics.Camera
{
    /// <summary>
    /// Additive camera shake using Perlin noise with linear decay.
    /// Sits on the same GO as TacticalCamera. Exposes ShakeOffset
    /// which TacticalCamera reads in ApplyTransform().
    /// Offset is computed on-demand (no LateUpdate), so execution order doesn't matter.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private float _intensity;
        private float _duration;
        private float _startTime;
        private float _frequency;
        private float _seedX;
        private float _seedY;
        private bool _isShaking;

        /// <summary>
        /// Current shake offset, computed on-demand when read by TacticalCamera.
        /// </summary>
        public Vector3 ShakeOffset
        {
            get
            {
                if (!_isShaking)
                    return Vector3.zero;

                float elapsed = Time.time - _startTime;
                if (elapsed >= _duration)
                {
                    _isShaking = false;
                    return Vector3.zero;
                }

                float decay = 1f - (elapsed / _duration);
                float t = elapsed * _frequency;
                float offsetX = (Mathf.PerlinNoise(_seedX + t, 0f) - 0.5f) * 2f * _intensity * decay;
                float offsetY = (Mathf.PerlinNoise(0f, _seedY + t) - 0.5f) * 2f * _intensity * decay;

                return new Vector3(offsetX, offsetY, 0f);
            }
        }

        /// <summary>
        /// Trigger a camera shake. Stronger shakes override weaker in-progress ones.
        /// </summary>
        public void Shake(float intensity, float duration, float frequency = 18f)
        {
            // Only override if new shake is stronger or current one is done
            if (_isShaking && intensity < _intensity)
                return;

            _intensity = intensity;
            _duration = duration;
            _frequency = frequency;
            _startTime = Time.time;
            _seedX = Random.Range(0f, 100f);
            _seedY = Random.Range(0f, 100f);
            _isShaking = true;
        }
    }
}
