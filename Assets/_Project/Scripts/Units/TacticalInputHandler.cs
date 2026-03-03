using UnityEngine;
using UnityEngine.InputSystem;
using TurnBasedTactics.Core;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Combat;
using TacticalCam = global::TurnBasedTactics.Camera.TacticalCamera;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Thin input router for tactical gameplay.
    /// Reads left-click (select) and right-click (move command) from the Input System,
    /// dispatches to UnitSelectionManager and UnitMovementSystem.
    /// </summary>
    public class TacticalInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _tacticalMap;
        private InputAction _selectAction;
        private InputAction _moveCommandAction;
        private InputAction _mousePositionAction;
        private InputAction _endTurnAction;
        private InputAction _previousUnitAction;
        private InputAction _nextUnitAction;

        private UnitSelectionManager _selectionManager;
        private UnitMovementSystem _movementSystem;
        private TacticalCam _camera;
        private HexGridMap _gridMap;
        private CombatSceneController _combatController;
        private MovementRangeVisualizer _rangeVisualizer;
        private bool _initialized;

        public void Initialize(
            UnitSelectionManager selectionManager,
            UnitMovementSystem movementSystem,
            TacticalCam camera,
            HexGridMap gridMap,
            MovementRangeVisualizer rangeVisualizer = null)
        {
            _selectionManager = selectionManager;
            _movementSystem = movementSystem;
            _camera = camera;
            _gridMap = gridMap;
            _rangeVisualizer = rangeVisualizer;
            _initialized = true;

            // Try to find CombatSceneController (optional — works without it in free-roam)
            _combatController = FindAnyObjectByType<CombatSceneController>();

            // Subscribe to turn start for auto-selection
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[TacticalInputHandler] InputActionAsset not assigned!");
                return;
            }

            _tacticalMap = _inputActions.FindActionMap("TacticalCamera");
            if (_tacticalMap == null)
            {
                Debug.LogError("[TacticalInputHandler] TacticalCamera action map not found!");
                return;
            }

            // Disable all maps first, then only enable TacticalCamera
            _inputActions.Disable();

            // All actions are pre-defined in the InputActionAsset — just look them up
            _moveCommandAction = _tacticalMap.FindAction("MoveCommand");
            _mousePositionAction = _tacticalMap.FindAction("MousePosition");
            _selectAction = _tacticalMap.FindAction("Select");
            _endTurnAction = _tacticalMap.FindAction("EndTurn");
            _previousUnitAction = _tacticalMap.FindAction("PreviousUnit");
            _nextUnitAction = _tacticalMap.FindAction("NextUnit");

            _tacticalMap.Enable();
        }


        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        }

        private void Update()
        {
            if (!_initialized || _tacticalMap == null) return;

            // Block all tactical input during action animations
            if (_combatController != null && _combatController.IsActionAnimating) return;

            // Left click: select/deselect unit
            if (_selectAction != null && _selectAction.WasPressedThisFrame())
            {
                HandleSelect();
            }

            // Right click: move command
            if (_moveCommandAction != null && _moveCommandAction.WasPressedThisFrame())
            {
                HandleMoveCommand();
            }

            // Space key: end turn
            if (_endTurnAction != null && _endTurnAction.WasPressedThisFrame())
            {
                HandleEndTurn();
            }

            if (_previousUnitAction != null && _previousUnitAction.WasPressedThisFrame())
            {
                HandleCycleUnit(-1);
            }

            if (_nextUnitAction != null && _nextUnitAction.WasPressedThisFrame())
            {
                HandleCycleUnit(1);
            }
        }

        private void HandleSelect()
        {
            if (_camera == null) return;

            Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
            Ray ray = _camera.GetScreenRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                var brain = hit.collider.GetComponentInParent<UnitBrain>();
                if (brain != null && brain.IsInitialized)
                {
                    if (_combatController != null && _combatController.TryExecuteQueuedActionOnTarget(brain.Runtime))
                        return;

                    _selectionManager.SelectUnit(brain);
                    return;
                }

                // No unit hit — try left-click-to-move if a unit is selected
                if (_selectionManager.HasSelection && _gridMap != null)
                {
                    HexCoord targetCoord = _gridMap.WorldToHex(hit.point);

                    if (_combatController != null && _combatController.IsCombatActive)
                    {
                        // Combat: only move the active turn unit
                        var activeUnit = _combatController.CurrentUnit;
                        var selectedUnit = _selectionManager.SelectedUnit;

                        if (activeUnit != null && selectedUnit != null
                            && selectedUnit.UnitId == activeUnit.UnitId
                            && _combatController.ActionSystem.CanMove(activeUnit)
                            && _rangeVisualizer != null && _rangeVisualizer.IsReachable(targetCoord))
                        {
                            _movementSystem.HandleMoveCommand(targetCoord);
                            if (_movementSystem.IsUnitMoving &&
                                _combatController.QueuedAction == CombatSceneController.QueuedActionType.Move)
                            {
                                _combatController.ClearQueuedAction();
                            }
                            return;
                        }
                    }
                    else
                    {
                        // Non-combat: move if in reachable range
                        if (_rangeVisualizer != null && _rangeVisualizer.IsReachable(targetCoord))
                        {
                            _movementSystem.HandleMoveCommand(targetCoord);
                            return;
                        }
                    }
                }
            }

            if (_combatController != null && _combatController.HasQueuedAction)
            {
                _combatController.ClearQueuedAction();
                return;
            }

            _selectionManager.DeselectUnit();
        }

        private void HandleMoveCommand()
        {
            if (_camera == null || _gridMap == null) return;

            if (!_selectionManager.HasSelection)
            {
                Debug.Log("[TacticalInputHandler] MoveCommand: no unit selected.");
                return;
            }

            // Combat mode: only allow move during player turn for the active unit
            if (_combatController != null && _combatController.IsCombatActive)
            {
                if (!_combatController.IsPlayerTurn)
                {
                    Debug.Log("[TacticalInputHandler] MoveCommand: not player turn.");
                    return;
                }

                var activeUnit = _combatController.CurrentUnit;
                if (activeUnit == null)
                {
                    Debug.Log("[TacticalInputHandler] MoveCommand: no active unit.");
                    return;
                }

                var selectedUnit = _selectionManager.SelectedUnit;
                if (selectedUnit == null || selectedUnit.UnitId != activeUnit.UnitId)
                {
                    Debug.Log($"[TacticalInputHandler] MoveCommand: selected({selectedUnit?.UnitId}) != active({activeUnit.UnitId}). Select the active unit first.");
                    return;
                }

                if (!_combatController.ActionSystem.CanMove(activeUnit))
                {
                    Debug.Log("[TacticalInputHandler] Active unit has no move action remaining.");
                    return;
                }
            }

            Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
            Ray ray = _camera.GetScreenRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                HexCoord targetCoord = _gridMap.WorldToHex(hit.point);
                Debug.Log($"[TacticalInputHandler] MoveCommand: right-click at hex {targetCoord}");
                _movementSystem.HandleMoveCommand(targetCoord);

                if (_movementSystem.IsUnitMoving &&
                    _combatController != null &&
                    _combatController.QueuedAction == CombatSceneController.QueuedActionType.Move)
                {
                    _combatController.ClearQueuedAction();
                }
            }
            else
            {
                Debug.Log("[TacticalInputHandler] MoveCommand: raycast hit nothing.");
            }
        }

        private void HandleEndTurn()
        {
            if (_combatController == null || !_combatController.IsPlayerTurn) return;

            Debug.Log("[TacticalInputHandler] Player pressed End Turn.");
            _combatController.EndCurrentTurn();
        }

        private void HandleCycleUnit(int direction)
        {
            if (_selectionManager == null)
                return;

            if (!_selectionManager.SelectAdjacentUnit(TurnManager.PlayerTeamId, direction))
                return;

            FocusCameraOnSelectedUnit();
        }

        private void FocusCameraOnSelectedUnit()
        {
            if (_camera == null || !_selectionManager.HasSelection)
                return;

            int selectedUnitId = _selectionManager.SelectedUnit.UnitId;
            var brains = FindObjectsByType<UnitBrain>(FindObjectsSortMode.None);
            foreach (var brain in brains)
            {
                if (brain.UnitId != selectedUnitId)
                    continue;

                _camera.FocusOnPoint(brain.transform.position);
                _camera.SetFollowTarget(brain.transform);
                return;
            }
        }

        // ------------------------------------------------------------------
        // Event Handlers
        // ------------------------------------------------------------------

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (!evt.IsPlayerControlled) return;

            // Auto-select the active unit at the start of a player turn
            if (_combatController != null)
            {
                var unit = _combatController.CurrentUnit;
                if (unit != null)
                {
                    _selectionManager.SelectUnit(unit.UnitId);
                    FocusCameraOnSelectedUnit();
                    Debug.Log($"[TacticalInputHandler] Auto-selected active unit: {unit.Definition.UnitName}");
                }
            }
        }
    }
}
