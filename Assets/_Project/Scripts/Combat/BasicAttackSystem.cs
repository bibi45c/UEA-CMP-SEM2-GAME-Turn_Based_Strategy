using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Minimal melee attack service for Phase 1 combat.
    /// Owns validation and damage application, but not scene cleanup.
    /// </summary>
    public readonly struct AttackResolution
    {
        public readonly bool Success;
        public readonly int DamageDealt;
        public readonly bool WasCritical;
        public readonly bool DidKill;
        public readonly string FailureReason;

        public AttackResolution(bool success, int damageDealt, bool wasCritical, bool didKill, string failureReason)
        {
            Success = success;
            DamageDealt = damageDealt;
            WasCritical = wasCritical;
            DidKill = didKill;
            FailureReason = failureReason;
        }
    }

    public class BasicAttackSystem
    {
        private readonly DamageResolver _damageResolver = new DamageResolver();

        public const int DefaultRange = 1;
        public int Range => DefaultRange;

        public AttackResolution Execute(UnitRuntime attacker, UnitRuntime target)
        {
            if (attacker == null || target == null)
                return new AttackResolution(false, 0, false, false, "Attacker or target is missing.");

            if (attacker.IsDead || target.IsDead)
                return new AttackResolution(false, 0, false, false, "Dead units cannot participate in attacks.");

            if (attacker.TeamId == target.TeamId)
                return new AttackResolution(false, 0, false, false, "Basic attack only targets enemies.");

            if (attacker.GridPosition.DistanceTo(target.GridPosition) > Range)
                return new AttackResolution(false, 0, false, false, $"Target is out of range ({Range}).");

            var damage = _damageResolver.ResolveBasicAttack(attacker, target);
            target.TakeDamage(damage.Damage);

            return new AttackResolution(
                true,
                damage.Damage,
                damage.WasCritical,
                target.IsDead,
                null);
        }
    }
}
