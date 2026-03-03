using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Core;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Handles click-to-select logic: raycast against unit colliders,
    /// manage selected state, and publish selection events via EventBus.
    /// </summary>
    public class UnitSelectionManager : MonoBehaviour
    {
        private UnitRegistry _registry;
        private UnitRuntime _selectedUnit;
        private UnitVisual _selectedVisual;

        public UnitRuntime SelectedUnit => _selectedUnit;
        public bool HasSelection => _selectedUnit != null;

        public void Initialize(UnitRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Called by TacticalInputHandler when left-click occurs.
        /// Raycast to find unit or deselect.
        /// </summary>
        public void HandleLeftClick(Ray ray)
        {
            // Raycast against all colliders
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                var brain = hit.collider.GetComponentInParent<UnitBrain>();
                if (brain != null && brain.IsInitialized)
                {
                    SelectUnit(brain);
                    return;
                }
            }

            // Clicked empty space — deselect
            DeselectUnit();
        }

        /// <summary>
        /// Programmatic selection by unit ID. Finds the UnitBrain in scene.
        /// Used by combat system for auto-selecting the active turn unit.
        /// </summary>
        public void SelectUnit(int unitId)
        {
            var brains = FindObjectsByType<UnitBrain>(FindObjectsSortMode.None);
            foreach (var b in brains)
            {
                if (b.UnitId == unitId)
                {
                    SelectUnit(b);
                    return;
                }
            }
            Debug.LogWarning($"[Selection] Cannot find UnitBrain for unitId={unitId}");
        }

        public void SelectUnit(UnitBrain brain)
        {
            if (brain == null || !brain.IsInitialized) return;

            // If same unit already selected, do nothing
            if (_selectedUnit != null && _selectedUnit.UnitId == brain.UnitId)
                return;

            // Deselect previous
            DeselectUnit();

            // Select new
            _selectedUnit = brain.Runtime;
            _selectedVisual = brain.GetComponent<UnitVisual>();
            if (_selectedVisual != null)
                _selectedVisual.SetSelected(true);

            EventBus.Publish(new UnitSelectedEvent
            {
                UnitId = _selectedUnit.UnitId,
                Position = _selectedUnit.GridPosition
            });

            Debug.Log($"[Selection] Selected: {_selectedUnit.Definition.UnitName} (ID:{_selectedUnit.UnitId})");
        }

        public bool SelectAdjacentUnit(int teamId, int direction)
        {
            if (_registry == null)
                return false;

            List<UnitRuntime> units = _registry.GetTeamUnits(teamId);
            if (units.Count == 0)
                return false;

            units.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));

            int step = direction >= 0 ? 1 : -1;
            int currentIndex = -1;
            if (_selectedUnit != null)
            {
                currentIndex = units.FindIndex(unit => unit.UnitId == _selectedUnit.UnitId);
            }

            int nextIndex = currentIndex < 0
                ? (step > 0 ? 0 : units.Count - 1)
                : (currentIndex + step + units.Count) % units.Count;

            SelectUnit(units[nextIndex].UnitId);
            return true;
        }

        /// <summary>
        /// Force re-publish UnitSelectedEvent for the current selection.
        /// Used to refresh MovementRangeVisualizer when Move is queued.
        /// </summary>
        public void RefreshSelection()
        {
            if (_selectedUnit == null) return;

            EventBus.Publish(new UnitSelectedEvent
            {
                UnitId = _selectedUnit.UnitId,
                Position = _selectedUnit.GridPosition
            });
        }

        public void DeselectUnit()
        {
            if (_selectedUnit == null) return;

            int prevId = _selectedUnit.UnitId;

            if (_selectedVisual != null)
                _selectedVisual.SetSelected(false);

            _selectedUnit = null;
            _selectedVisual = null;

            EventBus.Publish(new UnitDeselectedEvent { PreviousUnitId = prevId });
        }
    }
}
