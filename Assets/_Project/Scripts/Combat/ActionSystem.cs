using UnityEngine;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Manages action economy per unit turn.
    /// Phase 1: Each unit gets 1 Move Action + 1 Main Action per turn.
    /// Designed to be extensible for bonus actions, multi-AP, etc.
    /// Pure C# class — no MonoBehaviour.
    /// </summary>
    public class ActionSystem
    {
        // --- Query ---

        /// <summary>Can this unit still move this turn?</summary>
        public bool CanMove(UnitRuntime unit) => unit != null && unit.CanMove;

        /// <summary>Can this unit still perform a main action this turn?</summary>
        public bool CanAct(UnitRuntime unit) => unit != null && unit.CanAct;

        /// <summary>Has this unit exhausted all actions this turn?</summary>
        public bool IsTurnComplete(UnitRuntime unit)
        {
            if (unit == null) return true;
            return unit.HasMovedThisTurn && unit.HasActedThisTurn;
        }

        /// <summary>Can this unit still do anything?</summary>
        public bool HasAnyAction(UnitRuntime unit)
        {
            if (unit == null) return false;
            return unit.CanMove || unit.CanAct;
        }

        // --- Spend ---

        /// <summary>Consume the unit's move action for this turn.</summary>
        public void SpendMoveAction(UnitRuntime unit)
        {
            if (unit == null) return;
            if (!unit.CanMove)
            {
                Debug.LogWarning($"[ActionSystem] {unit.Definition.UnitName} has already moved this turn.");
                return;
            }
            unit.SpendMoveAction();
            Debug.Log($"[ActionSystem] {unit.Definition.UnitName} spent move action.");
        }

        /// <summary>Consume the unit's main action for this turn.</summary>
        public void SpendMainAction(UnitRuntime unit)
        {
            if (unit == null) return;
            if (!unit.CanAct)
            {
                Debug.LogWarning($"[ActionSystem] {unit.Definition.UnitName} has already acted this turn.");
                return;
            }
            unit.SpendMainAction();
            Debug.Log($"[ActionSystem] {unit.Definition.UnitName} spent main action.");
        }

        // --- Reset ---

        /// <summary>Reset actions at the start of a unit's turn.</summary>
        public void ResetForNewTurn(UnitRuntime unit)
        {
            if (unit == null) return;
            unit.ResetTurnActions();
        }
    }
}
