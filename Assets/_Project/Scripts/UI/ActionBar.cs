using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// DOS2-style bottom action bar. Replaces the legacy OnGUI CombatHudController.
    /// Contains: unit name, HP bar, AP pips, ability hotbar slots, hint text, round badge.
    /// All UI is procedurally generated (no prefabs).
    /// When HUDSpriteConfig is available, uses Synty Fantasy Warrior sprites;
    /// otherwise falls back to code-drawn colored rectangles.
    /// </summary>
    public class ActionBar : MonoBehaviour
    {
        private CombatSceneController _combatController;
        private Canvas _canvas;

        // Top-right round badge
        private Text _roundText;

        // Center bar elements
        private Text _unitNameText;
        private Image _hpBarFill;
        private RectTransform _hpBarFillRect;
        private Text _hpText;
        private bool _hpUseFillMode;
        private readonly List<Image> _apPips = new();
        private readonly List<AbilitySlot> _abilitySlots = new();
        private Text _hintText;

        // Standalone buttons
        private AbilitySlot _moveSlot;
        private AbilitySlot _cancelSlot;
        private AbilitySlot _endTurnSlot;

        private const float SlotSize = 56f;
        private const float SlotSpacing = 4f;
        private const float BorderWidth = 2f;
        private const int MaxAPPips = 6;

        public void Initialize(CombatSceneController combatController)
        {
            _combatController = combatController;
            CreateCanvas();
            BuildLayout();

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);

            Debug.Log("[ActionBar] Initialized.");
        }

        private void Update()
        {
            if (_combatController == null) return;
            HandleHotkeys();
            RefreshState();
        }

        private void HandleHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!_combatController.IsPlayerTurn || _combatController.IsActionAnimating) return;

            // 1 = Move
            if (kb.digit1Key.wasPressedThisFrame && _moveSlot.Button != null && _moveSlot.Button.interactable)
                _combatController.QueueMoveAction();

            // 2, 3, 4... = Abilities
            Key[] abilityKeys = { Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
            for (int i = 0; i < _abilitySlots.Count && i < abilityKeys.Length; i++)
            {
                if (kb[abilityKeys[i]].wasPressedThisFrame
                    && _abilitySlots[i].Button != null
                    && _abilitySlots[i].Button.interactable
                    && _abilitySlots[i].Ability != null)
                {
                    _combatController.QueueAbility(_abilitySlots[i].Ability);
                }
            }

            // C = Cancel
            if (kb.cKey.wasPressedThisFrame)
                _combatController.ClearQueuedAction();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
        }

        // ── Canvas Setup ──────────────────────────────────────────────

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("ActionBarCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 15;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // ── Layout Construction ───────────────────────────────────────

        private void BuildLayout()
        {
            BuildRoundBadge();
            BuildBottomBar();
        }

        private void BuildRoundBadge()
        {
            var badgeGO = DOS2Theme.CreateUIElement("RoundBadge", _canvas.transform);
            var badgeRect = badgeGO.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-16f, -16f);
            badgeRect.sizeDelta = new Vector2(140f, 40f);

            var s = DOS2Theme.Sprites;
            if (s != null && s.Banner != null)
            {
                var bannerImg = badgeGO.AddComponent<Image>();
                bannerImg.sprite = s.Banner;
                bannerImg.type = Image.Type.Sliced;
                bannerImg.color = DOS2Theme.SyntyGold;
            }
            else
            {
                var badgeBg = badgeGO.AddComponent<Image>();
                badgeBg.color = DOS2Theme.PanelBg85;
                var bgOutline = badgeGO.AddComponent<Outline>();
                bgOutline.effectColor = DOS2Theme.GoldAccent;
                bgOutline.effectDistance = new Vector2(2f, -2f);
            }

            _roundText = DOS2Theme.CreateOutlinedText("RoundText", badgeGO.transform,
                "Round 1", 18, DOS2Theme.GoldAccent, TextAnchor.MiddleCenter);
            _roundText.fontStyle = FontStyle.Bold;
            var rtRect = _roundText.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = Vector2.zero;
            rtRect.offsetMax = Vector2.zero;
        }

        private void BuildBottomBar()
        {
            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            // Main container — bottom center
            var barRoot = DOS2Theme.CreateUIElement("BarRoot", _canvas.transform);
            var barRootRect = barRoot.GetComponent<RectTransform>();
            barRootRect.anchorMin = new Vector2(0.5f, 0f);
            barRootRect.anchorMax = new Vector2(0.5f, 0f);
            barRootRect.pivot = new Vector2(0.5f, 0f);
            barRootRect.anchoredPosition = new Vector2(0f, 12f);
            barRootRect.sizeDelta = new Vector2(900f, 160f);

            // ── Unit name label (top of bar) ──
            _unitNameText = DOS2Theme.CreateOutlinedText("UnitName", barRoot.transform,
                "", 16, DOS2Theme.TextWhite, TextAnchor.MiddleCenter);
            _unitNameText.fontStyle = FontStyle.Bold;
            var nameRect = _unitNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = new Vector2(0f, 22f);

            // ── HP bar (below name) ──
            float hpBarHeight = hasSprites ? 14f : 8f;
            var hpContainer = DOS2Theme.CreateUIElement("HPBarContainer", barRoot.transform);
            var hpContainerRect = hpContainer.GetComponent<RectTransform>();
            hpContainerRect.anchorMin = new Vector2(0.15f, 1f);
            hpContainerRect.anchorMax = new Vector2(0.85f, 1f);
            hpContainerRect.pivot = new Vector2(0.5f, 1f);
            hpContainerRect.anchoredPosition = new Vector2(0f, -24f);
            hpContainerRect.sizeDelta = new Vector2(0f, hpBarHeight);

            // HP background
            var hpBg = hpContainer.AddComponent<Image>();
            if (s != null && s.HPBarBackground != null)
            {
                hpBg.sprite = s.HPBarBackground;
                hpBg.type = Image.Type.Sliced;
                hpBg.color = Color.white;
            }
            else
            {
                hpBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }
            hpBg.raycastTarget = false;

            // HP fill
            var hpFillGO = DOS2Theme.CreateUIElement("HPFill", hpContainer.transform);
            _hpBarFillRect = hpFillGO.GetComponent<RectTransform>();
            _hpBarFillRect.anchorMin = Vector2.zero;
            _hpBarFillRect.anchorMax = Vector2.one;
            _hpBarFillRect.offsetMin = Vector2.zero;
            _hpBarFillRect.offsetMax = Vector2.zero;

            _hpBarFill = hpFillGO.AddComponent<Image>();
            _hpBarFill.color = DOS2Theme.HPGreen;
            _hpBarFill.raycastTarget = false;

            if (s != null && s.HPBarFill != null)
            {
                _hpBarFill.sprite = s.HPBarFill;
                _hpBarFill.type = Image.Type.Filled;
                _hpBarFill.fillMethod = Image.FillMethod.Horizontal;
                _hpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _hpUseFillMode = true;
            }
            else
            {
                _hpBarFillRect.pivot = new Vector2(0f, 0.5f);
                _hpUseFillMode = false;
            }

            // HP vignette overlay (optional depth effect)
            if (s != null && s.HPBarVignette != null)
            {
                var vigImg = DOS2Theme.CreateSpriteImage("HPVignette", hpContainer.transform,
                    s.HPBarVignette, new Color(1f, 1f, 1f, 0.3f), false);
                vigImg.raycastTarget = false;
            }

            // HP frame overlay (decorative, on top)
            if (s != null && s.HPBarFrame != null)
            {
                var hpFrameImg = DOS2Theme.CreateSpriteImage("HPFrame", hpContainer.transform,
                    s.HPBarFrame, Color.white);
                hpFrameImg.raycastTarget = false;
            }

            // HP text
            _hpText = DOS2Theme.CreateOutlinedText("HPText", hpContainer.transform,
                "", 9, Color.white, TextAnchor.MiddleCenter);
            var hpTextRect = _hpText.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;

            // ── AP pips (below HP bar) ──
            float pipSize = hasSprites ? 16f : 12f;
            float pipSpacing = hasSprites ? 2f : 4f;
            float apRowY = hasSprites ? -40f : -36f;

            var apRow = DOS2Theme.CreateUIElement("APPipRow", barRoot.transform);
            var apRowRect = apRow.GetComponent<RectTransform>();
            apRowRect.anchorMin = new Vector2(0.5f, 1f);
            apRowRect.anchorMax = new Vector2(0.5f, 1f);
            apRowRect.pivot = new Vector2(0.5f, 1f);
            apRowRect.anchoredPosition = new Vector2(0f, apRowY);
            apRowRect.sizeDelta = new Vector2(MaxAPPips * (pipSize + pipSpacing), pipSize + 2f);

            var apLayout = apRow.AddComponent<HorizontalLayoutGroup>();
            apLayout.spacing = pipSpacing;
            apLayout.childAlignment = TextAnchor.MiddleCenter;
            apLayout.childForceExpandWidth = false;
            apLayout.childForceExpandHeight = false;

            for (int i = 0; i < MaxAPPips; i++)
            {
                var pipGO = DOS2Theme.CreateUIElement($"AP_{i}", apRow.transform);
                var pipRect = pipGO.GetComponent<RectTransform>();
                pipRect.sizeDelta = new Vector2(pipSize, pipSize);

                var layoutElem = pipGO.AddComponent<LayoutElement>();
                layoutElem.preferredWidth = pipSize;
                layoutElem.preferredHeight = pipSize;

                var pipImg = pipGO.AddComponent<Image>();
                if (s != null && s.APGemFull != null)
                {
                    pipImg.sprite = s.APGemFull;
                    pipImg.color = DOS2Theme.APGreen;
                }
                else
                {
                    pipImg.color = DOS2Theme.APGreen;
                }
                pipImg.raycastTarget = false;

                _apPips.Add(pipImg);
            }

            // ── Hotbar frame (panel with border) ──
            var frameGO = DOS2Theme.CreateUIElement("HotbarFrame", barRoot.transform);
            var frameRect = frameGO.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0f, 0f);
            frameRect.anchorMax = new Vector2(1f, 1f);
            frameRect.pivot = new Vector2(0.5f, 0f);
            float topInset = hasSprites ? -58f : -54f;
            frameRect.offsetMin = new Vector2(0f, 0f);
            frameRect.offsetMax = new Vector2(0f, topInset);

            if (hasSprites)
            {
                // Shadow behind the entire bar
                DOS2Theme.CreateShadow("BarShadow", frameGO.transform);

                // Panel background (sliced, dark)
                var panelBgImg = frameGO.AddComponent<Image>();
                panelBgImg.sprite = s.PanelBackground;
                panelBgImg.type = Image.Type.Sliced;
                panelBgImg.color = Color.white; // Let sprite's own colors show through
                panelBgImg.raycastTarget = true;

                // Panel frame border (decorative overlay)
                if (s.PanelFrame != null)
                {
                    var panelFrame = DOS2Theme.CreateSpriteImage("PanelFrame", frameGO.transform,
                        s.PanelFrame, Color.white); // Let sprite's own colors show through
                    panelFrame.raycastTarget = false;
                }
            }
            else
            {
                // Fallback: gold border + dark fill
                var frameBorderImg = frameGO.AddComponent<Image>();
                frameBorderImg.color = DOS2Theme.GoldAccent;

                var innerFill = DOS2Theme.CreatePanel("FrameFill", frameGO.transform, DOS2Theme.PanelBg);
                var innerFillRect = innerFill.GetComponent<RectTransform>();
                innerFillRect.offsetMin = new Vector2(BorderWidth, BorderWidth);
                innerFillRect.offsetMax = new Vector2(-BorderWidth, -BorderWidth);
                innerFill.raycastTarget = true;
            }

            // ── Slot container (horizontal layout) ──
            var slotContainer = DOS2Theme.CreateUIElement("SlotContainer", frameGO.transform);
            var slotContainerRect = slotContainer.GetComponent<RectTransform>();
            slotContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotContainerRect.pivot = new Vector2(0.5f, 0.5f);
            slotContainerRect.anchoredPosition = new Vector2(0f, 6f);

            var slotLayout = slotContainer.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = SlotSpacing;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;

            var slotFitter = slotContainer.AddComponent<ContentSizeFitter>();
            slotFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            slotFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Build slots — Move + abilities + Cancel + End Turn
            BuildSlots(slotContainer.transform);

            // ── Hint text (below slots, inside frame) ──
            _hintText = DOS2Theme.CreateOutlinedText("HintText", frameGO.transform,
                "", 11, DOS2Theme.TextGray, TextAnchor.MiddleCenter);
            _hintText.fontStyle = FontStyle.Italic;
            var hintRect = _hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 6f);
            hintRect.sizeDelta = new Vector2(-20f, 20f);

            // Adjust bar root width based on total slot count
            int totalSlots = 3 + (_combatController?.CurrentUnit?.Definition.Abilities?.Length ?? 4);
            float barWidth = Mathf.Max(600f, totalSlots * (SlotSize + SlotSpacing) + 80f);
            barRootRect.sizeDelta = new Vector2(barWidth, 160f);
        }

        private void BuildSlots(Transform parent)
        {
            var s = DOS2Theme.Sprites;

            // Move slot
            _moveSlot = CreateAbilitySlot(parent, "Move", "M", 1, "1");

            // Ability slots — built for the current (or first) unit
            RebuildAbilitySlots(parent);

            // Separator (thin line between abilities and cancel/end turn)
            var sep = DOS2Theme.CreateUIElement("Separator", parent);
            var sepRect = sep.GetComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(2f, SlotSize - 8f);
            var sepLayout = sep.AddComponent<LayoutElement>();
            sepLayout.preferredWidth = 2f;
            sepLayout.preferredHeight = SlotSize - 8f;
            var sepImg = sep.AddComponent<Image>();
            if (s != null && s.SeparatorLine != null)
            {
                sepImg.sprite = s.SeparatorLine;
                sepImg.type = Image.Type.Sliced;
                sepImg.color = DOS2Theme.SyntyGold;
            }
            else
            {
                sepImg.color = DOS2Theme.GoldAccent;
            }
            sepImg.raycastTarget = false;

            // Cancel slot
            _cancelSlot = CreateAbilitySlot(parent, "Cancel", "X", 0, "C");

            // End Turn slot
            _endTurnSlot = CreateAbilitySlot(parent, "End Turn", "ET", 0, "Space");
        }

        private void RebuildAbilitySlots(Transform parent)
        {
            // Clear existing ability slots
            foreach (var slot in _abilitySlots)
            {
                if (slot.Root != null)
                    Destroy(slot.Root);
            }
            _abilitySlots.Clear();

            var unit = _combatController?.CurrentUnit;
            var abilities = unit?.Definition.Abilities;
            int count = abilities?.Length ?? 0;

            for (int i = 0; i < count; i++)
            {
                var ability = abilities[i];
                if (ability == null) continue;

                string shortcut = (i + 2).ToString(); // 2,3,4... (1 is Move)
                var slot = CreateAbilitySlot(parent, ability.AbilityName, GetInitials(ability.AbilityName),
                    ability.ApCost, shortcut);
                slot.Ability = ability;
                _abilitySlots.Add(slot);
            }
        }

        private AbilitySlot CreateAbilitySlot(Transform parent, string label, string initials, int apCost, string shortcut)
        {
            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            var slotGO = DOS2Theme.CreateUIElement($"Slot_{label}", parent);
            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            var layoutElem = slotGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = SlotSize;
            layoutElem.preferredHeight = SlotSize;

            // Border / frame
            var borderImg = slotGO.AddComponent<Image>();
            if (hasSprites && s.SlotFrame != null)
            {
                borderImg.sprite = s.SlotFrame;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = DOS2Theme.SyntyFrameGray;
            }
            else
            {
                borderImg.color = DOS2Theme.GoldAccent;
            }

            // Inner dark background
            var innerGO = DOS2Theme.CreateUIElement("Inner", slotGO.transform);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(BorderWidth, BorderWidth);
            innerRect.offsetMax = new Vector2(-BorderWidth, -BorderWidth);

            var innerImg = innerGO.AddComponent<Image>();
            if (hasSprites && s.SlotBackground != null)
            {
                innerImg.sprite = s.SlotBackground;
                innerImg.type = Image.Type.Sliced;
                innerImg.color = DOS2Theme.SyntyDarkBg;
            }
            else
            {
                innerImg.color = DOS2Theme.PanelBgAlt;
            }
            innerImg.raycastTarget = false;

            // Ability initials (center) or placeholder icon
            var initialsText = DOS2Theme.CreateOutlinedText("Initials", innerGO.transform,
                initials, 18, DOS2Theme.TextWhite, TextAnchor.MiddleCenter);
            initialsText.fontStyle = FontStyle.Bold;
            var initRect = initialsText.GetComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.offsetMin = new Vector2(2f, 10f);
            initRect.offsetMax = new Vector2(-2f, -2f);

            // Shortcut number (top-left corner)
            var shortcutText = DOS2Theme.CreateOutlinedText("Shortcut", innerGO.transform,
                shortcut, 10, DOS2Theme.GoldAccent, TextAnchor.UpperLeft);
            shortcutText.fontStyle = FontStyle.Bold;
            var scRect = shortcutText.GetComponent<RectTransform>();
            scRect.anchorMin = Vector2.zero;
            scRect.anchorMax = Vector2.one;
            scRect.offsetMin = new Vector2(4f, 0f);
            scRect.offsetMax = new Vector2(0f, -3f);

            // AP cost badge (bottom-right corner)
            Text costText = null;
            if (apCost > 0)
            {
                var costBadge = DOS2Theme.CreateUIElement("CostBadge", slotGO.transform);
                var costBadgeRect = costBadge.GetComponent<RectTransform>();
                costBadgeRect.anchorMin = new Vector2(1f, 0f);
                costBadgeRect.anchorMax = new Vector2(1f, 0f);
                costBadgeRect.pivot = new Vector2(1f, 0f);
                costBadgeRect.anchoredPosition = new Vector2(2f, -2f);
                costBadgeRect.sizeDelta = new Vector2(18f, 16f);

                var costBg = costBadge.AddComponent<Image>();
                costBg.color = DOS2Theme.PanelBg;
                costBg.raycastTarget = false;

                costText = DOS2Theme.CreateText("Cost", costBadge.transform,
                    apCost.ToString(), 10, DOS2Theme.GoldAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
                var ctRect = costText.GetComponent<RectTransform>();
                ctRect.anchorMin = Vector2.zero;
                ctRect.anchorMax = Vector2.one;
                ctRect.offsetMin = Vector2.zero;
                ctRect.offsetMax = Vector2.zero;
            }

            // Label below slot
            var labelText = DOS2Theme.CreateText("Label", slotGO.transform,
                label, 9, DOS2Theme.TextGray, TextAnchor.UpperCenter);
            var lblRect = labelText.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(1f, 0f);
            lblRect.pivot = new Vector2(0.5f, 1f);
            lblRect.anchoredPosition = new Vector2(0f, -2f);
            lblRect.sizeDelta = new Vector2(SlotSize + 8f, 14f);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 7;
            labelText.resizeTextMaxSize = 10;

            // Button component
            var button = slotGO.AddComponent<Button>();
            button.targetGraphic = borderImg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;

            return new AbilitySlot
            {
                Root = slotGO,
                BorderImage = borderImg,
                InnerImage = innerImg,
                InitialsText = initialsText,
                CostText = costText,
                Button = button,
                Ability = null
            };
        }

        // ── State Refresh (called every frame) ────────────────────────

        private void RefreshState()
        {
            var unit = _combatController.CurrentUnit;
            bool isActive = _combatController.IsCombatActive;
            bool isPlayerTurn = _combatController.IsPlayerTurn;
            bool isAnimating = _combatController.IsActionAnimating;

            var s = DOS2Theme.Sprites;
            bool hasSprites = DOS2Theme.HasSprites;

            // Round badge
            int round = _combatController.TurnManager?.CurrentRound ?? 0;
            if (_roundText != null)
                _roundText.text = $"Round {round}";

            // Unit name
            string unitName = unit != null ? unit.Definition.UnitName : "\u2014";
            if (_unitNameText != null)
                _unitNameText.text = unitName;

            // HP bar
            if (unit != null)
            {
                float hpRatio = (float)unit.CurrentHP / unit.Stats.MaxHP;

                if (_hpUseFillMode)
                    _hpBarFill.fillAmount = hpRatio;
                else if (_hpBarFillRect != null)
                    _hpBarFillRect.anchorMax = new Vector2(hpRatio, 1f);

                if (_hpBarFill != null)
                    _hpBarFill.color = DOS2Theme.GetHPColor(hpRatio);

                if (_hpText != null)
                    _hpText.text = $"{unit.CurrentHP}/{unit.Stats.MaxHP}";
            }

            // AP pips
            int currentAP = unit?.CurrentAP ?? 0;
            int maxAP = unit?.MaxAP ?? MaxAPPips;
            for (int i = 0; i < _apPips.Count; i++)
            {
                if (i >= maxAP)
                {
                    _apPips[i].gameObject.SetActive(false);
                    continue;
                }

                _apPips[i].gameObject.SetActive(true);
                bool isFull = i < currentAP;

                if (hasSprites && s.APGemFull != null)
                {
                    _apPips[i].sprite = isFull ? s.APGemFull : (s.APGemEmpty != null ? s.APGemEmpty : s.APGemFull);
                    _apPips[i].color = isFull ? DOS2Theme.APGreen : new Color(0.3f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    _apPips[i].color = isFull ? DOS2Theme.APGreen : DOS2Theme.PanelBgAlt;
                }
            }

            // Button interactivity
            bool canInteract = isActive && isPlayerTurn && !isAnimating && unit != null;
            var queued = _combatController.QueuedAction;
            var queuedAbility = _combatController.QueuedAbility;

            // Move button
            if (_moveSlot.Button != null)
            {
                bool canMove = canInteract && _combatController.ActionSystem.CanMove(unit);
                _moveSlot.Button.interactable = canMove;
                UpdateSlotHighlight(_moveSlot, queued == CombatSceneController.QueuedActionType.Move, hasSprites, s);
                SetSlotAlpha(_moveSlot, canMove ? 1f : 0.4f);
            }

            // Ability slots
            for (int i = 0; i < _abilitySlots.Count; i++)
            {
                var slot = _abilitySlots[i];
                if (slot.Ability == null || slot.Button == null) continue;

                bool canAfford = canInteract && _combatController.ActionSystem.CanUseAbility(unit, slot.Ability);
                slot.Button.interactable = canAfford;
                bool isQueued = queued == CombatSceneController.QueuedActionType.Ability && queuedAbility == slot.Ability;
                UpdateSlotHighlight(slot, isQueued, hasSprites, s);
                SetSlotAlpha(slot, canAfford ? 1f : 0.4f);
            }

            // Cancel / End Turn
            if (_cancelSlot.Button != null)
            {
                _cancelSlot.Button.interactable = canInteract;
                SetSlotAlpha(_cancelSlot, canInteract ? 1f : 0.4f);
            }
            if (_endTurnSlot.Button != null)
            {
                _endTurnSlot.Button.interactable = canInteract;
                SetSlotAlpha(_endTurnSlot, canInteract ? 1f : 0.4f);
            }

            // Hint text
            if (_hintText != null)
            {
                if (!isActive)
                    _hintText.text = _combatController.State.ToString();
                else if (!isPlayerTurn)
                    _hintText.text = "Enemy turn in progress...";
                else if (isAnimating)
                    _hintText.text = "Action in progress...";
                else
                    _hintText.text = TacticalInputHandler.PendingActionHint
                                     ?? _combatController.GetQueuedActionHint();
            }
        }

        private static void UpdateSlotHighlight(AbilitySlot slot, bool isHighlighted, bool hasSprites, HUDSpriteConfig s)
        {
            if (slot.BorderImage == null) return;

            if (hasSprites && s.SlotHighlight != null)
            {
                slot.BorderImage.sprite = isHighlighted ? s.SlotHighlight : s.SlotFrame;
                slot.BorderImage.color = isHighlighted ? DOS2Theme.GoldHighlight : DOS2Theme.SyntyFrameGray;
            }
            else
            {
                slot.BorderImage.color = isHighlighted ? DOS2Theme.GoldHighlight : DOS2Theme.GoldAccent;
            }
        }

        private static void SetSlotAlpha(AbilitySlot slot, float alpha)
        {
            if (slot.InnerImage != null)
            {
                var c = slot.InnerImage.color;
                slot.InnerImage.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (slot.InitialsText != null)
            {
                var c = slot.InitialsText.color;
                slot.InitialsText.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        // ── Event Handlers ────────────────────────────────────────────

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            RebuildAllSlots();
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            // Round text updated in RefreshState
        }

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            // HP updated in RefreshState
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            // HP updated in RefreshState
        }

        /// <summary>
        /// Rebuild all hotbar slots for the current unit's abilities.
        /// Called when the active unit changes.
        /// </summary>
        private void RebuildAllSlots()
        {
            if (_moveSlot.Root == null) return;

            var parent = _moveSlot.Root.transform.parent;

            // Destroy everything in the container
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);

            _abilitySlots.Clear();
            _moveSlot = default;
            _cancelSlot = default;
            _endTurnSlot = default;

            // Rebuild
            BuildSlots(parent);
            WireButtonCallbacks();

            // Resize bar root
            var barRoot = parent.parent.GetComponent<RectTransform>();
            if (barRoot != null)
            {
                int totalSlots = 3 + _abilitySlots.Count + 1;
                float barWidth = Mathf.Max(600f, totalSlots * (SlotSize + SlotSpacing) + 80f);
                barRoot.sizeDelta = new Vector2(barWidth, barRoot.sizeDelta.y);
            }
        }

        /// <summary>Wire click callbacks for all buttons. Called after slot creation.</summary>
        public void WireButtonCallbacks()
        {
            if (_moveSlot.Button != null)
                _moveSlot.Button.onClick.AddListener(() => _combatController?.QueueMoveAction());

            for (int i = 0; i < _abilitySlots.Count; i++)
            {
                var slot = _abilitySlots[i];
                if (slot.Button != null && slot.Ability != null)
                {
                    var ability = slot.Ability;
                    slot.Button.onClick.AddListener(() => _combatController?.QueueAbility(ability));
                }
            }

            if (_cancelSlot.Button != null)
                _cancelSlot.Button.onClick.AddListener(() => _combatController?.ClearQueuedAction());

            if (_endTurnSlot.Button != null)
                _endTurnSlot.Button.onClick.AddListener(() => _combatController?.EndCurrentTurn());
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        // ── Slot Data ─────────────────────────────────────────────────

        private struct AbilitySlot
        {
            public GameObject Root;
            public Image BorderImage;
            public Image InnerImage;
            public Text InitialsText;
            public Text CostText;
            public Button Button;
            public AbilityDefinition Ability;
        }
    }
}
