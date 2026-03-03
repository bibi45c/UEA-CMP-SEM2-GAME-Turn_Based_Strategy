using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Manages action economy per unit turn.
    /// DOS2-style AP system: each unit has an Action Point pool per turn.
    /// Movement costs 1 AP per hex, abilities cost their defined ApCost.
    /// Pure C# class — no MonoBehaviour.
    /// </summary>
    public class ActionSystem
    {
        // --- Query ---

        /// <summary>Can this unit still move at least 1 hex?</summary>
        public bool CanMove(UnitRuntime unit) => unit != null && unit.CanMove;

        /// <summary>Can this unit still perform any action? (has at least 1 AP)</summary>
        public bool CanAct(UnitRuntime unit) => unit != null && unit.CanAct;

        /// <summary>Can this unit afford a specific ability?</summary>
        public bool CanUseAbility(UnitRuntime unit, AbilityDefinition ability)
        {
            if (unit == null || ability == null) return false;
            return unit.HasEnoughAP(ability.ApCost);
        }

        /// <summary>Has this unit exhausted all AP this turn?</summary>
        public bool IsTurnComplete(UnitRuntime unit)
        {
            if (unit == null) return true;
            return unit.CurrentAP <= 0;
        }

        /// <summary>Can this unit still do anything?</summary>
        public bool HasAnyAction(UnitRuntime unit)
        {
            if (unit == null) return false;
            return unit.CurrentAP > 0;
        }

        // --- Spend ---

        /// <summary>Spend AP for movement (1 AP per hex traversed).</summary>
        public void SpendMoveAP(UnitRuntime unit, int hexCount)
        {
            if (unit == null) return;
            int cost = Mathf.Max(1, hexCount);
            if (!unit.HasEnoughAP(cost))
            {
                Debug.LogWarning($"[ActionSystem] {unit.Definition.UnitName} does not have enough AP ({unit.CurrentAP}) to move {hexCount} hexes.");
                return;
            }
            unit.SpendAP(cost);
            Debug.Log($"[ActionSystem] {unit.Definition.UnitName} spent {cost} AP on movement. Remaining: {unit.CurrentAP}/{unit.MaxAP}");
        }

        /// <summary>Spend AP for an ability.</summary>
        public void SpendAbilityAP(UnitRuntime unit, AbilityDefinition ability)
        {
            if (unit == null || ability == null) return;
            int cost = ability.ApCost;
            if (!unit.HasEnoughAP(cost))
            {
                Debug.LogWarning($"[ActionSystem] {unit.Definition.UnitName} does not have enough AP ({unit.CurrentAP}) for {ability.AbilityName} (cost {cost}).");
                return;
            }
            unit.SpendAP(cost);
            Debug.Log($"[ActionSystem] {unit.Definition.UnitName} spent {cost} AP on {ability.AbilityName}. Remaining: {unit.CurrentAP}/{unit.MaxAP}");
        }

        // --- Reset ---

        /// <summary>Reset AP at the start of a unit's turn.</summary>
        public void ResetForNewTurn(UnitRuntime unit)
        {
            if (unit == null) return;
            unit.ResetAP();
        }
    }
}
