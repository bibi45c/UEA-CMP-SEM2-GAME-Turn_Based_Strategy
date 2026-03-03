using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Full-screen Victory/Defeat overlay shown when combat ends.
    /// Subscribes to CombatEndedEvent. Tracks kills via UnitDiedEvent.
    /// Uses uGUI Canvas (ScreenSpaceOverlay, sortingOrder=100).
    /// </summary>
    public class CombatResultsScreen : MonoBehaviour
    {
        // --- Color palette ---
        private static readonly Color VictoryTitleColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefeatTitleColor = new Color(0.85f, 0.15f, 0.15f, 1f);
        private static readonly Color OverlayBgColor = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Color PanelBgColor = new Color(0.1f, 0.1f, 0.15f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.15f, 0.35f, 0.6f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.2f, 0.45f, 0.75f, 1f);
        private static readonly Color DividerColor = new Color(1f, 0.85f, 0.2f, 0.4f);
        private static readonly Color StatLabelColor = new Color(0.7f, 0.7f, 0.75f, 1f);
        private static readonly Color StatValueColor = Color.white;

        // --- References ---
        private UnitRegistry _registry;
        private CombatSceneController _combatController;

        // --- Kill tracking ---
        private readonly Dictionary<int, UnitSnapshot> _unitSnapshots = new Dictionary<int, UnitSnapshot>();
        private int _initialPlayerCount;
        private int _initialEnemyCount;
        private int _playerKills;
        private int _enemyKills;

        // --- UI elements ---
        private GameObject _canvasGO;
        private Image _bgImage;
        private Text _titleText;
        private Text _roundsText;
        private Text _enemiesText;
        private Text _alliesText;
        private Transform _survivorListParent;
        private GameObject _continueButton;
        private bool _isShown;

        private struct UnitSnapshot
        {
            public string Name;
            public int TeamId;
            public int MaxHP;
        }

        public void Initialize(UnitRegistry registry, CombatSceneController combatController)
        {
            _registry = registry;
            _combatController = combatController;

            // Snapshot all current units for kill tracking
            foreach (var unit in registry.AllUnits)
            {
                _unitSnapshots[unit.UnitId] = new UnitSnapshot
                {
                    Name = unit.Definition.UnitName,
                    TeamId = unit.TeamId,
                    MaxHP = unit.Stats.MaxHP
                };

                if (unit.TeamId == TurnManager.PlayerTeamId)
                    _initialPlayerCount++;
                else
                    _initialEnemyCount++;
            }

            CreateCanvas();
            _canvasGO.SetActive(false);

            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            Debug.Log("[CombatResultsScreen] Initialized.");
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (!_unitSnapshots.TryGetValue(evt.UnitId, out var snapshot))
                return;

            if (snapshot.TeamId == TurnManager.PlayerTeamId)
                _playerKills++;
            else
                _enemyKills++;
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            bool isVictory = evt.WinningTeamId == TurnManager.PlayerTeamId;
            StartCoroutine(ShowResultsCoroutine(isVictory));
        }

        private IEnumerator ShowResultsCoroutine(bool isVictory)
        {
            // Brief delay to let death animation play
            yield return new WaitForSeconds(1.5f);

            if (_isShown) yield break;
            _isShown = true;

            PopulateResults(isVictory);
            _canvasGO.SetActive(true);

            // Fade in background
            float elapsed = 0f;
            float fadeDuration = 0.6f;
            Color startColor = new Color(0f, 0f, 0f, 0f);
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                _bgImage.color = Color.Lerp(startColor, OverlayBgColor, t);
                yield return null;
            }
            _bgImage.color = OverlayBgColor;
        }

        private void PopulateResults(bool isVictory)
        {
            // Title
            _titleText.text = isVictory ? "VICTORY" : "DEFEAT";
            _titleText.color = isVictory ? VictoryTitleColor : DefeatTitleColor;

            // Stats
            int rounds = _combatController.TurnManager.CurrentRound;
            _roundsText.text = $"{rounds}";
            _enemiesText.text = $"{_enemyKills} / {_initialEnemyCount}";
            int alliesSurvived = _initialPlayerCount - _playerKills;
            _alliesText.text = $"{alliesSurvived} / {_initialPlayerCount}";

            // Survivor list
            BuildSurvivorList(isVictory);

            // Continue button only on victory
            _continueButton.SetActive(isVictory);
        }

        private void BuildSurvivorList(bool isVictory)
        {
            // Clear existing entries
            foreach (Transform child in _survivorListParent)
                Destroy(child.gameObject);

            // Show surviving player units
            var survivors = _registry.GetTeamUnits(TurnManager.PlayerTeamId);
            if (survivors.Count == 0 && !isVictory)
            {
                AddSurvivorEntry("No survivors", 0, 1, false);
                return;
            }

            foreach (var unit in survivors)
            {
                AddSurvivorEntry(unit.Definition.UnitName, unit.CurrentHP, unit.Stats.MaxHP, true);
            }
        }

        private void AddSurvivorEntry(string name, int currentHP, int maxHP, bool alive)
        {
            var entryGO = new GameObject("SurvivorEntry");
            entryGO.transform.SetParent(_survivorListParent, false);

            var layout = entryGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = entryGO.AddComponent<LayoutElement>();
            fitter.preferredHeight = 28f;

            // Unit name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(entryGO.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.text = alive ? $"  {name}" : $"  {name}";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 20;
            nameText.color = alive ? StatValueColor : StatLabelColor;
            var nameLayout = nameGO.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 200f;

            // HP text
            var hpGO = new GameObject("HP");
            hpGO.transform.SetParent(entryGO.transform, false);
            var hpText = hpGO.AddComponent<Text>();
            hpText.text = alive ? $"{currentHP} / {maxHP} HP" : "";
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 20;
            hpText.color = alive ? HPColor(currentHP, maxHP) : StatLabelColor;
            var hpLayout = hpGO.AddComponent<LayoutElement>();
            hpLayout.preferredWidth = 140f;
        }

        private static Color HPColor(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0f;
            if (ratio > 0.5f) return new Color(0.3f, 0.9f, 0.35f);
            if (ratio > 0.25f) return new Color(0.9f, 0.8f, 0.2f);
            return new Color(0.9f, 0.25f, 0.2f);
        }

        // ======= Canvas Construction =======

        private void CreateCanvas()
        {
            _canvasGO = new GameObject("ResultsCanvas");
            _canvasGO.transform.SetParent(transform, false);

            var canvas = _canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGO.AddComponent<GraphicRaycaster>();

            // Full-screen background
            var bgGO = CreateImage(_canvasGO.transform, "Background", OverlayBgColor);
            StretchFill(bgGO);
            _bgImage = bgGO.GetComponent<Image>();

            // Center content panel
            var contentGO = CreatePanel(bgGO.transform, "ContentPanel", new Color(0, 0, 0, 0));
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.sizeDelta = new Vector2(520, 500);
            contentRT.anchoredPosition = Vector2.zero;
            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.padding = new RectOffset(30, 30, 30, 30);

            // Inner panel background
            var innerBgGO = CreateImage(contentGO.transform, "InnerBg", PanelBgColor);
            StretchFill(innerBgGO);
            innerBgGO.transform.SetAsFirstSibling();
            var innerBg = innerBgGO.GetComponent<Image>();
            innerBg.raycastTarget = false;

            // Title
            _titleText = CreateText(contentGO.transform, "Title", "VICTORY", 64, VictoryTitleColor);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            var titleLayout = _titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 80f;

            // Divider
            var dividerGO = CreateImage(contentGO.transform, "Divider", DividerColor);
            var dividerLayout = dividerGO.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 2f;

            // Stats section
            var statsGO = CreatePanel(contentGO.transform, "StatsPanel", new Color(0, 0, 0, 0));
            var statsLayout = statsGO.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 6f;
            statsLayout.childAlignment = TextAnchor.UpperLeft;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            var statsLE = statsGO.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 100f;

            _roundsText = CreateStatRow(statsGO.transform, "Rounds Completed");
            _enemiesText = CreateStatRow(statsGO.transform, "Enemies Defeated");
            _alliesText = CreateStatRow(statsGO.transform, "Allies Survived");

            // Survivor label
            var survivorLabel = CreateText(contentGO.transform, "SurvivorLabel", "Surviving Units", 22, StatLabelColor);
            survivorLabel.alignment = TextAnchor.MiddleCenter;
            survivorLabel.fontStyle = FontStyle.Italic;
            var survivorLabelLE = survivorLabel.gameObject.AddComponent<LayoutElement>();
            survivorLabelLE.preferredHeight = 28f;

            // Survivor list container
            var survivorGO = CreatePanel(contentGO.transform, "SurvivorList", new Color(0, 0, 0, 0));
            var survivorLayout = survivorGO.AddComponent<VerticalLayoutGroup>();
            survivorLayout.spacing = 2f;
            survivorLayout.childAlignment = TextAnchor.UpperCenter;
            survivorLayout.childForceExpandWidth = true;
            survivorLayout.childForceExpandHeight = false;
            var survivorLE = survivorGO.AddComponent<LayoutElement>();
            survivorLE.preferredHeight = 100f;
            _survivorListParent = survivorGO.transform;

            // Button panel
            var buttonPanelGO = CreatePanel(contentGO.transform, "ButtonPanel", new Color(0, 0, 0, 0));
            var buttonPanelLayout = buttonPanelGO.AddComponent<HorizontalLayoutGroup>();
            buttonPanelLayout.spacing = 20f;
            buttonPanelLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonPanelLayout.childForceExpandWidth = false;
            buttonPanelLayout.childForceExpandHeight = false;
            var buttonPanelLE = buttonPanelGO.AddComponent<LayoutElement>();
            buttonPanelLE.preferredHeight = 50f;

            CreateButton(buttonPanelGO.transform, "RestartButton", "Restart Battle", OnRestartClicked);
            _continueButton = CreateButton(buttonPanelGO.transform, "ContinueButton", "Continue", OnContinueClicked);
        }

        // ======= UI Factory Helpers =======

        private Text CreateStatRow(Transform parent, string label)
        {
            var rowGO = new GameObject(label.Replace(" ", ""));
            rowGO.transform.SetParent(parent, false);

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 28f;

            // Label
            var labelText = CreateText(rowGO.transform, "Label", $"  {label}:", 22, StatLabelColor);
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 260f;

            // Value
            var valueText = CreateText(rowGO.transform, "Value", "0", 22, StatValueColor);
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.fontStyle = FontStyle.Bold;

            return valueText;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            // Outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            return text;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            // Image already adds a RectTransform — no need to add another

            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            if (bgColor.a > 0.001f)
            {
                var img = go.AddComponent<Image>();
                img.color = bgColor;
                img.raycastTarget = false;
            }

            return go;
        }

        private GameObject CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = ButtonColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.pressedColor = new Color(0.1f, 0.25f, 0.5f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 200f;
            le.preferredHeight = 44f;

            // Button label
            var labelText = CreateText(go.transform, "Label", label, 22, Color.white);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;
            var labelRT = labelText.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            return go;
        }

        private static void StretchFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ======= Button Callbacks =======

        private void OnRestartClicked()
        {
            Debug.Log("[CombatResultsScreen] Restarting battle...");
            SceneTransitionManager.Instance.RestartCurrentScene();
        }

        private void OnContinueClicked()
        {
            if (EncounterTracker.HasNextEncounter())
            {
                EncounterTracker.AdvanceEncounter();
                var nextScene = EncounterTracker.ActiveList.GetSceneName(
                    EncounterTracker.CurrentEncounterIndex);
                Debug.Log($"[CombatResultsScreen] Advancing to encounter: {nextScene}");
                SceneTransitionManager.Instance.TransitionToScene(nextScene);
            }
            else
            {
                // No more encounters — restart for now
                Debug.Log("[CombatResultsScreen] No next encounter, restarting...");
                SceneTransitionManager.Instance.RestartCurrentScene();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }
    }
}
