using UnityEngine;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Brief emission flash on a unit when hit.
    /// Uses MaterialPropertyBlock to avoid material instance cloning.
    /// Added to each unit GO at spawn time by UnitSpawner.
    /// </summary>
    public class HitFlashEffect : MonoBehaviour
    {
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private Color _flashColor;
        private float _flashDuration;
        private float _elapsed;
        private bool _isFlashing;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Cache renderers and enable emission keyword on all materials.
        /// Must be called after model is instantiated.
        /// </summary>
        public void Initialize()
        {
            _propBlock = new MaterialPropertyBlock();

            // Collect renderers, skipping the SelectionRing child
            var allRenderers = GetComponentsInChildren<Renderer>();
            int count = 0;
            foreach (var r in allRenderers)
            {
                if (r.gameObject.name == "SelectionRing") continue;
                count++;
            }

            _renderers = new Renderer[count];
            int idx = 0;
            foreach (var r in allRenderers)
            {
                if (r.gameObject.name == "SelectionRing") continue;
                _renderers[idx++] = r;
            }

            // Enable emission keyword on all materials so _EmissionColor works
            foreach (var r in _renderers)
            {
                foreach (var mat in r.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
            }
        }

        /// <summary>
        /// Trigger a brief emission flash with the given color.
        /// </summary>
        public void Flash(Color color, float duration = 0.12f)
        {
            _flashColor = color;
            _flashDuration = duration;
            _elapsed = 0f;
            _isFlashing = true;
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flashDuration);

            // Lerp emission from flash color to black (no emission)
            Color current = Color.Lerp(_flashColor, Color.black, t);
            ApplyEmission(current);

            if (t >= 1f)
            {
                _isFlashing = false;
                ApplyEmission(Color.black);
            }
        }

        private void ApplyEmission(Color emissionColor)
        {
            if (_renderers == null) return;

            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColorId, emissionColor);
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
