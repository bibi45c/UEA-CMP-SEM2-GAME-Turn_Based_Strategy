using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Result of a single damage calculation.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly int Damage;
        public readonly bool WasCritical;
        public readonly bool WasBlockedByCover;

        public DamageResult(int damage, bool wasCritical, bool wasBlockedByCover = false)
        {
            Damage = damage;
            WasCritical = wasCritical;
            WasBlockedByCover = wasBlockedByCover;
        }
    }

    /// <summary>
    /// Stateless combat math helper for damage formulas.
    /// Pure C# service - no scene dependencies.
    /// </summary>
    public class DamageResolver
    {
        /// <summary>
        /// Resolve damage for a data-driven ability effect with cover consideration.
        /// </summary>
        public DamageResult ResolveAbilityDamage(
            UnitRuntime caster, UnitRuntime target, EffectPayload effect,
            CoverType cover, bool isRanged)
        {
            if (caster == null || target == null)
                return new DamageResult(0, false);

            // Ranged attacks against FullCover are blocked entirely
            if (isRanged && cover == CoverType.FullCover)
            {
                Debug.Log($"[DamageResolver] Attack blocked by full cover!");
                return new DamageResult(0, false, wasBlockedByCover: true);
            }

            int statValue = AbilityExecutor.GetStatValue(caster, effect.ScalingStat);

            // For physical attacks, add Finesse as secondary contribution
            int rawDamage = effect.BaseValue + Mathf.RoundToInt(statValue * effect.ScalingFactor);
            if (effect.EffectType == AbilityEffectType.PhysicalDamage)
                rawDamage += Mathf.RoundToInt(caster.Stats.Finesse * 0.35f);

            // Mitigation
            int armor = effect.EffectType == AbilityEffectType.MagicDamage
                ? target.Stats.MagicResistance
                : target.Stats.PhysicalArmor;
            int mitigatedDamage = Mathf.Max(1, rawDamage - armor);

            // Cover reduction (HalfCover = 25% reduction, ranged only)
            if (isRanged && cover == CoverType.HalfCover)
                mitigatedDamage = Mathf.Max(1, Mathf.RoundToInt(mitigatedDamage * 0.75f));

            // Crit check
            bool wasCritical = Random.value <= caster.Stats.CritChance;
            if (wasCritical)
                mitigatedDamage = Mathf.Max(1, Mathf.CeilToInt(mitigatedDamage * 1.5f));

            return new DamageResult(mitigatedDamage, wasCritical);
        }

        /// <summary>
        /// Backwards-compatible overload (no cover).
        /// </summary>
        public DamageResult ResolveAbilityDamage(UnitRuntime caster, UnitRuntime target, EffectPayload effect)
        {
            return ResolveAbilityDamage(caster, target, effect, CoverType.None, false);
        }
    }
}
