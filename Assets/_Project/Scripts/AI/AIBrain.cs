using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.AI
{
    /// <summary>
    /// AI decision controller for enemy turns.
    /// Executes a simple but effective loop: pick target → move toward → attack if in range.
    /// MonoBehaviour for coroutine support (paced actions with visual delays).
    /// </summary>
    public class AIBrain : MonoBehaviour
    {
        [SerializeField] private float _thinkDelay = 0.5f;
        [SerializeField] private float _moveDelay = 0.3f;
        [SerializeField] private float _attackDelay = 0.8f;

        private UnitRegistry _registry;
        private HexGridMap _gridMap;
        private UnitSpawner _spawner;
        private ActionSystem _actionSystem;
        private BasicAttackSystem _attackSystem;
        private CombatSceneController _combatController;

        private bool _isExecuting;

        public bool IsExecuting => _isExecuting;

        public void Initialize(
            UnitRegistry registry,
            HexGridMap gridMap,
            UnitSpawner spawner,
            ActionSystem actionSystem,
            BasicAttackSystem attackSystem,
            CombatSceneController combatController)
        {
            _registry = registry;
            _gridMap = gridMap;
            _spawner = spawner;
            _actionSystem = actionSystem;
            _attackSystem = attackSystem;
            _combatController = combatController;
        }

        /// <summary>
        /// Execute a full AI turn for the given unit.
        /// Called by CombatSceneController when an enemy turn starts.
        /// </summary>
        public void ExecuteTurn(UnitRuntime unit)
        {
            if (_isExecuting)
            {
                Debug.LogWarning("[AIBrain] Already executing a turn, ignoring.");
                return;
            }

            StartCoroutine(ExecuteTurnCoroutine(unit));
        }

        private IEnumerator ExecuteTurnCoroutine(UnitRuntime unit)
        {
            _isExecuting = true;

            Debug.Log($"[AIBrain] === AI thinking for {unit.Definition.UnitName} (id={unit.UnitId}) ===");

            // Brief pause so player can see it's the enemy's turn
            yield return new WaitForSeconds(_thinkDelay);

            // Safety: unit may have died between turn start and coroutine execution
            if (unit.IsDead)
            {
                _isExecuting = false;
                EndTurn();
                yield break;
            }

            // 1. Pick a target
            var playerUnits = _registry.GetTeamUnits(TurnManager.PlayerTeamId);
            UnitRuntime target = AIScorer.PickBestTarget(unit, playerUnits);

            if (target == null)
            {
                Debug.Log("[AIBrain] No valid targets found. Ending turn.");
                _isExecuting = false;
                EndTurn();
                yield break;
            }

            Debug.Log($"[AIBrain] Target selected: {target.Definition.UnitName} (HP={target.CurrentHP}/{target.Stats.MaxHP})");

            // 2. Move toward target if needed and can move
            if (_actionSystem.CanMove(unit))
            {
                int distToTarget = unit.GridPosition.DistanceTo(target.GridPosition);

                if (distToTarget > _attackSystem.Range)
                {
                    // Calculate reachable cells
                    var pathConfig = HexPathfinder.PathConfig.Default;
                    var reachable = HexPathfinder.GetReachable(
                        _gridMap, unit.GridPosition, unit.Stats.MovementPoints, pathConfig);

                    HexCoord moveTarget = AIScorer.FindBestMoveToward(
                        unit, target, _attackSystem.Range, _gridMap, reachable);

                    if (moveTarget != unit.GridPosition)
                    {
                        yield return StartCoroutine(ExecuteMove(unit, moveTarget));
                    }
                    else
                    {
                        Debug.Log("[AIBrain] No better position found, staying put.");
                        _actionSystem.SpendMoveAction(unit);
                    }
                }
                else
                {
                    // Already in range — save the move action (still spend it per action economy)
                    Debug.Log("[AIBrain] Already in attack range, skipping movement.");
                    _actionSystem.SpendMoveAction(unit);
                }
            }

            // 3. Attack if in range and can act
            if (!unit.IsDead && _actionSystem.CanAct(unit))
            {
                // Re-evaluate target in case the original died or distances changed
                playerUnits = _registry.GetTeamUnits(TurnManager.PlayerTeamId);
                target = FindAttackableTarget(unit, playerUnits);

                if (target != null)
                {
                    yield return new WaitForSeconds(_attackDelay);
                    ExecuteAttack(unit, target);
                    yield return new WaitForSeconds(1.0f); // Wait for attack animation
                }
                else
                {
                    Debug.Log("[AIBrain] No target in attack range after moving.");
                }
            }

            _isExecuting = false;
            EndTurn();
        }

        private IEnumerator ExecuteMove(UnitRuntime unit, HexCoord destination)
        {
            var pathConfig = HexPathfinder.PathConfig.Default;
            var path = HexPathfinder.FindPath(_gridMap, unit.GridPosition, destination, pathConfig);

            if (path == null || path.Count < 2)
            {
                Debug.Log("[AIBrain] No path to destination, skipping move.");
                _actionSystem.SpendMoveAction(unit);
                yield break;
            }

            HexCoord from = unit.GridPosition;
            HexCoord to = path[path.Count - 1];

            // Update grid state (authoritative)
            _gridMap.SetOccupant(from, -1);
            _gridMap.SetOccupant(to, unit.UnitId);
            unit.SetGridPosition(to);
            _actionSystem.SpendMoveAction(unit);

            // Publish move event
            EventBus.Publish(new UnitMoveStartedEvent
            {
                UnitId = unit.UnitId,
                From = from,
                To = to,
                Path = path
            });

            // Convert to world path and animate
            var worldPath = new List<Vector3>(path.Count);
            foreach (var coord in path)
            {
                worldPath.Add(_gridMap.GetCellWorldPosition(coord));
            }

            UnitBrain brain = _spawner.GetBrain(unit.UnitId);
            if (brain != null)
            {
                var visual = brain.GetComponent<UnitVisual>();
                if (visual != null)
                {
                    bool moveComplete = false;
                    visual.StartPathMovement(worldPath, () => moveComplete = true);

                    // Wait until movement animation finishes
                    while (!moveComplete)
                        yield return null;
                }
            }

            yield return new WaitForSeconds(_moveDelay);

            Debug.Log($"[AIBrain] {unit.Definition.UnitName} moved from {from} to {to}");

            EventBus.Publish(new UnitMoveCompletedEvent
            {
                UnitId = unit.UnitId,
                FinalPosition = to
            });
        }

        private void ExecuteAttack(UnitRuntime attacker, UnitRuntime target)
        {
            var result = _attackSystem.Execute(attacker, target);
            if (!result.Success)
            {
                Debug.LogWarning($"[AIBrain] Attack failed: {result.FailureReason}");
                return;
            }

            _actionSystem.SpendMainAction(attacker);

            // Play attack animation
            UnitBrain brain = _spawner.GetBrain(attacker.UnitId);
            if (brain != null)
            {
                var visual = brain.GetComponent<UnitVisual>();
                if (visual != null)
                {
                    // Face the target before attacking
                    Vector3 targetPos = _gridMap.GetCellWorldPosition(target.GridPosition);
                    Vector3 dir = targetPos - brain.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        brain.transform.rotation = Quaternion.LookRotation(dir);

                    visual.PlayActionAnimation();
                }
            }

            EventBus.Publish(new UnitDamagedEvent
            {
                AttackerUnitId = attacker.UnitId,
                TargetUnitId = target.UnitId,
                DamageAmount = result.DamageDealt,
                RemainingHP = target.CurrentHP,
                WasCritical = result.WasCritical,
                DidKill = result.DidKill
            });

            Debug.Log($"[AIBrain] {attacker.Definition.UnitName} attacks {target.Definition.UnitName} " +
                      $"for {result.DamageDealt} damage{(result.WasCritical ? " (CRIT!)" : "")}");

            if (result.DidKill)
            {
                Debug.Log($"[AIBrain] {target.Definition.UnitName} has been killed!");
                HandleUnitDeath(target);
            }
        }

        /// <summary>
        /// Find the best target that is currently within attack range.
        /// </summary>
        private UnitRuntime FindAttackableTarget(UnitRuntime attacker, List<UnitRuntime> enemies)
        {
            UnitRuntime best = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                int dist = attacker.GridPosition.DistanceTo(enemy.GridPosition);
                if (dist > _attackSystem.Range) continue;

                float score = AIScorer.ScoreTarget(attacker, enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        private void HandleUnitDeath(UnitRuntime unit)
        {
            if (_gridMap != null)
                _gridMap.SetOccupant(unit.GridPosition, -1);

            _registry?.Unregister(unit.UnitId);
            _spawner?.DespawnUnit(unit.UnitId);

            EventBus.Publish(new UnitDiedEvent
            {
                UnitId = unit.UnitId,
                Position = unit.GridPosition
            });
        }

        private void EndTurn()
        {
            if (_combatController != null && _combatController.IsCombatActive)
            {
                _combatController.EndCurrentTurn();
            }
        }
    }
}
