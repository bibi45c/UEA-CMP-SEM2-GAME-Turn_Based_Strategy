namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// All combat-related EventBus event structs.
    /// Published by TurnManager and CombatSceneController.
    /// </summary>

    /// <summary>Combat encounter has begun. All units are spawned and ready.</summary>
    public struct CombatStartedEvent { }

    /// <summary>A new round of turns is starting.</summary>
    public struct RoundStartedEvent
    {
        public int RoundNumber;
    }

    /// <summary>A unit's turn has begun — they may now act.</summary>
    public struct TurnStartedEvent
    {
        public int UnitId;
        public bool IsPlayerControlled;
    }

    /// <summary>A unit's turn has ended — their actions are spent or skipped.</summary>
    public struct TurnEndedEvent
    {
        public int UnitId;
    }

    /// <summary>All units in the current round have acted.</summary>
    public struct RoundEndedEvent
    {
        public int RoundNumber;
    }

    /// <summary>Combat is over. One side has won.</summary>
    public struct CombatEndedEvent
    {
        public int WinningTeamId;
    }

    /// <summary>
    /// The active (currently acting) unit has changed.
    /// UI and Camera systems subscribe to this for focus updates.
    /// </summary>
    public struct ActiveUnitChangedEvent
    {
        public int UnitId;
        public int TeamId;
    }
}
