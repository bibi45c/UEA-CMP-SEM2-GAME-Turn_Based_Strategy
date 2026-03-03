using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// WorldSpace Canvas attached to each unit showing HP bar.
    /// Always faces the camera (billboard). Created by CombatUIManager.
    /// </summary>
    public class UnitWorldUI : MonoBehaviour
    {
        private Image _hpFill;
        private Image _hpBackground;
        private Text _nameText;
        private int _unitId;
        private int _teamId;
        private float _currentFillTarget = 1f;
        private float _displayedFill = 1f;

        private const float BarWidth = 1.0f;
        private const float BarHeight = 0.08f;
        private const float HeightAboveUnit = 2.0f;
        private const float FillLerpSpeed = 5f;

        private static readonly Color PlayerBarColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private static readonly Color EnemyBarColor = new Color(0.85f, 0.15f, 0.15f, 1f);
        private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        private static readonly Color BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        public int UnitId => _unitId;

        public void Initialize(int unitId, int teamId, string unitName)
        {
            _unitId = unitId;
            _teamId = teamId;

            // Create WorldSpace Canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(BarWidth, BarHeight + 0.15f);
            rectTransform.localScale = Vector3.one;

            // Background (dark bar)
            var bgGO = CreateUIElement("HPBackground", transform);
            _hpBackground = bgGO.AddComponent<Image>();
            _hpBackground.color = BackgroundColor;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            bgRect.anchoredPosition = Vector2.zero;

            // Border
            var borderGO = CreateUIElement("HPBorder", transform);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = BorderColor;
            var borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.sizeDelta = new Vector2(BarWidth + 0.02f, BarHeight + 0.02f);
            borderRect.anchoredPosition = Vector2.zero;
            borderGO.transform.SetAsFirstSibling(); // Behind fill

            // Fill (colored portion)
            var fillGO = CreateUIElement("HPFill", transform);
            _hpFill = fillGO.AddComponent<Image>();
            _hpFill.color = teamId == 0 ? PlayerBarColor : EnemyBarColor;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(-BarWidth * 0.5f, 0f);

            // Name text (small, above the bar)
            var nameGO = CreateUIElement("NameText", transform);
            _nameText = nameGO.AddComponent<Text>();
            _nameText.text = unitName;
            _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameText.fontSize = 16;
            _nameText.color = Color.white;
            _nameText.alignment = TextAnchor.MiddleCenter;
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(2f, 0.15f);
            nameRect.anchoredPosition = new Vector2(0f, BarHeight * 0.5f + 0.08f);

            var nameOutline = nameGO.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(1f, -1f);
        }

        public void UpdateHP(float hpRatio)
        {
            _currentFillTarget = Mathf.Clamp01(hpRatio);
        }

        private void Update()
        {
            // Billboard: face camera
            if (UnityEngine.Camera.main != null)
            {
                transform.rotation = UnityEngine.Camera.main.transform.rotation;
            }

            // Smooth HP bar fill
            _displayedFill = Mathf.Lerp(_displayedFill, _currentFillTarget, Time.deltaTime * FillLerpSpeed);

            if (_hpFill != null)
            {
                var rect = _hpFill.rectTransform;
                rect.sizeDelta = new Vector2(BarWidth * _displayedFill, BarHeight);
            }

            // Color shift: green -> yellow -> red
            if (_hpFill != null && _teamId == 0)
            {
                if (_displayedFill > 0.5f)
                    _hpFill.color = Color.Lerp(Color.yellow, PlayerBarColor, (_displayedFill - 0.5f) * 2f);
                else
                    _hpFill.color = Color.Lerp(Color.red, Color.yellow, _displayedFill * 2f);
            }
        }

        private void LateUpdate()
        {
            // Position above the unit
            if (transform.parent != null)
            {
                transform.localPosition = new Vector3(0f, HeightAboveUnit, 0f);
            }
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
