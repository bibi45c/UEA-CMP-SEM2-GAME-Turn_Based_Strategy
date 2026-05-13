using UnityEngine;
using UnityEngine.UI;
using TurnBasedTactics.Core;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Always-on screen overlay: FPS counter (top-right) + Low/Med/High quality buttons.
    /// Self-contained — call Initialize() after AddComponent.
    /// </summary>
    public class GameOverlayHUD : MonoBehaviour
    {
        private Text _fpsText;

        // FPS sampling
        private float _fpsTimer;
        private int   _frameCount;
        private float _currentFPS;
        private const float FpsInterval = 0.5f;

        // Quality button labels for highlight refresh
        private Text[] _qualityLabels = new Text[3];

        public void Initialize()
        {
            BuildCanvas();
        }

        // ── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= FpsInterval)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer   = 0f;

                if (_fpsText != null)
                    _fpsText.text = $"FPS: {_currentFPS:0}";
            }
        }

        // ── UI Build ──────────────────────────────────────────────────────

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("OverlayHUDCanvas");
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var root = canvasGO.transform;

            // ── FPS label ─────────────────────────────────────────────────
            // Top-right, small gold text
            var fpsGO  = new GameObject("FPS");
            fpsGO.transform.SetParent(root, false);
            _fpsText   = fpsGO.AddComponent<Text>();
            _fpsText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _fpsText.fontSize  = 16;
            _fpsText.color     = new Color(0.72f, 0.53f, 0.04f, 1f); // gold
            _fpsText.text      = "FPS: --";
            _fpsText.alignment = TextAnchor.UpperRight;

            var outline = fpsGO.AddComponent<Outline>();
            outline.effectColor    = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            var fpsRT          = fpsGO.GetComponent<RectTransform>();
            fpsRT.anchorMin    = new Vector2(1f, 1f);
            fpsRT.anchorMax    = new Vector2(1f, 1f);
            fpsRT.pivot        = new Vector2(1f, 1f);
            fpsRT.anchoredPosition = new Vector2(-8f, -8f);
            fpsRT.sizeDelta    = new Vector2(110f, 24f);

            // ── Quality buttons ───────────────────────────────────────────
            // Stacked below the FPS label: Low / Med / High
            var qualDefs = new[] { ("Low", 0), ("Med", 2), ("High", 5) };
            float startY = -36f; // below FPS label

            for (int i = 0; i < 3; i++)
            {
                int level = qualDefs[i].Item2;
                int idx   = i;

                var btn = MakeQualityButton(root, qualDefs[i].Item1,
                    new Vector2(-8f, startY - i * 26f), level);
                _qualityLabels[i] = btn.GetComponentInChildren<Text>();
            }

            RefreshQualityButtons();
        }

        private GameObject MakeQualityButton(Transform parent, string label,
            Vector2 anchoredPos, int qualityLevel)
        {
            var go = new GameObject("Qual_" + label);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.06f, 0.10f, 0.82f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = new Color(0.72f, 0.53f, 0.04f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.3f, 1f);
            colors.pressedColor     = new Color(0.7f, 0.55f, 0.15f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                GameSettings.QualityLevel = qualityLevel;
                QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges: true);
                RefreshQualityButtons();
            });

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = new Vector2(1f, 1f);
            rt.anchorMax       = new Vector2(1f, 1f);
            rt.pivot           = new Vector2(1f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = new Vector2(60f, 22f);

            var txt = new GameObject("Label");
            txt.transform.SetParent(go.transform, false);
            var t      = txt.AddComponent<Text>();
            t.font     = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.color    = Color.white;
            t.text     = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;

            var tRT       = txt.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;

            return go;
        }

        private void RefreshQualityButtons()
        {
            int[] levels = { 0, 2, 5 };
            for (int i = 0; i < _qualityLabels.Length; i++)
            {
                if (_qualityLabels[i] == null) continue;
                bool active = GameSettings.QualityLevel == levels[i];
                _qualityLabels[i].color = active
                    ? new Color(0.72f, 0.53f, 0.04f, 1f)
                    : Color.white;
            }
        }
    }
}
