using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Simple exploration-mode party status panel (left side).
    /// Shows unit names, levels, and full HP bars.
    /// Lightweight version of PartyPortraitPanel — no combat state needed.
    /// </summary>
    public class ExplorationHUD : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _panelContainer;
        private readonly List<GameObject> _slotRoots = new List<GameObject>();

        private const float SlotWidth = 150f;
        private const float SlotHeight = 44f;
        private const float SlotSpacing = 3f;
        private const float PanelPadding = 6f;
        private const float LeftMargin = 10f;
        private const float TopOffset = 10f;

        public void Initialize(UnitDefinition leader, UnitDefinition[] followers)
        {
            CreateCanvas();

            var units = new List<UnitDefinition> { leader };
            if (followers != null)
            {
                foreach (var f in followers)
                {
                    if (f != null) units.Add(f);
                }
            }

            foreach (var def in units)
            {
                CreateSlot(def);
            }
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("ExplorationHUDCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Container — top-left
            var containerGO = new GameObject("PartyStatusContainer", typeof(RectTransform));
            containerGO.transform.SetParent(canvasGO.transform, false);

            _panelContainer = containerGO.GetComponent<RectTransform>();
            _panelContainer.anchorMin = new Vector2(0f, 1f);
            _panelContainer.anchorMax = new Vector2(0f, 1f);
            _panelContainer.pivot = new Vector2(0f, 1f);
            _panelContainer.anchoredPosition = new Vector2(LeftMargin, -TopOffset);

            // Background panel
            var bgImg = containerGO.AddComponent<Image>();
            bgImg.color = DOS2Theme.PanelBg70;
            bgImg.raycastTarget = false;

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

        private void CreateSlot(UnitDefinition def)
        {
            // Compute max HP from definition: 20 + Con*3 + Level*5
            int maxHP = 20 + def.Constitution * 3 + def.Level * 5;

            var slotGO = DOS2Theme.CreateUIElement($"Slot_{def.UnitName}", _panelContainer);
            var slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);

            var le = slotGO.AddComponent<LayoutElement>();
            le.preferredWidth = SlotWidth;
            le.preferredHeight = SlotHeight;

            // Bordered frame
            var (border, fill) = DOS2Theme.CreateBorderedPanel("Frame", slotGO.transform,
                DOS2Theme.SyntyFrameGray, DOS2Theme.PanelBgAlt, 1.5f);

            // Unit name + level (upper portion)
            var nameText = DOS2Theme.CreateOutlinedText("Name", fill.transform,
                $"{def.UnitName}  Lv.{def.Level}", 12, DOS2Theme.TextWhite,
                TextAnchor.MiddleLeft);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(6f, 0f);
            nameRect.offsetMax = new Vector2(-4f, -2f);

            // HP bar background (lower portion)
            var hpBgGO = DOS2Theme.CreateUIElement("HPBarBg", fill.transform);
            var hpBgRect = hpBgGO.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0.04f, 0.12f);
            hpBgRect.anchorMax = new Vector2(0.96f, 0.45f);
            hpBgRect.offsetMin = Vector2.zero;
            hpBgRect.offsetMax = Vector2.zero;

            var hpBgImg = hpBgGO.AddComponent<Image>();
            hpBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            hpBgImg.raycastTarget = false;

            // HP bar fill (always 100% in exploration)
            var hpFillGO = DOS2Theme.CreateUIElement("HPBarFill", hpBgGO.transform);
            var hpFillRect = hpFillGO.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;

            var hpFillImg = hpFillGO.AddComponent<Image>();
            hpFillImg.color = DOS2Theme.HPGreen;
            hpFillImg.raycastTarget = false;

            // HP text overlay
            var hpText = DOS2Theme.CreateOutlinedText("HPText", hpBgGO.transform,
                $"{maxHP}/{maxHP}", 9, Color.white, TextAnchor.MiddleCenter);
            var hpTextRect = hpText.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;

            _slotRoots.Add(slotGO);
        }

        public void Cleanup()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
