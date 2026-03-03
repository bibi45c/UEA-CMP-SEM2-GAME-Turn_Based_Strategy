namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Runtime state of a single active status effect on a unit.
    /// Created when a status is applied, destroyed when it expires or is dispelled.
    /// </summary>
    public class StatusInstance
    {
        public StatusDefinition Definition { get; }
        public int SourceUnitId { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => Definition.Duration > 0 && RemainingTurns <= 0;

        public StatusInstance(StatusDefinition definition, int sourceUnitId)
        {
            Definition = definition;
            SourceUnitId = sourceUnitId;
            RemainingTurns = definition.Duration;
        }

        /// <summary>
        /// Decrement the remaining duration by one turn.
        /// Returns true if the status has expired after this tick.
        /// </summary>
        public bool TickDuration()
        {
            if (Definition.Duration <= 0)
                return false; // permanent

            RemainingTurns--;
            return RemainingTurns <= 0;
        }
    }
}
