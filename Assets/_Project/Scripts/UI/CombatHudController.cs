using UnityEngine;
using UnityEngine.InputSystem;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Lightweight immediate-mode combat HUD.
    /// Draws a round banner and simple action buttons without scene-authored UI.
    /// </summary>
    public class CombatHudController : MonoBehaviour
    {
        private CombatSceneController _combatController;
        private GUIStyle _bannerStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _activeButtonStyle;

        private static readonly Color ActiveButtonColor = new Color(0.3f, 0.7f, 1f, 1f);
        private static readonly Color AnimatingColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

        // Scale factors
        private const float TopScale = 2f;
        private const float BottomScale = 3f;

        // Cached rects for UI-over-world blocking
        private Rect _bannerScreenRect;
        private Rect _panelScreenRect;

        /// <summary>
        /// Returns true if the current mouse position is over any OnGUI HUD element.
        /// Call from Update() to block world clicks beneath the HUD.
        /// </summary>
        public static bool IsMouseOverHud { get; private set; }

        private void Update()
        {
            if (Mouse.current == null) return;

            // Convert OnGUI rects (top-left origin, Y-down) to screen coords and test
            Vector2 mouse = Mouse.current.position.ReadValue();
            // OnGUI Y is inverted relative to screen-space mouse position
            float guiMouseY = Screen.height - mouse.y;
            IsMouseOverHud = _bannerScreenRect.Contains(new Vector2(mouse.x, guiMouseY))
                          || _panelScreenRect.Contains(new Vector2(mouse.x, guiMouseY));
        }

        public void Initialize(CombatSceneController combatController)
        {
            _combatController = combatController;
        }

        private void OnGUI()
        {
            if (_combatController == null)
                return;

            EnsureStyles();
            DrawRoundBanner();
            DrawActionPanel();
        }

        private void DrawRoundBanner()
        {
            int round = _combatController.TurnManager?.CurrentRound ?? 0;
            float w = 180f * TopScale;
            float h = 36f * TopScale;
            Rect rect = new Rect(12f, 12f, w, h);
            _bannerScreenRect = rect;
            GUI.Box(rect, $"Round {round}", _bannerStyle);
        }

        private void DrawActionPanel()
        {
            float panelW = Mathf.Min(520f * BottomScale, Screen.width - 40f);
            float panelH = 120f * BottomScale;
            Rect panelRect = new Rect(20f, Screen.height - panelH - 20f, panelW, panelH);
            _panelScreenRect = panelRect;
            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            float textX = panelRect.x + 16f * BottomScale;
            float buttonY = panelRect.y + 52f * BottomScale;
            float buttonWidth = 90f * BottomScale;
            float buttonHeight = 32f * BottomScale;
            float spacing = 10f * BottomScale;

            string activeUnitName = _combatController.CurrentUnit != null
                ? _combatController.CurrentUnit.Definition.UnitName
                : "None";

            GUI.Label(
                new Rect(textX, panelRect.y + 12f * BottomScale, panelRect.width - 32f * BottomScale, 24f * BottomScale),
                $"Active Unit: {activeUnitName}",
                _titleStyle);

            if (!_combatController.IsCombatActive)
            {
                GUI.Label(
                    new Rect(textX, buttonY, panelRect.width - 32f * BottomScale, 24f * BottomScale),
                    _combatController.State.ToString(),
                    _hintStyle);
                return;
            }

            if (!_combatController.IsPlayerTurn || _combatController.CurrentUnit == null)
            {
                GUI.Label(
                    new Rect(textX, buttonY, panelRect.width - 32f * BottomScale, 24f * BottomScale),
                    "Enemy turn in progress...",
                    _hintStyle);
                return;
            }

            bool isAnimating = _combatController.IsActionAnimating;
            bool canMove = !isAnimating && _combatController.ActionSystem.CanMove(_combatController.CurrentUnit);
            bool canUseMainAction = !isAnimating && _combatController.ActionSystem.CanAct(_combatController.CurrentUnit);
            var queued = _combatController.QueuedAction;

            bool previousGuiEnabled = GUI.enabled;
            Color savedBg = GUI.backgroundColor;

            // Show animating state
            if (isAnimating)
            {
                GUI.Label(
                    new Rect(textX, buttonY + buttonHeight + 10f * BottomScale, panelRect.width - 32f * BottomScale, 24f * BottomScale),
                    "Action in progress...",
                    _hintStyle);
                GUI.backgroundColor = AnimatingColor;
                GUI.enabled = false;

                GUI.Button(new Rect(textX, buttonY, buttonWidth, buttonHeight), "Move", _buttonStyle);
                GUI.Button(new Rect(textX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight), "Attack", _buttonStyle);
                GUI.Button(new Rect(textX + (buttonWidth + spacing) * 2f, buttonY, buttonWidth, buttonHeight), "Heal", _buttonStyle);
                GUI.Button(new Rect(textX + (buttonWidth + spacing) * 3f, buttonY, buttonWidth, buttonHeight), "Cancel", _buttonStyle);
                GUI.Button(new Rect(textX + (buttonWidth + spacing) * 4f, buttonY, buttonWidth, buttonHeight), "End Turn", _buttonStyle);

                GUI.enabled = previousGuiEnabled;
                GUI.backgroundColor = savedBg;
                return;
            }

            // Move button (highlighted when queued)
            GUI.enabled = canMove;
            GUI.backgroundColor = queued == CombatSceneController.QueuedActionType.Move ? ActiveButtonColor : savedBg;
            if (GUI.Button(new Rect(textX, buttonY, buttonWidth, buttonHeight), "Move", _buttonStyle))
                _combatController.QueueMoveAction();

            // Attack button
            GUI.enabled = canUseMainAction;
            GUI.backgroundColor = queued == CombatSceneController.QueuedActionType.Attack ? ActiveButtonColor : savedBg;
            if (GUI.Button(new Rect(textX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight), "Attack", _buttonStyle))
                _combatController.QueueAttackAction();

            // Heal button
            GUI.backgroundColor = queued == CombatSceneController.QueuedActionType.Heal ? ActiveButtonColor : savedBg;
            if (GUI.Button(new Rect(textX + (buttonWidth + spacing) * 2f, buttonY, buttonWidth, buttonHeight), "Heal", _buttonStyle))
                _combatController.QueueHealAction();

            GUI.backgroundColor = savedBg;
            GUI.enabled = previousGuiEnabled;

            // Cancel button
            if (GUI.Button(new Rect(textX + (buttonWidth + spacing) * 3f, buttonY, buttonWidth, buttonHeight), "Cancel", _buttonStyle))
                _combatController.ClearQueuedAction();

            // End Turn button (Space)
            if (GUI.Button(new Rect(textX + (buttonWidth + spacing) * 4f, buttonY, buttonWidth, buttonHeight), "End Turn", _buttonStyle))
                _combatController.EndCurrentTurn();

            // Show pending move confirmation hint if active, otherwise regular hint
            string hint = TacticalInputHandler.PendingActionHint ?? _combatController.GetQueuedActionHint();
            GUI.Label(
                new Rect(textX, buttonY + buttonHeight + 10f * BottomScale, panelRect.width - 32f * BottomScale, 24f * BottomScale),
                hint,
                _hintStyle);
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
                return;

            _bannerStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(20 * TopScale),
                fontStyle = FontStyle.Bold
            };
            _bannerStyle.normal.textColor = Color.white;

            _panelStyle = new GUIStyle(GUI.skin.box);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * BottomScale),
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(12 * BottomScale),
                wordWrap = true
            };
            _hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14 * BottomScale),
                fontStyle = FontStyle.Bold
            };
        }
    }
}
