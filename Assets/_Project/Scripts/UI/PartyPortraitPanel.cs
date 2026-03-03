using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Left-side party portrait panel. Shows player team units vertically.
    /// Each portrait is clickable to select that unit (and focus camera).
    /// Updates HP in real-time, highlights the active unit.
    /// </summary>
    public class PartyPortraitPanel : MonoBehaviour
    {
        private UnitRegistry _registry;
        private UnitSelectionManager _selectionManager;
        private CombatSceneController _combatController;
        private Canvas _canvas;
        private RectTransform _panelContainer;
        private readonly List<PortraitSlot> _slots = new();

        private const float SlotWidth = 140f;
        private const float SlotHeight = 60f;
        private const float SlotSpacing = 4f;
        private const float PanelPadding = 8f;
        private const float LeftMargin = 10f;
        private const float TopOffset = 100f; // below Round banner

        private static readonly Color PanelBgColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);
        private static readonly Color SlotBgColor = new Color(0.12f, 0.18f, 0.28f, 0.9f);
        private static readonly Color ActiveBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color SelectedBorderColor = new Color(0.4f, 0.8f, 1f, 0.9f);
        private static readonly Color NormalBorderColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);
        private static readonly Color HPBarColor = new Color(0.2f, 0.75f, 0.25f, 1f);
        private static readonly Color HPBarLowColor = new Color(0.85f, 0.2f, 0.15f, 1f);
        private static readonly Color HPBarBgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        public void Initialize(
            UnitRegistry registry,
            UnitSelectionManager selectionManager,
            CombatSceneController combatController)
        {
            _registry = registry;
            _selectionManager = selectionManager;
            _combatController = combatController;

            CreateCanvas();
            RebuildPortraits();

            EventBus.Subscribe<TurnStartedEvent>(OnTurnChanged);
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            Debug.Log("[PartyPortraitPanel] Initialized.");
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("PartyPortraitCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel container — left side, below Round banner
            var containerGO = new GameObject("PortraitContainer", typeof(RectTransform));
            containerGO.transform.SetParent(canvasGO.transform, false);

            _panelContainer = containerGO.GetComponent<RectTransform>();
            _panelContainer.anchorMin = new Vector2(0f, 1f);
            _panelContainer.anchorMax = new Vector2(0f, 1f);
            _panelContainer.pivot = new Vector2(0f, 1f);
            _panelContainer.anchoredPosition = new Vector2(LeftMargin, -TopOffset);

            // Background
            var bgImage = containerGO.AddComponent<Image>();
            bgImage.color = PanelBgColor;

            // Vertical layout
            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = SlotSpacing;
            layout.padding = new RectOffset(
                (int)PanelPadding, (int)PanelPadding,
                (int)PanelPadding, (int)PanelPadding);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = containerGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RebuildPortraits()
        {
            foreach (var slot in _slots)
            {
                if (slot.Root != null) Destroy(slot.Root);
            }
            _slots.Clear();

            var playerUnits = _registry.GetTeamUnits(TurnManager.PlayerTeamId);

            foreach (var unit in playerUnits)
            {
                var slot = CreatePortraitSlot(unit);
                _slots.Add(slot);
            }

            UpdateHighlights();
        }

        private PortraitSlot CreatePortraitSlot(UnitRuntime unit)
        {
            // Slot root (button-like)
            var slotGO = new GameObject($"Portrait_{unit.Definition.UnitName}", typeof(RectTransform));
            slotGO.transform.SetParent(_panelContainer, false);

            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);

            var layoutElem = slotGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = SlotWidth;
            layoutElem.preferredHeight = SlotHeight;

            // Border image
            var borderImg = slotGO.AddComponent<Image>();
            borderImg.color = NormalBorderColor;

            // Button for click
            var button = slotGO.AddComponent<Button>();
            button.targetGraphic = borderImg;

            // Disable default color transition (we manage colors manually)
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;

            int capturedUnitId = unit.UnitId;
            button.onClick.AddListener(() => OnPortraitClicked(capturedUnitId));

            // Inner background
            var innerGO = CreateUIElement("Inner", slotGO.transform);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2f, 2f);
            innerRect.offsetMax = new Vector2(-2f, -2f);
            var innerImg = innerGO.AddComponent<Image>();
            innerImg.color = SlotBgColor;
            innerImg.raycastTarget = false;

            // Name (left portion)
            var nameGO = CreateUIElement("Name", innerGO.transform);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.4f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(6f, 0f);
            nameRect.offsetMax = new Vector2(-4f, -3f);
            var nameText = nameGO.AddComponent<Text>();
            nameText.text = unit.Definition.UnitName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 13;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 9;
            nameText.resizeTextMaxSize = 14;
            nameText.raycastTarget = false;

            // HP bar background
            var hpBgGO = CreateUIElement("HPBarBg", innerGO.transform);
            var hpBgRect = hpBgGO.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0.04f, 0.1f);
            hpBgRect.anchorMax = new Vector2(0.96f, 0.35f);
            hpBgRect.offsetMin = Vector2.zero;
            hpBgRect.offsetMax = Vector2.zero;
            var hpBgImg = hpBgGO.AddComponent<Image>();
            hpBgImg.color = HPBarBgColor;
            hpBgImg.raycastTarget = false;

            // HP bar fill
            var hpFillGO = CreateUIElement("HPBarFill", hpBgGO.transform);
            var hpFillRect = hpFillGO.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            var hpFillImg = hpFillGO.AddComponent<Image>();
            hpFillImg.color = HPBarColor;
            hpFillImg.raycastTarget = false;

            // HP text overlay
            var hpTextGO = CreateUIElement("HPText", hpBgGO.transform);
            var hpTextRect = hpTextGO.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            var hpText = hpTextGO.AddComponent<Text>();
            hpText.text = $"{unit.CurrentHP}/{unit.Stats.MaxHP}";
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 10;
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.raycastTarget = false;

            var hpOutline = hpTextGO.AddComponent<Outline>();
            hpOutline.effectColor = Color.black;
            hpOutline.effectDistance = new Vector2(1f, -1f);

            return new PortraitSlot
            {
                Root = slotGO,
                BorderImage = borderImg,
                HPFillImage = hpFillImg,
                HPFillRect = hpFillRect,
                HPText = hpText,
                UnitId = unit.UnitId
            };
        }

        private void OnPortraitClicked(int unitId)
        {
            if (_selectionManager == null) return;

            _selectionManager.SelectUnit(unitId);
            _selectionManager.RefreshSelection();
        }

        private void UpdateHighlights()
        {
            var activeUnit = _combatController?.CurrentUnit;
            int activeId = activeUnit?.UnitId ?? -1;
            int selectedId = _selectionManager?.SelectedUnit?.UnitId ?? -1;

            foreach (var slot in _slots)
            {
                if (slot.BorderImage == null) continue;

                if (slot.UnitId == activeId)
                    slot.BorderImage.color = ActiveBorderColor;
                else if (slot.UnitId == selectedId)
                    slot.BorderImage.color = SelectedBorderColor;
                else
                    slot.BorderImage.color = NormalBorderColor;
            }
        }

        private void UpdateSlotHP(int unitId)
        {
            var unit = _registry.GetUnit(unitId);
            if (unit == null) return;

            foreach (var slot in _slots)
            {
                if (slot.UnitId != unitId) continue;

                float ratio = (float)unit.CurrentHP / unit.Stats.MaxHP;

                if (slot.HPFillRect != null)
                    slot.HPFillRect.anchorMax = new Vector2(ratio, 1f);

                if (slot.HPFillImage != null)
                    slot.HPFillImage.color = ratio > 0.35f ? HPBarColor : HPBarLowColor;

                if (slot.HPText != null)
                    slot.HPText.text = $"{unit.CurrentHP}/{unit.Stats.MaxHP}";

                break;
            }
        }

        private void OnTurnChanged(TurnStartedEvent evt) => UpdateHighlights();
        private void OnUnitSelected(UnitSelectedEvent evt) => UpdateHighlights();
        private void OnUnitDeselected(UnitDeselectedEvent evt) => UpdateHighlights();

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            UpdateSlotHP(evt.TargetUnitId);
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            UpdateSlotHP(evt.TargetUnitId);
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            RebuildPortraits();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnChanged);
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private struct PortraitSlot
        {
            public GameObject Root;
            public Image BorderImage;
            public Image HPFillImage;
            public RectTransform HPFillRect;
            public Text HPText;
            public int UnitId;
        }
    }
}
