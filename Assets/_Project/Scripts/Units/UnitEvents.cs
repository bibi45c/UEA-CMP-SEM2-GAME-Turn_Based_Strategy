using System.Collections.Generic;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// All unit-related EventBus event structs.
    /// Structs to satisfy EventBus's where T : struct constraint.
    /// </summary>

    public struct UnitSpawnedEvent
    {
        public int UnitId;
        public HexCoord Position;
    }

    public struct UnitSelectedEvent
    {
        public int UnitId;
        public HexCoord Position;
    }

    public struct UnitDeselectedEvent
    {
        public int PreviousUnitId;
    }

    public struct UnitMoveStartedEvent
    {
        public int UnitId;
        public HexCoord From;
        public HexCoord To;
        public List<HexCoord> Path;
    }

    public struct UnitMoveCompletedEvent
    {
        public int UnitId;
        public HexCoord FinalPosition;
        public int HexesMoved;
    }

    public struct UnitDamagedEvent
    {
        public int AttackerUnitId;
        public int TargetUnitId;
        public int DamageAmount;
        public int RemainingHP;
        public bool WasCritical;
        public bool DidKill;
    }

    public struct UnitHealedEvent
    {
        public int SourceUnitId;
        public int TargetUnitId;
        public int HealAmount;
        public int CurrentHP;
    }

    public struct UnitDiedEvent
    {
        public int UnitId;
        public HexCoord Position;
    }

    public struct StatusAppliedEvent
    {
        public int TargetUnitId;
        public string StatusName;
        public int Duration;
    }

    public struct StatusExpiredEvent
    {
        public int TargetUnitId;
        public string StatusName;
    }
}
