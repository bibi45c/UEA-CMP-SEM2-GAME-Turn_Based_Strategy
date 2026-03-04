using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Screen-space UI bar showing the turn order (initiative sequence).
    /// Displays unit names + HP in order, highlights the active unit.
    /// Anchored top-center. Slot size auto-scales when many units are present.
    /// When HUDSpriteConfig is available, uses Synty Fantasy Warrior sprites;
    /// otherwise falls back to code-drawn colored rectangles.
    /// </summary>
    public class TurnOrderBar : MonoBehaviour
    {
        private TurnManager _turnManager;
        private UnitRegistry _registry;
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _barContainer;
        private HorizontalLayoutGroup _layout;
        private readonly List<TurnOrderSlot> _slots = new();

        private const float BaseSlotWidth = 80f;
        private const float BaseSlotHeight = 50f;
        private const float MinSlotWidth = 52f;
        private const float SlotSpacing = 3f;
        private const float BarPadding = 8f;
        private const float TopMargin = 10f;
        private const float MaxBarWidthRatio = 0.6f;

        // DOS2 color palette
        private static readonly Color PlayerBorderColor = DOS2Theme.PartyBlue;
        private static readonly Color EnemyBorderColor = DOS2Theme.EnemyRed;
        private static readonly Color ActiveBorderColor = DOS2Theme.GoldHighlight;
        private static readonly Color InactiveBorderColor = DOS2Theme.InactiveBorder;
        private static readonly Color SlotFillColor = DOS2Theme.PanelBgAlt;

        public void Initialize(TurnManager turnManager, UnitRegistry registry)
        {
            _turnManager = turnManager;
            _registry = registry;

            CreateCanvas();
            RefreshBar();

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            Debug.Log("[TurnOrderBar] Initialized.");
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("TurnOrderCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            _scaler = canvasGO.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Bar container — top-center anchor
            var containerGO = new GameObject("BarContainer", typeof(RectTransform));
            containerGO.transform.SetParent(canvasGO.transform, false);

            _barContainer = containerGO.GetComponent<RectTransform>();
            _barContainer.anchorMin = new Vector2(0.5f, 1f);
            _barContainer.anchorMax = new Vector2(0.5f, 1f);
            _barContainer.pivot = new Vector2(0.5f, 1f);
            _barContainer.anchoredPosition = new Vector2(0f, -TopMargin);

            // Background panel
            var s = DOS2Theme.Sprites;
            var bgImage = containerGO.AddComponent<Image>();
            if (s != null && s.PanelBackground != null)
            {
                bgImage.sprite = s.PanelBackground;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white; // Let sprite's own colors show through
            }
            else
            {
                bgImage.color = DOS2Theme.PanelBg85;
            }

            // Shadow behind the bar
            if (DOS2Theme.HasSprites)
                DOS2Theme.CreateShadow("BarShadow", containerGO.transform);

            // Horizontal layout
            _layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            _layout.spacing = SlotSpacing;
            _layout.padding = new RectOffset(
                (int)BarPadding, (int)BarPadding,
                (int)BarPadding, (int)BarPadding);
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childForceExpandWidth = false;
            _layout.childForceExpandHeight = false;

            var fitter = containerGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RefreshBar()
        {
            // Clear old slots
            foreach (var slot in _slots)
            {
                if (slot.Root != null)
                    Destroy(slot.Root);
            }
            _slots.Clear();

            if (_turnManager == null) return;

            var turnOrder = _turnManager.TurnOrder;
            var current = _turnManager.CurrentUnit;

            // Count alive units to compute adaptive slot size
            int aliveCount = 0;
            for (int i = 0; i < turnOrder.Count; i++)
            {
                if (!turnOrder[i].IsDead) aliveCount++;
            }

            float slotW = ComputeSlotWidth(aliveCount);
            float slotH = BaseSlotHeight * (slotW / BaseSlotWidth);

            for (int i = 0; i < turnOrder.Count; i++)
            {
                var unit = turnOrder[i];
                if (unit.IsDead) continue;

                bool isActive = current != null && unit.UnitId == current.UnitId;
                var slot = CreateSlot(unit, isActive, slotW, slotH);
                _slots.Add(slot);
            }
        }

        private float ComputeSlotWidth(int unitCount)
        {
            if (unitCount <= 0) return BaseSlotWidth;

            float maxBarWidth = _scaler.referenceResolution.x * MaxBarWidthRatio;
            float availableForSlots = maxBarWidth - BarPadding * 2f;
            float maxSlotWidth = (availableForSlots - SlotSpacing * (unitCount - 1)) / unitCount;

            return Mathf.Clamp(maxSlotWidth, MinSlotWidth, BaseSlotWidth);
        }

        private TurnOrderSlot CreateSlot(UnitRuntime unit, bool isActive, float slotW, float slotH)
        {
            bool isPlayer = unit.TeamId == TurnManager.PlayerTeamId;
            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            // Slot root
            var slotGO = new GameObject($"Slot_{unit.Definition.UnitName}", typeof(RectTransform));
            slotGO.transform.SetParent(_barContainer, false);

            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotW, slotH);

            var layoutElem = slotGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = slotW;
            layoutElem.preferredHeight = slotH;

            // Border — sprite or colored rectangle
            var borderImg = slotGO.AddComponent<Image>();
            Color factionBorder = isPlayer ? PlayerBorderColor : EnemyBorderColor;

            if (hasSprites && s.TurnSlotFrame != null)
            {
                borderImg.sprite = s.TurnSlotFrame;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = isActive ? ActiveBorderColor : factionBorder;
            }
            else
            {
                borderImg.color = isActive ? ActiveBorderColor : factionBorder;
            }

            // Inner background
            float bw = isActive ? 3f : 2f;
            var innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(slotGO.transform, false);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(bw, bw);
            innerRect.offsetMax = new Vector2(-bw, -bw);

            var innerImg = innerGO.AddComponent<Image>();
            if (hasSprites && s.SlotBackground != null)
            {
                innerImg.sprite = s.SlotBackground;
                innerImg.type = Image.Type.Sliced;
                innerImg.color = Color.white; // Let sprite's own colors show through
            }
            else
            {
                innerImg.color = SlotFillColor;
            }

            // Unit name (lower 65%)
            var nameText = DOS2Theme.CreateText("Name", innerGO.transform,
                unit.Definition.UnitName, 11, Color.white, TextAnchor.MiddleCenter);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(1f, 0.6f);
            nameRect.offsetMin = new Vector2(2f, 1f);
            nameRect.offsetMax = new Vector2(-2f, 0f);
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 7;
            nameText.resizeTextMaxSize = 12;

            // HP text (upper 40%)
            var hpText = DOS2Theme.CreateText("HP", innerGO.transform,
                $"{unit.CurrentHP}/{unit.Stats.MaxHP}", 10, DOS2Theme.TextGray, TextAnchor.MiddleCenter);
            var hpRect = hpText.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0f, 0.6f);
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = new Vector2(2f, 0f);
            hpRect.offsetMax = new Vector2(-2f, -2f);
            hpText.resizeTextForBestFit = true;
            hpText.resizeTextMinSize = 7;
            hpText.resizeTextMaxSize = 10;

            return new TurnOrderSlot
            {
                Root = slotGO,
                BorderImage = borderImg,
                HPText = hpText,
                UnitId = unit.UnitId
            };
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            RefreshBar();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            RefreshBar();
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            RefreshBar();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        private struct TurnOrderSlot
        {
            public GameObject Root;
            public Image BorderImage;
            public Text HPText;
            public int UnitId;
        }
    }
}
