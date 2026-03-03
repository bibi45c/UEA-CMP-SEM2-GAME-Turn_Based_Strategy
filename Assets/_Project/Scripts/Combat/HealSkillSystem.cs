using UnityEngine;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Simple ally heal skill for the Phase 1 player kit.
    /// </summary>
    public readonly struct HealResolution
    {
        public readonly bool Success;
        public readonly int HealAmount;
        public readonly string FailureReason;

        public HealResolution(bool success, int healAmount, string failureReason)
        {
            Success = success;
            HealAmount = healAmount;
            FailureReason = failureReason;
        }
    }

    public class HealSkillSystem
    {
        public const int DefaultRange = 2;
        public int Range => DefaultRange;

        public HealResolution Execute(UnitRuntime source, UnitRuntime target)
        {
            if (source == null || target == null)
                return new HealResolution(false, 0, "Source or target is missing.");

            if (source.IsDead || target.IsDead)
                return new HealResolution(false, 0, "Dead units cannot use or receive healing.");

            if (source.TeamId != target.TeamId)
                return new HealResolution(false, 0, "Heal only targets allies.");

            if (source.GridPosition.DistanceTo(target.GridPosition) > Range)
                return new HealResolution(false, 0, $"Target is out of range ({Range}).");

            int healPower = 4 + Mathf.RoundToInt(source.Stats.Intelligence * 0.5f);
            int previousHp = target.CurrentHP;
            target.Heal(healPower);
            int healedAmount = target.CurrentHP - previousHp;

            if (healedAmount <= 0)
                return new HealResolution(false, 0, $"{target.Definition.UnitName} is already at full health.");

            return new HealResolution(true, healedAmount, null);
        }
    }
}
