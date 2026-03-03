namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Unified result of executing any ability.
    /// Returned by AbilityExecutor, consumed by CombatSceneController and AIBrain.
    /// </summary>
    public readonly struct AbilityResult
    {
        public readonly bool Success;
        public readonly string FailureReason;
        public readonly int TotalDamage;
        public readonly int TotalHealing;
        public readonly bool WasCritical;
        public readonly bool DidKill;
        public readonly int StatusesApplied;
        public readonly bool WasBlockedByCover;

        public AbilityResult(bool success, string failureReason,
            int totalDamage = 0, int totalHealing = 0,
            bool wasCritical = false, bool didKill = false,
            int statusesApplied = 0, bool wasBlockedByCover = false)
        {
            Success = success;
            FailureReason = failureReason;
            TotalDamage = totalDamage;
            TotalHealing = totalHealing;
            WasCritical = wasCritical;
            DidKill = didKill;
            StatusesApplied = statusesApplied;
            WasBlockedByCover = wasBlockedByCover;
        }

        public static AbilityResult Fail(string reason) =>
            new AbilityResult(false, reason);
    }
}
