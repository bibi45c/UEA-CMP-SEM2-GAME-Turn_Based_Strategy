using UnityEngine;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Mutable runtime state for a living unit in combat.
    /// One instance per unit on the battlefield.
    /// Plain C# class — no MonoBehaviour.
    /// </summary>
    public class UnitRuntime
    {
        // --- Identity ---
        public int UnitId { get; }
        public UnitDefinition Definition { get; }
        public UnitStats Stats { get; }
        public int TeamId { get; }

        // --- Grid State ---
        public HexCoord GridPosition { get; private set; }

        // --- Combat State ---
        public int CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0;

        // --- Turn Action Tracking (AP System) ---
        public int MaxAP { get; private set; }
        public int CurrentAP { get; private set; }
        public bool CanMove => !IsDead && CurrentAP >= 1;
        public bool CanAct => !IsDead && CurrentAP >= 1;

        public UnitRuntime(int unitId, UnitDefinition definition, int teamId, HexCoord startPosition)
        {
            UnitId = unitId;
            Definition = definition;
            Stats = new UnitStats(definition);
            TeamId = teamId;
            GridPosition = startPosition;
            CurrentHP = Stats.MaxHP;
            MaxAP = Stats.ActionPoints;
            CurrentAP = MaxAP;
        }

        // --- Grid Position ---

        public void SetGridPosition(HexCoord newPosition)
        {
            GridPosition = newPosition;
        }

        // --- Health ---

        public void TakeDamage(int amount)
        {
            amount = Mathf.Max(0, amount);
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
        }

        public void Heal(int amount)
        {
            amount = Mathf.Max(0, amount);
            CurrentHP = Mathf.Min(Stats.MaxHP, CurrentHP + amount);
        }

        // --- Action Points ---

        /// <summary>Does this unit have enough AP for a given cost?</summary>
        public bool HasEnoughAP(int cost) => CurrentAP >= cost;

        /// <summary>Spend AP. Clamps to 0.</summary>
        public void SpendAP(int cost)
        {
            cost = Mathf.Max(0, cost);
            CurrentAP = Mathf.Max(0, CurrentAP - cost);
        }

        /// <summary>Reset AP to max at the start of a new turn.</summary>
        public void ResetAP()
        {
            MaxAP = Stats.ActionPoints;
            CurrentAP = MaxAP;
        }

        // --- Backwards-compatible wrappers (called by TurnManager) ---

        public void ResetTurnActions()
        {
            ResetAP();
        }
    }
}
