using System.Collections.Generic;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Central registry of all active UnitRuntime instances.
    /// Provides lookup by ID, position, and team.
    /// Plain C# class — no MonoBehaviour, no Find() calls.
    /// </summary>
    public class UnitRegistry
    {
        private readonly Dictionary<int, UnitRuntime> _units = new Dictionary<int, UnitRuntime>();
        private int _nextId;

        public int Count => _units.Count;
        public IReadOnlyCollection<UnitRuntime> AllUnits => _units.Values;

        public int GenerateId()
        {
            return _nextId++;
        }

        public void Register(UnitRuntime unit)
        {
            _units[unit.UnitId] = unit;
        }

        public void Unregister(int unitId)
        {
            _units.Remove(unitId);
        }

        public UnitRuntime GetUnit(int unitId)
        {
            _units.TryGetValue(unitId, out UnitRuntime unit);
            return unit;
        }

        public bool TryGetUnit(int unitId, out UnitRuntime unit)
        {
            return _units.TryGetValue(unitId, out unit);
        }

        /// <summary>
        /// Find a unit at the given grid position. Linear scan — OK for small unit counts.
        /// </summary>
        public UnitRuntime GetUnitAtPosition(HexCoord coord)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.GridPosition == coord && !unit.IsDead)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// Get all living units on a given team.
        /// </summary>
        public List<UnitRuntime> GetTeamUnits(int teamId)
        {
            var result = new List<UnitRuntime>();
            foreach (var unit in _units.Values)
            {
                if (unit.TeamId == teamId && !unit.IsDead)
                    result.Add(unit);
            }
            return result;
        }

        public void Clear()
        {
            _units.Clear();
            _nextId = 0;
        }
    }
}
