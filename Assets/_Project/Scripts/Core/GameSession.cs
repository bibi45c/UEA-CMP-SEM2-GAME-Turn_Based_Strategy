using System.Collections.Generic;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// Session-level state that persists across scene transitions.
    /// Holds party data, inventory snapshot, and encounter results.
    /// Created once by GameBootstrap; survives scene loads.
    /// </summary>
    public class GameSession
    {
        public static GameSession Current { get; private set; }

        /// <summary>Whether a combat encounter is currently active.</summary>
        public bool IsInCombat { get; set; }

        /// <summary>Identifier of the current encounter (set before loading combat scene).</summary>
        public string CurrentEncounterId { get; set; }

        /// <summary>Result of the last completed combat (win/lose/flee).</summary>
        public CombatOutcome LastCombatOutcome { get; set; }

        /// <summary>Generic key-value store for lightweight cross-scene flags.</summary>
        private readonly Dictionary<string, object> _flags = new();

        /// <summary>Initialize a new session or replace the current one.</summary>
        public static GameSession Create()
        {
            Current = new GameSession();
            return Current;
        }

        public void SetFlag(string key, object value) => _flags[key] = value;

        public T GetFlag<T>(string key, T defaultValue = default)
        {
            return _flags.TryGetValue(key, out var val) && val is T typed
                ? typed
                : defaultValue;
        }

        public bool HasFlag(string key) => _flags.ContainsKey(key);

        public void ClearFlags() => _flags.Clear();
    }

    /// <summary>Outcome of a combat encounter.</summary>
    public enum CombatOutcome
    {
        None,
        Victory,
        Defeat,
        Fled
    }
}
