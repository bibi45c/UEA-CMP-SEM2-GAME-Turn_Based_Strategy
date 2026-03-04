using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TurnBasedTactics.Core;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// DOS2-style combat log panel (bottom-right corner).
    /// Displays scrolling text feed of combat events.
    /// </summary>
    public class CombatLog : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _logText;
        private RectTransform _contentRect;
        private RectTransform _viewportRect;
        private UnitRegistry _registry;
        private readonly List<string> _logEntries = new List<string>();

        private const int MaxVisibleLines = 10;
        private const int MaxLogEntries = 80;
        private const float PanelWidth = 380f;
        private const float PanelHeight = 200f;
        private const float BottomMargin = 160f;
        private const float RightMargin = 10f;

        public void Initialize(UnitRegistry registry)
        {
            _registry = registry;
        }

        private void Awake()
        {
            BuildUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGO = new GameObject("CombatLogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 8;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel (bottom-right)
            var panelGO = DOS2Theme.CreateUIElement("LogPanel", canvasGO.transform);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.80f);
            bgImage.raycastTarget = false;

            var outline = panelGO.AddComponent<Outline>();
            outline.effectColor = new Color(DOS2Theme.GoldAccent.r, DOS2Theme.GoldAccent.g, DOS2Theme.GoldAccent.b, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Title
            var titleGO = DOS2Theme.CreateUIElement("Title", panelGO.transform);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 22f);

            var titleBg = titleGO.AddComponent<Image>();
            titleBg.color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
            titleBg.raycastTarget = false;

            var titleText = DOS2Theme.CreateText("TitleText", titleGO.transform,
                "Combat Log", 12, DOS2Theme.GoldAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;

            // Text area (fill remaining space below title, with mask)
            var maskGO = DOS2Theme.CreateUIElement("Mask", panelGO.transform);
            var maskRect = maskGO.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(6f, 4f);
            maskRect.offsetMax = new Vector2(-6f, -24f);

            var maskImg = maskGO.AddComponent<Image>();
            maskImg.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but needed for mask
            maskImg.raycastTarget = false;

            var mask = maskGO.AddComponent<RectMask2D>();
            _viewportRect = maskRect;

            // Log text — anchored to bottom-left, grows upward
            var textGO = DOS2Theme.CreateUIElement("LogText", maskGO.transform);
            _contentRect = textGO.GetComponent<RectTransform>();
            _contentRect.anchorMin = new Vector2(0f, 0f);
            _contentRect.anchorMax = new Vector2(1f, 0f);
            _contentRect.pivot = new Vector2(0f, 0f);
            _contentRect.anchoredPosition = Vector2.zero;
            _contentRect.sizeDelta = new Vector2(0f, 500f); // Tall enough for all text

            _logText = textGO.AddComponent<Text>();
            _logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _logText.fontSize = 13;
            _logText.lineSpacing = 1.2f;
            _logText.color = DOS2Theme.TextWhite;
            _logText.alignment = TextAnchor.LowerLeft;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Truncate;
            _logText.supportRichText = true;
            _logText.raycastTarget = false;
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<UnitMoveCompletedEvent>(OnUnitMoved);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<UnitMoveCompletedEvent>(OnUnitMoved);
        }

        private string GetUnitName(int unitId)
        {
            if (_registry == null) return $"Unit#{unitId}";
            var unit = _registry.GetUnit(unitId);
            return unit?.Definition.UnitName ?? $"Unit#{unitId}";
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            AddLogEntry("--- Combat Started ---", DOS2Theme.GoldAccent);
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            AddLogEntry($"=== Round {evt.RoundNumber} ===", DOS2Theme.GoldAccent);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            string name = GetUnitName(evt.UnitId);
            string tag = evt.IsPlayerControlled ? "" : " [AI]";
            AddLogEntry($"<b>{name}</b>'s turn{tag}", DOS2Theme.TextWhite);
        }

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            string target = GetUnitName(evt.TargetUnitId);
            string attacker = GetUnitName(evt.AttackerUnitId);
            string crit = evt.WasCritical ? " CRIT!" : "";
            AddLogEntry($"  {attacker} -> {target}: <b>{evt.DamageAmount}</b> dmg{crit}", DOS2Theme.EnemyRed);
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            string target = GetUnitName(evt.TargetUnitId);
            AddLogEntry($"  {target} healed <b>+{evt.HealAmount}</b> HP", DOS2Theme.HPGreen);
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            string name = GetUnitName(evt.UnitId);
            AddLogEntry($"  ** {name} defeated! **", new Color(1f, 0.3f, 0.3f));
        }

        private void OnUnitMoved(UnitMoveCompletedEvent evt)
        {
            string name = GetUnitName(evt.UnitId);
            AddLogEntry($"  {name} moved {evt.HexesMoved} hex", DOS2Theme.TextGray);
        }

        public void AddLogEntry(string message, Color color)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            _logEntries.Add($"<color=#{colorHex}>{message}</color>");

            // Trim old entries
            while (_logEntries.Count > MaxLogEntries)
                _logEntries.RemoveAt(0);

            // Rebuild text (show only last N entries to keep it readable)
            int start = Mathf.Max(0, _logEntries.Count - MaxVisibleLines);
            var sb = new System.Text.StringBuilder();
            for (int i = start; i < _logEntries.Count; i++)
            {
                if (i > start) sb.Append('\n');
                sb.Append(_logEntries[i]);
            }
            _logText.text = sb.ToString();
        }

        public void ClearLog()
        {
            _logEntries.Clear();
            _logText.text = "";
        }
    }
}
