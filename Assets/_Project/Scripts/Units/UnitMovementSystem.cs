using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Core;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Orchestrates unit movement: validates commands, runs pathfinding,
    /// updates grid state, and delegates visual animation to UnitVisual.
    /// Grid state is updated BEFORE visual animation starts (prevents race conditions).
    /// </summary>
    public class UnitMovementSystem : MonoBehaviour
    {
        private HexGridMap _gridMap;
        private UnitRegistry _registry;
        private UnitSelectionManager _selectionManager;
        private MovementRangeVisualizer _rangeVisualizer;
        private UnitSpawner _spawner;
        private bool _isUnitMoving;

        private readonly HexPathfinder.PathConfig _pathConfig = HexPathfinder.PathConfig.Default;

        public bool IsUnitMoving => _isUnitMoving;

        public void Initialize(
            HexGridMap gridMap,
            UnitRegistry registry,
            UnitSelectionManager selectionManager,
            MovementRangeVisualizer rangeVisualizer,
            UnitSpawner spawner)
        {
            _gridMap = gridMap;
            _registry = registry;
            _selectionManager = selectionManager;
            _rangeVisualizer = rangeVisualizer;
            _spawner = spawner;
        }

        /// <summary>
        /// Called by TacticalInputHandler when right-click on ground.
        /// </summary>
        public void HandleMoveCommand(HexCoord targetCoord)
        {
            // Guards
            if (_isUnitMoving) return;
            if (!_selectionManager.HasSelection) return;

            var unit = _selectionManager.SelectedUnit;
            if (!unit.CanMove) return;
            if (unit.TeamId != 0) return; // Only player units

            // Check target cell exists and is walkable
            if (!_gridMap.TryGetCell(targetCoord, out HexCell targetCell)) return;
            if (!targetCell.Walkable) return;
            if (targetCell.IsOccupied) return;

            // Check target is in reachable range
            if (!_rangeVisualizer.IsReachable(targetCoord)) return;

            // Find path
            var path = HexPathfinder.FindPath(
                _gridMap, unit.GridPosition, targetCoord, _pathConfig);

            if (path == null || path.Count < 2) return;

            ExecuteMovement(unit, path);
        }

        private void ExecuteMovement(UnitRuntime unit, List<HexCoord> path)
        {
            _isUnitMoving = true;

            HexCoord from = unit.GridPosition;
            HexCoord to = path[path.Count - 1];

            // 1. Update grid state immediately (authoritative)
            _gridMap.SetOccupant(from, -1);
            _gridMap.SetOccupant(to, unit.UnitId);

            // 2. Update runtime state
            unit.SetGridPosition(to);

            // 3. Hide movement range overlay
            _rangeVisualizer.ClearHighlights();

            // 4. Publish move started event
            EventBus.Publish(new UnitMoveStartedEvent
            {
                UnitId = unit.UnitId,
                From = from,
                To = to,
                Path = path
            });

            // 5. Convert hex path to world positions
            var worldPath = new List<Vector3>(path.Count);
            foreach (var coord in path)
            {
                worldPath.Add(_gridMap.GetCellWorldPosition(coord));
            }

            // 6. Animate visual movement
            UnitBrain brain = _spawner.GetBrain(unit.UnitId);
            if (brain == null)
            {
                Debug.LogError($"[UnitMovementSystem] No brain found for unit {unit.UnitId}!");
                OnMovementComplete(unit);
                return;
            }

            var visual = brain.GetComponent<UnitVisual>();
            if (visual != null)
            {
                visual.StartPathMovement(worldPath, () => OnMovementComplete(unit));
            }
            else
            {
                // Instant teleport fallback
                brain.transform.position = _gridMap.GetCellWorldPosition(to);
                OnMovementComplete(unit);
            }

            Debug.Log($"[Movement] {unit.Definition.UnitName} moving from {from} to {to} ({path.Count} steps)");
        }

        private void OnMovementComplete(UnitRuntime unit)
        {
            _isUnitMoving = false;

            EventBus.Publish(new UnitMoveCompletedEvent
            {
                UnitId = unit.UnitId,
                FinalPosition = unit.GridPosition
            });

            Debug.Log($"[Movement] {unit.Definition.UnitName} arrived at {unit.GridPosition}");
        }
    }
}
