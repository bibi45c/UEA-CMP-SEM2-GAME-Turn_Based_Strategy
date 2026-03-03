using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Stateless service that executes any AbilityDefinition.
    /// Pipeline: validate -> collect targets -> apply effects -> return result.
    /// </summary>
    public class AbilityExecutor
    {
        private readonly DamageResolver _damageResolver = new DamageResolver();
        private StatusManager _statusManager;
        private SurfaceSystem _surfaceSystem;

        /// <summary>
        /// Inject the StatusManager so ApplyStatus effects can be processed.
        /// </summary>
        public void SetStatusManager(StatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        /// <summary>
        /// Inject the SurfaceSystem so CreateSurface effects can be processed.
        /// </summary>
        public void SetSurfaceSystem(SurfaceSystem surfaceSystem)
        {
            _surfaceSystem = surfaceSystem;
        }

        /// <summary>
        /// Execute an ability from caster on a primary target.
        /// Returns an AbilityResult. Caller is responsible for publishing events.
        /// </summary>
        public AbilityResult Execute(
            AbilityDefinition ability,
            UnitRuntime caster,
            UnitRuntime target,
            HexGridMap gridMap,
            UnitRegistry registry)
        {
            // --- Validation ---
            if (ability == null)
                return AbilityResult.Fail("No ability specified.");
            if (caster == null || caster.IsDead)
                return AbilityResult.Fail("Caster is null or dead.");
            if (target == null || target.IsDead)
                return AbilityResult.Fail("Target is null or dead.");

            if (!TargetingHelper.IsValidTarget(ability, caster, target))
                return AbilityResult.Fail($"Invalid target for {ability.AbilityName}.");

            if (!TargetingHelper.IsInRange(ability, caster, target))
                return AbilityResult.Fail($"Target out of range ({ability.Range}).");

            // --- LoS Check (ranged abilities blocked by FullCover obstacles) ---
            if (!CoverResolver.HasLineOfSight(gridMap, caster.GridPosition, target.GridPosition, ability.Range))
                return AbilityResult.Fail("No line of sight to target.");

            // --- Collect targets ---
            List<UnitRuntime> targets = TargetingHelper.CollectTargets(
                ability, caster, target, registry);

            if (targets.Count == 0)
                return AbilityResult.Fail("No valid targets found.");

            // --- Pre-compute cover for damage effects ---
            bool isRanged = CoverResolver.IsRangedAttack(ability);

            // --- Apply effects ---
            int totalDamage = 0;
            int totalHealing = 0;
            bool anyCrit = false;
            bool anyKill = false;
            bool anyBlocked = false;
            int statusesApplied = 0;

            foreach (var t in targets)
            {
                if (t.IsDead) continue;

                // Compute cover between caster and this target
                CoverType cover = CoverResolver.GetCoverBetween(gridMap, caster.GridPosition, t.GridPosition);

                foreach (var effect in ability.Effects)
                {
                    switch (effect.EffectType)
                    {
                        case AbilityEffectType.PhysicalDamage:
                        case AbilityEffectType.MagicDamage:
                            var dmg = _damageResolver.ResolveAbilityDamage(caster, t, effect, cover, isRanged);
                            if (dmg.WasBlockedByCover)
                            {
                                anyBlocked = true;
                                break;
                            }
                            t.TakeDamage(dmg.Damage);
                            totalDamage += dmg.Damage;
                            if (dmg.WasCritical) anyCrit = true;
                            if (t.IsDead) anyKill = true;
                            break;

                        case AbilityEffectType.Heal:
                            int healPower = CalculateHealPower(caster, effect);
                            int prevHp = t.CurrentHP;
                            t.Heal(healPower);
                            totalHealing += t.CurrentHP - prevHp;
                            break;

                        case AbilityEffectType.ApplyStatus:
                            if (effect.StatusToApply != null && _statusManager != null)
                            {
                                _statusManager.ApplyStatus(effect.StatusToApply, t, caster.UnitId);
                                statusesApplied++;
                            }
                            else if (effect.StatusToApply == null)
                            {
                                Debug.LogWarning($"[AbilityExecutor] ApplyStatus effect has no StatusDefinition assigned.");
                            }
                            break;

                        case AbilityEffectType.CreateSurface:
                            if (effect.SurfaceToCreate != null && _surfaceSystem != null)
                            {
                                _surfaceSystem.CreateSurface(t.GridPosition, effect.SurfaceToCreate, caster.UnitId, gridMap);
                            }
                            else if (effect.SurfaceToCreate == null)
                            {
                                Debug.LogWarning($"[AbilityExecutor] CreateSurface effect has no SurfaceDefinition assigned.");
                            }
                            break;
                    }
                }
            }

            return new AbilityResult(
                true, null,
                totalDamage, totalHealing,
                anyCrit, anyKill,
                statusesApplied, anyBlocked);
        }

        private int CalculateHealPower(UnitRuntime caster, EffectPayload effect)
        {
            int statValue = GetStatValue(caster, effect.ScalingStat);
            return Mathf.Max(1, effect.BaseValue + Mathf.RoundToInt(statValue * effect.ScalingFactor));
        }

        public static int GetStatValue(UnitRuntime unit, ScalingStat stat)
        {
            if (unit == null) return 0;

            return stat switch
            {
                ScalingStat.Strength => unit.Stats.Strength,
                ScalingStat.Finesse => unit.Stats.Finesse,
                ScalingStat.Intelligence => unit.Stats.Intelligence,
                ScalingStat.Constitution => unit.Stats.Constitution,
                ScalingStat.Wits => unit.Stats.Wits,
                _ => 0
            };
        }
    }
}
