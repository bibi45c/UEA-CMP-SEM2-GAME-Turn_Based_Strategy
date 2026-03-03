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
    /// Anchored top-left to avoid overlapping the Round banner.
    /// Slot size auto-scales when many units are present (max ~60% screen width).
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
        private const float MaxBarWidthRatio = 0.6f; // max 60% of reference width

        private static readonly Color PlayerSlotColor = new Color(0.15f, 0.35f, 0.6f, 0.9f);
        private static readonly Color EnemySlotColor = new Color(0.55f, 0.12f, 0.12f, 0.9f);
        private static readonly Color ActiveBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color InactiveBorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

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
            var bgImage = containerGO.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);

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
            float slotH = BaseSlotHeight * (slotW / BaseSlotWidth); // keep aspect ratio

            for (int i = 0; i < turnOrder.Count; i++)
            {
                var unit = turnOrder[i];
                if (unit.IsDead) continue;

                bool isActive = current != null && unit.UnitId == current.UnitId;
                var slot = CreateSlot(unit, isActive, slotW, slotH);
                _slots.Add(slot);
            }
        }

        /// <summary>
        /// Compute slot width so the bar fits within MaxBarWidthRatio of the reference width.
        /// </summary>
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

            // Slot root
            var slotGO = new GameObject($"Slot_{unit.Definition.UnitName}", typeof(RectTransform));
            slotGO.transform.SetParent(_barContainer, false);

            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotW, slotH);

            var layoutElem = slotGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = slotW;
            layoutElem.preferredHeight = slotH;

            // Border (highlight for active)
            var borderImg = slotGO.AddComponent<Image>();
            borderImg.color = isActive ? ActiveBorderColor : InactiveBorderColor;

            // Inner background
            var innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(slotGO.transform, false);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2f, 2f);
            innerRect.offsetMax = new Vector2(-2f, -2f);

            var innerImg = innerGO.AddComponent<Image>();
            innerImg.color = isPlayer ? PlayerSlotColor : EnemySlotColor;

            // Unit name (lower 65%)
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(innerGO.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(1f, 0.6f);
            nameRect.offsetMin = new Vector2(2f, 1f);
            nameRect.offsetMax = new Vector2(-2f, 0f);

            var nameText = nameGO.AddComponent<Text>();
            nameText.text = unit.Definition.UnitName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 11;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 7;
            nameText.resizeTextMaxSize = 12;

            // HP text (upper 40%)
            var hpGO = new GameObject("HP", typeof(RectTransform));
            hpGO.transform.SetParent(innerGO.transform, false);
            var hpRect = hpGO.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0f, 0.6f);
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = new Vector2(2f, 0f);
            hpRect.offsetMax = new Vector2(-2f, -2f);

            var hpText = hpGO.AddComponent<Text>();
            hpText.text = $"{unit.CurrentHP}/{unit.Stats.MaxHP}";
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 10;
            hpText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            hpText.alignment = TextAnchor.MiddleCenter;
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
