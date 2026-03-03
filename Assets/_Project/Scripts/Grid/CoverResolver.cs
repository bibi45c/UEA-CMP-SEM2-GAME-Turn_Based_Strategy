using TurnBasedTactics.Abilities;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Static utility for directional cover checks between attacker and target.
    /// Draws hex line from attacker to target, checks intervening cells for cover.
    /// </summary>
    public static class CoverResolver
    {
        /// <summary>
        /// Determine the best cover protecting the target from an attack originating at the attacker's position.
        /// Walks the hex line between the two cells and returns the highest CoverType found.
        /// Height advantage (attacker above target) downgrades cover by one tier.
        /// </summary>
        public static CoverType GetCoverBetween(HexGridMap gridMap, HexCoord attacker, HexCoord target)
        {
            if (gridMap == null) return CoverType.None;
            if (attacker.Equals(target)) return CoverType.None;

            // Get hex line from attacker to target
            var line = attacker.LineTo(target);

            CoverType bestCover = CoverType.None;

            // Walk intervening cells (skip attacker at index 0 and target at last index)
            for (int i = 1; i < line.Count - 1; i++)
            {
                var cell = gridMap.GetCell(line[i]);
                if (cell == null) continue;

                if (cell.Cover > bestCover)
                    bestCover = cell.Cover;

                // Early out — can't get higher than FullCover
                if (bestCover == CoverType.FullCover)
                    break;
            }

            // Height bypass: high ground attacker shoots over cover
            if (bestCover != CoverType.None)
            {
                var attackerCell = gridMap.GetCell(attacker);
                var targetCell = gridMap.GetCell(target);
                if (attackerCell != null && targetCell != null &&
                    attackerCell.HeightLevel > targetCell.HeightLevel)
                {
                    // Downgrade cover by one tier
                    bestCover = bestCover switch
                    {
                        CoverType.FullCover => CoverType.HalfCover,
                        CoverType.HalfCover => CoverType.None,
                        _ => CoverType.None
                    };
                }
            }

            return bestCover;
        }

        /// <summary>
        /// Check line of sight from attacker to target.
        /// Walks the hex line between the two positions.
        /// FullCover on intervening cells blocks LoS unless attacker is above the blocking cell.
        /// Melee (range &lt;= 1) always has LoS.
        /// </summary>
        public static bool HasLineOfSight(HexGridMap gridMap, HexCoord attacker, HexCoord target, int abilityRange)
        {
            // Melee always has LoS
            if (abilityRange <= 1) return true;
            if (gridMap == null || attacker.Equals(target)) return true;

            var attackerCell = gridMap.GetCell(attacker);
            if (attackerCell == null) return true;

            var line = attacker.LineTo(target);

            // Walk intervening cells (skip attacker at index 0, target at last index)
            for (int i = 1; i < line.Count - 1; i++)
            {
                var cell = gridMap.GetCell(line[i]);
                if (cell == null) continue;

                // FullCover blocks LoS unless attacker is above the blocking cell
                if (cell.Cover == CoverType.FullCover && attackerCell.HeightLevel <= cell.HeightLevel)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the ability is ranged (range > 1).
        /// Melee attacks (range 1) ignore cover entirely.
        /// </summary>
        public static bool IsRangedAttack(AbilityDefinition ability)
        {
            return ability != null && ability.Range > 1;
        }

        /// <summary>
        /// Get the damage multiplier for a given cover type.
        /// HalfCover = 0.75 (25% reduction), FullCover = 0 (blocked), None = 1 (full damage).
        /// </summary>
        public static float GetDamageMultiplier(CoverType cover)
        {
            return cover switch
            {
                CoverType.HalfCover => 0.75f,
                CoverType.FullCover => 0f,
                _ => 1f
            };
        }
    }
}
