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

        // --- Turn Action Tracking ---
        public bool HasMovedThisTurn { get; private set; }
        public bool HasActedThisTurn { get; private set; }
        public bool CanMove => !IsDead && !HasMovedThisTurn;
        public bool CanAct => !IsDead && !HasActedThisTurn;

        public UnitRuntime(int unitId, UnitDefinition definition, int teamId, HexCoord startPosition)
        {
            UnitId = unitId;
            Definition = definition;
            Stats = new UnitStats(definition);
            TeamId = teamId;
            GridPosition = startPosition;
            CurrentHP = Stats.MaxHP;
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

        // --- Turn Actions ---

        public void SpendMoveAction()
        {
            HasMovedThisTurn = true;
        }

        public void SpendMainAction()
        {
            HasActedThisTurn = true;
        }

        public void ResetTurnActions()
        {
            HasMovedThisTurn = false;
            HasActedThisTurn = false;
        }
    }
}
