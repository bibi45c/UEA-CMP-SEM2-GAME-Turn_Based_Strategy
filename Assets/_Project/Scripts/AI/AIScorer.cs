using System.Collections.Generic;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.AI
{
    /// <summary>
    /// Stateless scoring helper for AI decision-making.
    /// Evaluates targets and positions to produce simple numeric scores.
    /// Pure C# — no MonoBehaviour, no scene dependencies.
    /// </summary>
    public static class AIScorer
    {
        /// <summary>
        /// Score a potential attack target. Higher = more desirable.
        /// Factors: low HP targets are prioritized, closer targets preferred.
        /// </summary>
        public static float ScoreTarget(UnitRuntime attacker, UnitRuntime target)
        {
            if (target == null || target.IsDead)
                return float.MinValue;

            float score = 0f;

            // Prefer low-HP targets (potential kills are high value)
            float hpRatio = (float)target.CurrentHP / target.Stats.MaxHP;
            score += (1f - hpRatio) * 50f; // 0~50 points for low HP

            // Prefer closer targets (less movement wasted)
            int distance = attacker.GridPosition.DistanceTo(target.GridPosition);
            score -= distance * 5f;

            // Bonus if target can be killed this turn (estimate)
            int estimatedDamage = attacker.Stats.Strength +
                                  (int)(attacker.Stats.Finesse * 0.35f) -
                                  target.Stats.PhysicalArmor;
            if (estimatedDamage >= target.CurrentHP)
                score += 80f; // Big bonus for potential kills

            return score;
        }

        /// <summary>
        /// Pick the best target from a list of enemies.
        /// Returns null if no valid target exists.
        /// </summary>
        public static UnitRuntime PickBestTarget(UnitRuntime attacker, List<UnitRuntime> enemies)
        {
            UnitRuntime best = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;

                float score = ScoreTarget(attacker, enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        /// <summary>
        /// Find the best cell to move to in order to attack a target.
        /// Prefers cells adjacent to the target (melee range).
        /// Returns the attacker's current position if already in range or no valid move found.
        /// </summary>
        public static HexCoord FindBestMoveToward(
            UnitRuntime attacker,
            UnitRuntime target,
            int attackRange,
            HexGridMap gridMap,
            Dictionary<HexCoord, float> reachableCells)
        {
            int currentDist = attacker.GridPosition.DistanceTo(target.GridPosition);

            // Already in attack range — don't move
            if (currentDist <= attackRange)
                return attacker.GridPosition;

            HexCoord bestCell = attacker.GridPosition;
            int bestDist = currentDist;

            foreach (var kvp in reachableCells)
            {
                HexCoord cell = kvp.Key;

                // Skip occupied cells (except our own position)
                if (cell != attacker.GridPosition)
                {
                    if (!gridMap.TryGetCell(cell, out HexCell hexCell)) continue;
                    if (hexCell.IsOccupied) continue;
                }

                int dist = cell.DistanceTo(target.GridPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCell = cell;
                }
            }

            return bestCell;
        }
    }
}
