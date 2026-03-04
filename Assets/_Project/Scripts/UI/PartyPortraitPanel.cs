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
    /// When HUDSpriteConfig is available, uses Synty Fantasy Warrior sprites;
    /// otherwise falls back to code-drawn colored rectangles.
    /// </summary>
    public class PartyPortraitPanel : MonoBehaviour
    {
        private UnitRegistry _registry;
        private UnitSelectionManager _selectionManager;
        private CombatSceneController _combatController;
        private Canvas _canvas;
        private RectTransform _panelContainer;
        private readonly List<PortraitSlot> _slots = new();

        private const float SlotWidth = 160f;
        private const float SlotHeight = 80f;
        private const float SlotSpacing = 4f;
        private const float PanelPadding = 8f;
        private const float LeftMargin = 10f;
        private const float TopOffset = 100f;

        // DOS2 color palette
        private static readonly Color PanelBgColor = DOS2Theme.PanelBg70;
        private static readonly Color SlotBgColor = DOS2Theme.PanelBgAlt;
        private static readonly Color ActiveBorderColor = DOS2Theme.GoldHighlight;
        private static readonly Color SelectedBorderColor = DOS2Theme.PartyBlue;
        private static readonly Color NormalBorderColor = DOS2Theme.InactiveBorder;
        private static readonly Color HPBarBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

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
                bgImage.color = PanelBgColor;
            }

            // Shadow behind the panel
            if (DOS2Theme.HasSprites)
                DOS2Theme.CreateShadow("PanelShadow", containerGO.transform);

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
            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            // Slot root (button-like)
            var slotGO = new GameObject($"Portrait_{unit.Definition.UnitName}", typeof(RectTransform));
            slotGO.transform.SetParent(_panelContainer, false);

            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);

            var layoutElem = slotGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = SlotWidth;
            layoutElem.preferredHeight = SlotHeight;

            // Border (portrait frame or colored rectangle)
            var borderImg = slotGO.AddComponent<Image>();
            if (hasSprites && s.PortraitFrame != null)
            {
                borderImg.sprite = s.PortraitFrame;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = NormalBorderColor;
            }
            else
            {
                borderImg.color = NormalBorderColor;
            }

            // Button for click
            var button = slotGO.AddComponent<Button>();
            button.targetGraphic = borderImg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;

            int capturedUnitId = unit.UnitId;
            button.onClick.AddListener(() => OnPortraitClicked(capturedUnitId));

            // Inner dark background
            var innerGO = CreateUIElement("Inner", slotGO.transform);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2f, 2f);
            innerRect.offsetMax = new Vector2(-2f, -2f);

            var innerImg = innerGO.AddComponent<Image>();
            if (hasSprites && s.SlotBackground != null)
            {
                innerImg.sprite = s.SlotBackground;
                innerImg.type = Image.Type.Sliced;
                innerImg.color = DOS2Theme.SyntyDarkBg;
            }
            else
            {
                innerImg.color = SlotBgColor;
            }
            innerImg.raycastTarget = false;

            // Unit name (upper 50%)
            var nameText = DOS2Theme.CreateText("Name", innerGO.transform,
                unit.Definition.UnitName, 13, DOS2Theme.TextWhite, TextAnchor.MiddleLeft, FontStyle.Bold);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(6f, 0f);
            nameRect.offsetMax = new Vector2(-4f, -3f);
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 9;
            nameText.resizeTextMaxSize = 14;

            // HP bar background (middle band)
            var hpBgGO = CreateUIElement("HPBarBg", innerGO.transform);
            var hpBgRect = hpBgGO.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0.04f, 0.28f);
            hpBgRect.anchorMax = new Vector2(0.96f, 0.48f);
            hpBgRect.offsetMin = Vector2.zero;
            hpBgRect.offsetMax = Vector2.zero;

            var hpBgImg = hpBgGO.AddComponent<Image>();
            if (hasSprites && s.HPBarBackground != null)
            {
                hpBgImg.sprite = s.HPBarBackground;
                hpBgImg.type = Image.Type.Sliced;
                hpBgImg.color = Color.white;
            }
            else
            {
                hpBgImg.color = HPBarBgColor;
            }
            hpBgImg.raycastTarget = false;

            // HP bar fill
            var hpFillGO = CreateUIElement("HPBarFill", hpBgGO.transform);
            var hpFillRect = hpFillGO.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.pivot = new Vector2(0f, 0.5f);

            float hpRatio = (float)unit.CurrentHP / unit.Stats.MaxHP;
            hpFillRect.anchorMax = new Vector2(hpRatio, 1f);

            var hpFillImg = hpFillGO.AddComponent<Image>();
            if (hasSprites && s.HPBarFill != null)
            {
                hpFillImg.sprite = s.HPBarFill;
                hpFillImg.type = Image.Type.Sliced;
            }
            hpFillImg.color = DOS2Theme.GetHPColor(hpRatio);
            hpFillImg.raycastTarget = false;

            // HP vignette overlay (optional depth effect)
            if (hasSprites && s.HPBarVignette != null)
            {
                var vigImg = DOS2Theme.CreateSpriteImage("HPVignette", hpBgGO.transform,
                    s.HPBarVignette, new Color(1f, 1f, 1f, 0.3f), false);
                vigImg.raycastTarget = false;
            }

            // HP frame overlay (decorative, on top)
            if (hasSprites && s.HPBarFrame != null)
            {
                var hpFrameImg = DOS2Theme.CreateSpriteImage("HPFrame", hpBgGO.transform,
                    s.HPBarFrame, Color.white);
                hpFrameImg.raycastTarget = false;
            }

            // HP text overlay
            var hpText = DOS2Theme.CreateOutlinedText("HPText", hpBgGO.transform,
                $"{unit.CurrentHP}/{unit.Stats.MaxHP}", 10, Color.white, TextAnchor.MiddleCenter);
            var hpTextRect = hpText.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;

            // AP pips row (bottom band)
            var apRow = CreateUIElement("APRow", innerGO.transform);
            var apRowRect = apRow.GetComponent<RectTransform>();
            apRowRect.anchorMin = new Vector2(0.04f, 0.04f);
            apRowRect.anchorMax = new Vector2(0.96f, 0.24f);
            apRowRect.offsetMin = Vector2.zero;
            apRowRect.offsetMax = Vector2.zero;

            var apLayout = apRow.AddComponent<HorizontalLayoutGroup>();
            apLayout.spacing = 3f;
            apLayout.childAlignment = TextAnchor.MiddleLeft;
            apLayout.childForceExpandWidth = false;
            apLayout.childForceExpandHeight = false;

            int maxAP = unit.MaxAP;
            int currentAP = unit.CurrentAP;
            var apPips = new Image[maxAP];

            for (int i = 0; i < maxAP; i++)
            {
                var pipGO = CreateUIElement($"AP_{i}", apRow.transform);
                var pipLE = pipGO.AddComponent<LayoutElement>();
                pipLE.preferredWidth = 10f;
                pipLE.preferredHeight = 10f;

                var pipImg = pipGO.AddComponent<Image>();
                bool isFull = i < currentAP;

                if (hasSprites && s.APGemFull != null)
                {
                    pipImg.sprite = isFull ? s.APGemFull : (s.APGemEmpty != null ? s.APGemEmpty : s.APGemFull);
                    pipImg.color = isFull ? DOS2Theme.APGreen : new Color(0.3f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    pipImg.color = isFull ? DOS2Theme.APGreen : DOS2Theme.PanelBgAlt;
                }
                pipImg.raycastTarget = false;
                apPips[i] = pipImg;
            }

            return new PortraitSlot
            {
                Root = slotGO,
                BorderImage = borderImg,
                HPFillImage = hpFillImg,
                HPFillRect = hpFillRect,
                HPText = hpText,
                APPips = apPips,
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

            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            foreach (var slot in _slots)
            {
                if (slot.UnitId != unitId) continue;

                float ratio = (float)unit.CurrentHP / unit.Stats.MaxHP;

                if (slot.HPFillRect != null)
                    slot.HPFillRect.anchorMax = new Vector2(ratio, 1f);

                if (slot.HPFillImage != null)
                    slot.HPFillImage.color = DOS2Theme.GetHPColor(ratio);

                if (slot.HPText != null)
                    slot.HPText.text = $"{unit.CurrentHP}/{unit.Stats.MaxHP}";

                // Update AP pips
                if (slot.APPips != null)
                {
                    int currentAP = unit.CurrentAP;
                    for (int i = 0; i < slot.APPips.Length; i++)
                    {
                        if (slot.APPips[i] == null) continue;

                        bool isFull = i < currentAP;
                        if (hasSprites && s.APGemFull != null)
                        {
                            slot.APPips[i].sprite = isFull ? s.APGemFull : (s.APGemEmpty != null ? s.APGemEmpty : s.APGemFull);
                            slot.APPips[i].color = isFull ? DOS2Theme.APGreen : new Color(0.3f, 0.3f, 0.3f, 1f);
                        }
                        else
                        {
                            slot.APPips[i].color = isFull ? DOS2Theme.APGreen : DOS2Theme.PanelBgAlt;
                        }
                    }
                }

                break;
            }
        }

        private void OnTurnChanged(TurnStartedEvent evt)
        {
            UpdateHighlights();
            RefreshAllAP();
        }
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

        private void RefreshAllAP()
        {
            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            foreach (var slot in _slots)
            {
                var unit = _registry.GetUnit(slot.UnitId);
                if (unit == null || slot.APPips == null) continue;

                int currentAP = unit.CurrentAP;
                for (int i = 0; i < slot.APPips.Length; i++)
                {
                    if (slot.APPips[i] == null) continue;

                    bool isFull = i < currentAP;
                    if (hasSprites && s.APGemFull != null)
                    {
                        slot.APPips[i].sprite = isFull ? s.APGemFull : (s.APGemEmpty != null ? s.APGemEmpty : s.APGemFull);
                        slot.APPips[i].color = isFull ? DOS2Theme.APGreen : new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                    else
                    {
                        slot.APPips[i].color = isFull ? DOS2Theme.APGreen : DOS2Theme.PanelBgAlt;
                    }
                }
            }
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
            public Image[] APPips;
            public int UnitId;
        }
    }
}
