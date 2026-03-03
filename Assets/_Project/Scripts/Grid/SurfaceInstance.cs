namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Runtime state for one active surface on a hex cell.
    /// Created when a surface is placed, destroyed when it expires or is transformed.
    /// </summary>
    public class SurfaceInstance
    {
        public SurfaceDefinition Definition { get; }
        public HexCoord CellCoord { get; }
        public int SourceUnitId { get; }
        public int RemainingRounds { get; private set; }
        public bool IsExpired => Definition.DefaultDuration > 0 && RemainingRounds <= 0;

        public SurfaceInstance(SurfaceDefinition definition, HexCoord cellCoord, int sourceUnitId)
        {
            Definition = definition;
            CellCoord = cellCoord;
            SourceUnitId = sourceUnitId;
            RemainingRounds = definition.DefaultDuration;
        }

        /// <summary>
        /// Decrement the remaining duration by one round.
        /// Returns true if the surface has expired after this tick.
        /// </summary>
        public bool TickDuration()
        {
            if (Definition.DefaultDuration <= 0)
                return false; // permanent

            RemainingRounds--;
            return RemainingRounds <= 0;
        }
    }
}
