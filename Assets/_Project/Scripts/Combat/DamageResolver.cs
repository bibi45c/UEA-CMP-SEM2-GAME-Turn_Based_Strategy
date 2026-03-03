using UnityEngine;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Stateless combat math helper for damage formulas.
    /// Pure C# service - no scene dependencies.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly int Damage;
        public readonly bool WasCritical;

        public DamageResult(int damage, bool wasCritical)
        {
            Damage = damage;
            WasCritical = wasCritical;
        }
    }

    public class DamageResolver
    {
        public DamageResult ResolveBasicAttack(UnitRuntime attacker, UnitRuntime target)
        {
            if (attacker == null || target == null)
                return new DamageResult(0, false);

            int rawDamage = attacker.Stats.Strength + Mathf.RoundToInt(attacker.Stats.Finesse * 0.35f);
            int mitigatedDamage = Mathf.Max(1, rawDamage - target.Stats.PhysicalArmor);

            bool wasCritical = Random.value <= attacker.Stats.CritChance;
            if (wasCritical)
                mitigatedDamage = Mathf.Max(1, Mathf.CeilToInt(mitigatedDamage * 1.5f));

            return new DamageResult(mitigatedDamage, wasCritical);
        }
    }
}
