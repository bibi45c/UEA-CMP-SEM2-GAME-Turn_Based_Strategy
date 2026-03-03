using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Manages turn order and round cycling for combat.
    /// Pure C# class — no MonoBehaviour. Owned by CombatSceneController.
    ///
    /// Flow:
    ///   StartRound() → builds initiative order → StartNextTurn()
    ///   StartNextTurn() → finds next alive unit → publishes TurnStartedEvent
    ///   EndCurrentTurn() → publishes TurnEndedEvent → advances index
    ///   When all units have acted → EndRound() → StartRound() (new round)
    /// </summary>
    public class TurnManager
    {
        private readonly UnitRegistry _registry;
        private readonly List<UnitRuntime> _turnOrder = new();

        private int _currentIndex = -1;
        private int _currentRound;
        private bool _roundInProgress;

        // --- Public State ---
        public UnitRuntime CurrentUnit => (_currentIndex >= 0 && _currentIndex < _turnOrder.Count)
            ? _turnOrder[_currentIndex]
            : null;

        public int CurrentRound => _currentRound;
        public bool IsRoundInProgress => _roundInProgress;

        /// <summary>Player team is always teamId 0.</summary>
        public const int PlayerTeamId = 0;

        public bool IsPlayerTurn => CurrentUnit != null && CurrentUnit.TeamId == PlayerTeamId;

        public IReadOnlyList<UnitRuntime> TurnOrder => _turnOrder;

        public TurnManager(UnitRegistry registry)
        {
            _registry = registry;
        }

        // ------------------------------------------------------------------
        // Round lifecycle
        // ------------------------------------------------------------------

        /// <summary>
        /// Begin a new round: sort by initiative, start first turn.
        /// </summary>
        public void StartRound()
        {
            _currentRound++;
            BuildTurnOrder();
            _currentIndex = -1;
            _roundInProgress = true;

            Debug.Log($"[TurnManager] === Round {_currentRound} started === ({_turnOrder.Count} units)");

            EventBus.Publish(new RoundStartedEvent { RoundNumber = _currentRound });

            StartNextTurn();
        }

        /// <summary>
        /// End the current unit's turn and advance to the next.
        /// Called by CombatSceneController (player presses End Turn or actions exhausted).
        /// </summary>
        public void EndCurrentTurn()
        {
            if (CurrentUnit == null) return;

            var unit = CurrentUnit;
            Debug.Log($"[TurnManager] Turn ended: {unit.Definition.UnitName} (id={unit.UnitId})");
            EventBus.Publish(new TurnEndedEvent { UnitId = unit.UnitId });

            StartNextTurn();
        }

        // ------------------------------------------------------------------
        // Internal
        // ------------------------------------------------------------------

        private void BuildTurnOrder()
        {
            _turnOrder.Clear();

            foreach (var unit in _registry.AllUnits)
            {
                if (!unit.IsDead)
                    _turnOrder.Add(unit);
            }

            // Sort by Initiative descending; tie-break with small random offset
            _turnOrder.Sort((a, b) =>
            {
                float initA = a.Stats.Initiative + Random.Range(0f, 0.01f);
                float initB = b.Stats.Initiative + Random.Range(0f, 0.01f);
                return initB.CompareTo(initA); // descending
            });

            // Log order
            for (int i = 0; i < _turnOrder.Count; i++)
            {
                var u = _turnOrder[i];
                Debug.Log($"[TurnManager]   #{i + 1} {u.Definition.UnitName} (Init={u.Stats.Initiative:F1}, Team={u.TeamId})");
            }
        }

        private void StartNextTurn()
        {
            _currentIndex++;

            // Skip dead units
            while (_currentIndex < _turnOrder.Count && _turnOrder[_currentIndex].IsDead)
            {
                _currentIndex++;
            }

            if (_currentIndex >= _turnOrder.Count)
            {
                // All units have acted → end the round
                EndRound();
                return;
            }

            var unit = CurrentUnit;
            unit.ResetTurnActions();

            Debug.Log($"[TurnManager] Turn started: {unit.Definition.UnitName} (id={unit.UnitId}, Team={unit.TeamId})");

            EventBus.Publish(new TurnStartedEvent
            {
                UnitId = unit.UnitId,
                IsPlayerControlled = unit.TeamId == PlayerTeamId
            });

            EventBus.Publish(new ActiveUnitChangedEvent
            {
                UnitId = unit.UnitId,
                TeamId = unit.TeamId
            });
        }

        private void EndRound()
        {
            _roundInProgress = false;
            Debug.Log($"[TurnManager] === Round {_currentRound} ended ===");
            EventBus.Publish(new RoundEndedEvent { RoundNumber = _currentRound });

            // Auto-start next round
            StartRound();
        }
    }
}
