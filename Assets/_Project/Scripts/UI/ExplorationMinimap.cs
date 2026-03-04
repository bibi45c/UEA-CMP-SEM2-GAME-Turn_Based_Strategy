using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Radar-style minimap for exploration mode (top-right corner).
    /// Fixed north-up orientation. Shows party members as blue dots,
    /// enemies as red dots, and the player leader as a white dot at center.
    /// </summary>
    public class ExplorationMinimap : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _dotContainer;
        private Image _playerDot;
        private readonly List<Image> _partyDots = new List<Image>();
        private readonly List<Image> _enemyDots = new List<Image>();

        private Transform _leaderTransform;
        private readonly List<Transform> _followerTransforms = new List<Transform>();
        private readonly List<Transform> _enemyTransforms = new List<Transform>();

        private const float MapSize = 140f;
        private const float MapRadius = 58f;
        private const float WorldRadius = 25f;
        private const float DotSize = 7f;
        private const float PlayerDotSize = 9f;
        private const float RightMargin = 10f;
        private const float TopMargin = 10f;

        public void Initialize(
            Transform leader,
            List<Transform> followers,
            List<Transform> enemies)
        {
            _leaderTransform = leader;

            if (followers != null)
                _followerTransforms.AddRange(followers);
            if (enemies != null)
                _enemyTransforms.AddRange(enemies);

            CreateCanvas();
            CreateRadar();
            CreateDots();
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void CreateRadar()
        {
            // Outer frame (top-right corner)
            var frameGO = DOS2Theme.CreateUIElement("MinimapFrame", _canvas.transform);
            var frameRect = frameGO.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(1f, 1f);
            frameRect.anchorMax = new Vector2(1f, 1f);
            frameRect.pivot = new Vector2(1f, 1f);
            frameRect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            frameRect.sizeDelta = new Vector2(MapSize + 8f, MapSize + 26f);

            var (border, fill) = DOS2Theme.CreateBorderedPanel("Frame", frameGO.transform,
                DOS2Theme.GoldAccent, DOS2Theme.PanelBg, 2f);

            // Title
            var titleText = DOS2Theme.CreateOutlinedText("Title", fill.transform,
                "Dungeon Forge", 10, DOS2Theme.GoldHighlight, TextAnchor.MiddleCenter);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 18f);

            // Radar background
            var radarGO = DOS2Theme.CreateUIElement("RadarBg", fill.transform);
            var radarRect = radarGO.GetComponent<RectTransform>();
            radarRect.anchorMin = new Vector2(0f, 0f);
            radarRect.anchorMax = new Vector2(1f, 1f);
            radarRect.offsetMin = new Vector2(2f, 2f);
            radarRect.offsetMax = new Vector2(-2f, -20f);

            var radarBg = radarGO.AddComponent<Image>();
            radarBg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            radarBg.raycastTarget = false;

            // Grid crosshair lines
            CreateGridLine(radarRect, true);
            CreateGridLine(radarRect, false);

            // Range ring (subtle circle outline at ~70% radius)
            CreateRangeRing(radarRect);

            // North indicator
            var northText = DOS2Theme.CreateOutlinedText("North", radarRect,
                "N", 9, DOS2Theme.GoldHighlight, TextAnchor.MiddleCenter);
            var northRect = northText.GetComponent<RectTransform>();
            northRect.anchorMin = new Vector2(0.5f, 1f);
            northRect.anchorMax = new Vector2(0.5f, 1f);
            northRect.pivot = new Vector2(0.5f, 1f);
            northRect.anchoredPosition = new Vector2(0f, -1f);
            northRect.sizeDelta = new Vector2(14f, 12f);

            // Dot container (centered, no rotation for fixed-north)
            var dotGO = DOS2Theme.CreateUIElement("Dots", radarRect);
            _dotContainer = dotGO.GetComponent<RectTransform>();
            _dotContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _dotContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _dotContainer.sizeDelta = Vector2.zero;
        }

        private void CreateGridLine(RectTransform parent, bool horizontal)
        {
            var lineGO = DOS2Theme.CreateUIElement(horizontal ? "HLine" : "VLine", parent);
            var lineRect = lineGO.GetComponent<RectTransform>();

            if (horizontal)
            {
                lineRect.anchorMin = new Vector2(0.15f, 0.5f);
                lineRect.anchorMax = new Vector2(0.85f, 0.5f);
                lineRect.sizeDelta = new Vector2(0f, 1f);
            }
            else
            {
                lineRect.anchorMin = new Vector2(0.5f, 0.15f);
                lineRect.anchorMax = new Vector2(0.5f, 0.85f);
                lineRect.sizeDelta = new Vector2(1f, 0f);
            }
            lineRect.anchoredPosition = Vector2.zero;

            var img = lineGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.3f, 0.4f);
            img.raycastTarget = false;
        }

        private void CreateRangeRing(RectTransform parent)
        {
            // Simple square outline at ~70% size to suggest range boundary
            var ringGO = DOS2Theme.CreateUIElement("RangeRing", parent);
            var ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.15f, 0.15f);
            ringRect.anchorMax = new Vector2(0.85f, 0.85f);
            ringRect.offsetMin = Vector2.zero;
            ringRect.offsetMax = Vector2.zero;

            var ringImg = ringGO.AddComponent<Image>();
            ringImg.color = new Color(0.2f, 0.2f, 0.3f, 0.3f);
            ringImg.raycastTarget = false;

            // Inner fill to make it look like a ring
            var innerGO = DOS2Theme.CreateUIElement("RingInner", ringGO.transform);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1f, 1f);
            innerRect.offsetMax = new Vector2(-1f, -1f);

            var innerImg = innerGO.AddComponent<Image>();
            innerImg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            innerImg.raycastTarget = false;
        }

        private void CreateDots()
        {
            // Player dot (center, white, larger)
            _playerDot = CreateDot("PlayerDot", Color.white, PlayerDotSize);

            // Follower dots (blue)
            for (int i = 0; i < _followerTransforms.Count; i++)
            {
                _partyDots.Add(CreateDot($"PartyDot_{i}", DOS2Theme.PartyBlue, DotSize));
            }

            // Enemy dots (red)
            for (int i = 0; i < _enemyTransforms.Count; i++)
            {
                _enemyDots.Add(CreateDot($"EnemyDot_{i}", DOS2Theme.EnemyRed, DotSize));
            }
        }

        private Image CreateDot(string name, Color color, float size)
        {
            var go = DOS2Theme.CreateUIElement(name, _dotContainer);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void LateUpdate()
        {
            if (_leaderTransform == null) return;

            Vector3 leaderPos = _leaderTransform.position;

            // Player dot stays at center
            _playerDot.rectTransform.anchoredPosition = Vector2.zero;

            // Update follower dots
            for (int i = 0; i < _partyDots.Count; i++)
            {
                if (i < _followerTransforms.Count && _followerTransforms[i] != null)
                {
                    UpdateDotPosition(_partyDots[i], _followerTransforms[i].position, leaderPos);
                    _partyDots[i].enabled = true;
                }
                else
                {
                    _partyDots[i].enabled = false;
                }
            }

            // Update enemy dots
            for (int i = 0; i < _enemyDots.Count; i++)
            {
                if (i < _enemyTransforms.Count && _enemyTransforms[i] != null)
                {
                    UpdateDotPosition(_enemyDots[i], _enemyTransforms[i].position, leaderPos);
                    _enemyDots[i].enabled = true;
                }
                else
                {
                    _enemyDots[i].enabled = false;
                }
            }
        }

        private void UpdateDotPosition(Image dot, Vector3 worldPos, Vector3 center)
        {
            Vector3 offset = worldPos - center;
            // Map world X → minimap X, world Z → minimap Y (north-up)
            float x = offset.x / WorldRadius * MapRadius;
            float y = offset.z / WorldRadius * MapRadius;

            Vector2 pos = new Vector2(x, y);
            if (pos.magnitude > MapRadius)
                pos = pos.normalized * MapRadius;

            dot.rectTransform.anchoredPosition = pos;
        }

        public void Cleanup()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
