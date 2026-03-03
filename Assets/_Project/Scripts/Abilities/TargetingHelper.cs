using System.Collections.Generic;
using TurnBasedTactics.Units;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Static utility for ability target validation and collection.
    /// </summary>
    public static class TargetingHelper
    {
        /// <summary>
        /// Check whether a primary target is valid for the given ability and caster.
        /// Does NOT check range — that is done separately.
        /// </summary>
        public static bool IsValidTarget(AbilityDefinition ability, UnitRuntime caster, UnitRuntime target)
        {
            if (ability == null || caster == null || target == null)
                return false;

            if (target.IsDead)
                return false;

            switch (ability.TargetingType)
            {
                case TargetingType.SingleEnemy:
                    return target.TeamId != caster.TeamId;

                case TargetingType.SingleAlly:
                    return target.TeamId == caster.TeamId;

                case TargetingType.Self:
                    return target.UnitId == caster.UnitId;

                case TargetingType.CircleAOE:
                    // AOE primary target can be anyone (or even empty ground).
                    // For unit-targeted AOE, accept any living unit.
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Check whether the primary target is within ability range of the caster.
        /// </summary>
        public static bool IsInRange(AbilityDefinition ability, UnitRuntime caster, UnitRuntime target)
        {
            if (ability == null || caster == null || target == null)
                return false;

            if (ability.TargetingType == TargetingType.Self)
                return true;

            return caster.GridPosition.DistanceTo(target.GridPosition) <= ability.Range;
        }

        /// <summary>
        /// Collect all units that will be affected by this ability.
        /// For single-target abilities, returns just the target.
        /// For AOE, returns all valid units within the radius.
        /// </summary>
        public static List<UnitRuntime> CollectTargets(
            AbilityDefinition ability,
            UnitRuntime caster,
            UnitRuntime primaryTarget,
            UnitRegistry registry)
        {
            var targets = new List<UnitRuntime>();

            switch (ability.TargetingType)
            {
                case TargetingType.SingleEnemy:
                case TargetingType.SingleAlly:
                    targets.Add(primaryTarget);
                    break;

                case TargetingType.Self:
                    targets.Add(caster);
                    break;

                case TargetingType.CircleAOE:
                    HexCoord center = primaryTarget.GridPosition;
                    int radius = ability.AoeRadius;
                    foreach (var unit in registry.AllUnits)
                    {
                        if (unit.IsDead) continue;
                        if (unit.GridPosition.DistanceTo(center) <= radius)
                            targets.Add(unit);
                    }
                    break;
            }

            return targets;
        }
    }
}
