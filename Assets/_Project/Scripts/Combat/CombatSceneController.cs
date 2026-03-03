using System.Collections;
using UnityEngine;
using TurnBasedTactics.Core;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Top-level combat controller. Manages combat lifecycle and state transitions.
    /// Attach to a GameObject under CombatRoot in the scene hierarchy.
    /// </summary>
    public class CombatSceneController : MonoBehaviour
    {
        public enum CombatState
        {
            Idle,
            Starting,
            PlayerTurn,
            EnemyTurn,
            Victory,
            Defeat
        }

        public enum QueuedActionType
        {
            None,
            Move,
            Attack,
            Heal
        }

        private UnitRegistry _registry;
        private HexGridMap _gridMap;
        private UnitSpawner _spawner;
        private UnitSelectionManager _selectionManager;
        private TurnManager _turnManager;
        private ActionSystem _actionSystem;
        private BasicAttackSystem _basicAttackSystem;
        private HealSkillSystem _healSkillSystem;

        private CombatState _state = CombatState.Idle;
        private QueuedActionType _queuedAction = QueuedActionType.None;
        private bool _initialized;
        private bool _actionAnimating;

        public CombatState State => _state;
        public TurnManager TurnManager => _turnManager;
        public ActionSystem ActionSystem => _actionSystem;
        public QueuedActionType QueuedAction => _queuedAction;
        public bool HasQueuedAction => _queuedAction != QueuedActionType.None;
        public bool IsPlayerTurn => _state == CombatState.PlayerTurn;
        public bool IsCombatActive => _state == CombatState.PlayerTurn || _state == CombatState.EnemyTurn;
        public bool IsActionAnimating => _actionAnimating;
        public UnitRuntime CurrentUnit => _turnManager?.CurrentUnit;

        public void Initialize(
            UnitRegistry registry,
            HexGridMap gridMap,
            UnitSpawner spawner,
            UnitSelectionManager selectionManager)
        {
            _registry = registry;
            _gridMap = gridMap;
            _spawner = spawner;
            _selectionManager = selectionManager;
            _actionSystem = new ActionSystem();
            _turnManager = new TurnManager(registry);
            _basicAttackSystem = new BasicAttackSystem();
            _healSkillSystem = new HealSkillSystem();
            _initialized = true;

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            Debug.Log("[CombatSceneController] Initialized.");
        }

        public void StartCombat()
        {
            if (!_initialized)
            {
                Debug.LogError("[CombatSceneController] Not initialized! Call Initialize() first.");
                return;
            }

            _state = CombatState.Starting;
            Debug.Log("[CombatSceneController] === Combat Started ===");

            EventBus.Publish(new CombatStartedEvent());
            _turnManager.StartRound();
        }

        public void EndCurrentTurn()
        {
            if (!IsCombatActive || _actionAnimating)
                return;

            ClearQueuedAction();
            _turnManager.EndCurrentTurn();
        }

        public void QueueAttackAction()
        {
            if (!CanQueueMainAction())
                return;

            _queuedAction = QueuedActionType.Attack;
            Debug.Log("[CombatSceneController] Queued Attack. Select an enemy target.");
        }

        public void QueueMoveAction()
        {
            if (!IsPlayerTurn || CurrentUnit == null)
                return;

            if (!_actionSystem.CanMove(CurrentUnit))
            {
                Debug.LogWarning("[CombatSceneController] Active unit has no move action remaining.");
                return;
            }

            _queuedAction = QueuedActionType.Move;

            // Auto-select the active unit and refresh to show movement range immediately
            if (_selectionManager != null)
            {
                _selectionManager.SelectUnit(CurrentUnit.UnitId);
                _selectionManager.RefreshSelection();
            }

            Debug.Log("[CombatSceneController] Queued Move. Click a reachable cell to move.");
        }

        public void QueueHealAction()
        {
            if (!CanQueueMainAction())
                return;

            _queuedAction = QueuedActionType.Heal;
            Debug.Log("[CombatSceneController] Queued Heal. Select an ally target.");
        }

        public void ClearQueuedAction()
        {
            _queuedAction = QueuedActionType.None;
        }

        public string GetQueuedActionHint()
        {
            if (!IsCombatActive)
                return "Combat inactive.";

            if (!IsPlayerTurn)
                return "Waiting for the enemy turn to finish.";

            return _queuedAction switch
            {
                QueuedActionType.Move => "Move selected: right-click a reachable highlighted hex.",
                QueuedActionType.Attack => $"Attack selected: click an enemy within {_basicAttackSystem.Range} hex.",
                QueuedActionType.Heal => $"Heal selected: click an ally within {_healSkillSystem.Range} hex.",
                _ => "Right-click a reachable hex to move, or choose Attack/Heal and click a unit target."
            };
        }

        public bool TryExecuteQueuedActionOnTarget(UnitRuntime target)
        {
            if (!HasQueuedAction || !IsPlayerTurn || target == null || _actionAnimating)
                return false;

            switch (_queuedAction)
            {
                case QueuedActionType.Attack:
                    ExecuteAttack(target);
                    return true;
                case QueuedActionType.Heal:
                    ExecuteHeal(target);
                    return true;
                default:
                    return false;
            }
        }

        private bool CanQueueMainAction()
        {
            if (!IsPlayerTurn || CurrentUnit == null)
                return false;

            if (!_actionSystem.CanAct(CurrentUnit))
            {
                Debug.LogWarning("[CombatSceneController] Active unit has no main action remaining.");
                return false;
            }

            return true;
        }

        private void ExecuteAttack(UnitRuntime target)
        {
            var attacker = CurrentUnit;
            if (attacker == null)
                return;

            var attack = _basicAttackSystem.Execute(attacker, target);
            if (!attack.Success)
            {
                Debug.LogWarning($"[CombatSceneController] Attack failed: {attack.FailureReason}");
                return;
            }

            _actionSystem.SpendMainAction(attacker);
            _actionAnimating = true;
            PlayActionAnimation(attacker.UnitId);

            EventBus.Publish(new UnitDamagedEvent
            {
                AttackerUnitId = attacker.UnitId,
                TargetUnitId = target.UnitId,
                DamageAmount = attack.DamageDealt,
                RemainingHP = target.CurrentHP,
                WasCritical = attack.WasCritical,
                DidKill = attack.DidKill
            });

            Debug.Log($"[CombatSceneController] {attacker.Definition.UnitName} hit {target.Definition.UnitName} for {attack.DamageDealt}.");

            ClearQueuedAction();

            if (attack.DidKill)
                HandleUnitDeath(target);

            StartCoroutine(WaitThenProcessPostAction(attacker));
        }

        private void ExecuteHeal(UnitRuntime target)
        {
            var source = CurrentUnit;
            if (source == null)
                return;

            var heal = _healSkillSystem.Execute(source, target);
            if (!heal.Success)
            {
                Debug.LogWarning($"[CombatSceneController] Heal failed: {heal.FailureReason}");
                return;
            }

            _actionSystem.SpendMainAction(source);
            _actionAnimating = true;
            PlayActionAnimation(source.UnitId);

            EventBus.Publish(new UnitHealedEvent
            {
                SourceUnitId = source.UnitId,
                TargetUnitId = target.UnitId,
                HealAmount = heal.HealAmount,
                CurrentHP = target.CurrentHP
            });

            Debug.Log($"[CombatSceneController] {source.Definition.UnitName} healed {target.Definition.UnitName} for {heal.HealAmount}.");

            ClearQueuedAction();

            StartCoroutine(WaitThenProcessPostAction(source));
        }

        private void PlayActionAnimation(int unitId)
        {
            if (_spawner == null)
                return;

            var brain = _spawner.GetBrain(unitId);
            if (brain == null)
                return;

            var visual = brain.GetComponent<UnitVisual>();
            if (visual != null)
                visual.PlayActionAnimation();
        }

        private IEnumerator WaitThenProcessPostAction(UnitRuntime unit)
        {
            yield return new WaitForSeconds(1.0f);
            _actionAnimating = false;

            if (unit == null || unit.IsDead)
                yield break;

            // Re-select the unit so player can see remaining actions
            if (_selectionManager != null)
                _selectionManager.SelectUnit(unit.UnitId);

            if (_actionSystem.IsTurnComplete(unit))
            {
                Debug.Log($"[CombatSceneController] All actions spent for {unit.Definition.UnitName}, auto-ending turn.");
                EndCurrentTurn();
            }
        }

        private void HandleUnitDeath(UnitRuntime unit)
        {
            if (unit == null)
                return;

            if (_selectionManager != null && _selectionManager.SelectedUnit == unit)
                _selectionManager.DeselectUnit();

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

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (!_initialized)
                return;

            ClearQueuedAction();

            if (evt.IsPlayerControlled)
            {
                _state = CombatState.PlayerTurn;
                Debug.Log($"[CombatSceneController] -> Player turn (Unit id={evt.UnitId})");
            }
            else
            {
                _state = CombatState.EnemyTurn;
                Debug.Log($"[CombatSceneController] -> Enemy turn (Unit id={evt.UnitId}) -> auto-skipping (no AI yet)");
                Invoke(nameof(AutoEndEnemyTurn), 0.1f);
            }
        }

        private void AutoEndEnemyTurn()
        {
            if (_state == CombatState.EnemyTurn)
                EndCurrentTurn();
        }

        private void OnUnitMoveCompleted(UnitMoveCompletedEvent evt)
        {
            if (!IsCombatActive)
                return;

            var unit = _turnManager.CurrentUnit;
            if (unit == null || unit.UnitId != evt.UnitId)
                return;

            if (_actionSystem.CanMove(unit))
            {
                _actionSystem.SpendMoveAction(unit);
            }

            if (_actionSystem.IsTurnComplete(unit))
            {
                Debug.Log($"[CombatSceneController] All actions spent for {unit.Definition.UnitName}, auto-ending turn.");
                EndCurrentTurn();
            }
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (!IsCombatActive)
                return;

            CheckCombatEnd();
        }

        private void CheckCombatEnd()
        {
            var playerUnits = _registry.GetTeamUnits(TurnManager.PlayerTeamId);
            bool playerAlive = playerUnits.Count > 0;

            bool enemyAlive = false;
            foreach (var unit in _registry.AllUnits)
            {
                if (unit.TeamId != TurnManager.PlayerTeamId && !unit.IsDead)
                {
                    enemyAlive = true;
                    break;
                }
            }

            if (!enemyAlive)
            {
                EndCombat(TurnManager.PlayerTeamId);
            }
            else if (!playerAlive)
            {
                int enemyTeam = 1;
                foreach (var unit in _registry.AllUnits)
                {
                    if (unit.TeamId != TurnManager.PlayerTeamId && !unit.IsDead)
                    {
                        enemyTeam = unit.TeamId;
                        break;
                    }
                }

                EndCombat(enemyTeam);
            }
        }

        private void EndCombat(int winningTeamId)
        {
            ClearQueuedAction();

            if (winningTeamId == TurnManager.PlayerTeamId)
            {
                _state = CombatState.Victory;
                Debug.Log("[CombatSceneController] === VICTORY ===");
            }
            else
            {
                _state = CombatState.Defeat;
                Debug.Log("[CombatSceneController] === DEFEAT ===");
            }

            EventBus.Publish(new CombatEndedEvent { WinningTeamId = winningTeamId });
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }
    }
}
