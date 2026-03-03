using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Animated floating text that drifts upward and fades out.
    /// Spawned by FloatingTextSpawner, self-destructs after lifetime.
    /// </summary>
    public class FloatingDamageText : MonoBehaviour
    {
        private Text _text;
        private float _lifetime;
        private float _elapsed;
        private Vector3 _velocity;
        private Color _startColor;

        private const float DefaultLifetime = 1.2f;
        private const float RiseSpeed = 0.8f;
        private const float DriftSpread = 0.3f;

        public void Initialize(string message, Color color, bool isCritical)
        {
            _lifetime = DefaultLifetime;
            _elapsed = 0f;
            _startColor = color;

            // Random horizontal drift for variety
            float driftX = Random.Range(-DriftSpread, DriftSpread);
            _velocity = new Vector3(driftX, RiseSpeed, 0f);

            // Create canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(2f, 0.5f);
            rectTransform.localScale = Vector3.one * 0.01f;

            // Create text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(transform, false);

            _text = textGO.AddComponent<Text>();
            _text.text = message;
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = isCritical ? 48 : 36;
            _text.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;
            _text.color = color;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200f, 50f);
            textRect.anchoredPosition = Vector2.zero;

            // Add outline for readability
            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Scale up for crits
            if (isCritical)
                rectTransform.localScale = Vector3.one * 0.014f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Rise upward
            transform.position += _velocity * Time.deltaTime;

            // Face camera
            if (UnityEngine.Camera.main != null)
            {
                transform.rotation = UnityEngine.Camera.main.transform.rotation;
            }

            // Fade out in the last 40% of lifetime
            float fadeStart = _lifetime * 0.6f;
            if (_elapsed > fadeStart)
            {
                float fadeProgress = (_elapsed - fadeStart) / (_lifetime - fadeStart);
                Color c = _startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                if (_text != null)
                    _text.color = c;
            }
        }
    }
}
