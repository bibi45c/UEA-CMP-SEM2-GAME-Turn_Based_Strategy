using System.Collections;
using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.AI;
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
            Ability
        }

        [Header("Surface Definitions")]
        [SerializeField] private SurfaceDefinition[] _surfaceDefinitions;

        private UnitRegistry _registry;
        private HexGridMap _gridMap;
        private UnitSpawner _spawner;
        private UnitSelectionManager _selectionManager;
        private TurnManager _turnManager;
        private ActionSystem _actionSystem;
        private AbilityExecutor _abilityExecutor;
        private StatusManager _statusManager;
        private SurfaceSystem _surfaceSystem;
        private AIBrain _aiBrain;

        private CombatState _state = CombatState.Idle;
        private QueuedActionType _queuedAction = QueuedActionType.None;
        private AbilityDefinition _queuedAbility;
        private bool _initialized;
        private bool _actionAnimating;

        public CombatState State => _state;
        public TurnManager TurnManager => _turnManager;
        public ActionSystem ActionSystem => _actionSystem;
        public AbilityExecutor AbilityExecutor => _abilityExecutor;
        public StatusManager StatusManager => _statusManager;
        public SurfaceSystem SurfaceSystem => _surfaceSystem;
        public QueuedActionType QueuedAction => _queuedAction;
        public AbilityDefinition QueuedAbility => _queuedAbility;
        public bool HasQueuedAction => _queuedAction != QueuedActionType.None;
        public bool IsPlayerTurn => _state == CombatState.PlayerTurn;
        public bool IsCombatActive => _state == CombatState.PlayerTurn || _state == CombatState.EnemyTurn;
        public bool IsActionAnimating => _actionAnimating;
        public bool IsEnemyActing => _state == CombatState.EnemyTurn && _aiBrain != null && _aiBrain.IsExecuting;
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
            _statusManager = new StatusManager();
            _surfaceSystem = new SurfaceSystem();
            if (_surfaceDefinitions != null)
            {
                foreach (var def in _surfaceDefinitions)
                    _surfaceSystem.RegisterDefinition(def);
            }
            _abilityExecutor = new AbilityExecutor();
            _abilityExecutor.SetStatusManager(_statusManager);
            _abilityExecutor.SetSurfaceSystem(_surfaceSystem);

            // Initialize AI
            _aiBrain = GetComponent<AIBrain>();
            if (_aiBrain == null)
                _aiBrain = gameObject.AddComponent<AIBrain>();
            _aiBrain.Initialize(registry, gridMap, spawner, _actionSystem, _abilityExecutor, this);

            _initialized = true;

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);

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

        public void QueueMoveAction()
        {
            if (!IsPlayerTurn || CurrentUnit == null)
                return;

            if (!_actionSystem.CanMove(CurrentUnit))
            {
                Debug.LogWarning("[CombatSceneController] Active unit has no AP for movement.");
                return;
            }

            _queuedAction = QueuedActionType.Move;
            _queuedAbility = null;

            // Auto-select the active unit and refresh to show movement range immediately
            if (_selectionManager != null)
            {
                _selectionManager.SelectUnit(CurrentUnit.UnitId);
                _selectionManager.RefreshSelection();
            }

            Debug.Log($"[CombatSceneController] Queued Move. AP remaining: {CurrentUnit.CurrentAP}/{CurrentUnit.MaxAP}");
        }

        public void QueueAbility(AbilityDefinition ability)
        {
            if (ability == null) return;
            if (!CanQueueMainAction()) return;

            // Check if unit can afford this specific ability
            if (!_actionSystem.CanUseAbility(CurrentUnit, ability))
            {
                Debug.LogWarning($"[CombatSceneController] Not enough AP ({CurrentUnit.CurrentAP}) for {ability.AbilityName} (cost {ability.ApCost}).");
                return;
            }

            _queuedAction = QueuedActionType.Ability;
            _queuedAbility = ability;
            Debug.Log($"[CombatSceneController] Queued {ability.AbilityName}. Select a target.");
        }

        public void ClearQueuedAction()
        {
            _queuedAction = QueuedActionType.None;
            _queuedAbility = null;
        }

        public string GetQueuedActionHint()
        {
            if (!IsCombatActive)
                return "Combat inactive.";

            if (!IsPlayerTurn)
                return "Waiting for the enemy turn to finish.";

            if (_queuedAction == QueuedActionType.Move)
                return "Move selected: right-click a reachable highlighted hex.";

            if (_queuedAction == QueuedActionType.Ability && _queuedAbility != null)
            {
                string targetHint = _queuedAbility.TargetingType switch
                {
                    TargetingType.SingleEnemy => "an enemy",
                    TargetingType.SingleAlly => "an ally",
                    TargetingType.Self => "yourself (click self)",
                    TargetingType.CircleAOE => "a target area",
                    _ => "a target"
                };
                return $"{_queuedAbility.AbilityName} selected: click {targetHint} within {_queuedAbility.Range} hex.";
            }

            return "Right-click a reachable hex to move, or choose an ability and click a target.";
        }

        public bool TryExecuteQueuedActionOnTarget(UnitRuntime target)
        {
            if (!HasQueuedAction || !IsPlayerTurn || target == null || _actionAnimating)
                return false;

            if (_queuedAction == QueuedActionType.Ability && _queuedAbility != null)
            {
                ExecuteAbility(target);
                return true;
            }

            return false;
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

        private void ExecuteAbility(UnitRuntime target)
        {
            var caster = CurrentUnit;
            if (caster == null || _queuedAbility == null)
                return;

            var result = _abilityExecutor.Execute(_queuedAbility, caster, target, _gridMap, _registry);
            if (!result.Success)
            {
                Debug.LogWarning($"[CombatSceneController] {_queuedAbility.AbilityName} failed: {result.FailureReason}");
                return;
            }

            _actionSystem.SpendAbilityAP(caster, _queuedAbility);
            _actionAnimating = true;
            PlayActionAnimation(caster.UnitId);

            // Publish appropriate events based on effect type
            if (result.TotalDamage > 0)
            {
                EventBus.Publish(new UnitDamagedEvent
                {
                    AttackerUnitId = caster.UnitId,
                    TargetUnitId = target.UnitId,
                    DamageAmount = result.TotalDamage,
                    RemainingHP = target.CurrentHP,
                    WasCritical = result.WasCritical,
                    DidKill = result.DidKill
                });
                Debug.Log($"[CombatSceneController] {caster.Definition.UnitName} used {_queuedAbility.AbilityName} on {target.Definition.UnitName} for {result.TotalDamage} damage.");
            }

            if (result.TotalHealing > 0)
            {
                EventBus.Publish(new UnitHealedEvent
                {
                    SourceUnitId = caster.UnitId,
                    TargetUnitId = target.UnitId,
                    HealAmount = result.TotalHealing,
                    CurrentHP = target.CurrentHP
                });
                Debug.Log($"[CombatSceneController] {caster.Definition.UnitName} used {_queuedAbility.AbilityName} on {target.Definition.UnitName} for {result.TotalHealing} healing.");
            }

            ClearQueuedAction();

            if (result.DidKill)
                HandleUnitDeath(target);

            StartCoroutine(WaitThenProcessPostAction(caster));
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

            // Clear grid occupancy immediately so pathfinding works
            if (_gridMap != null)
                _gridMap.SetOccupant(unit.GridPosition, -1);

            // Remove from registry immediately so turn order updates
            _registry?.Unregister(unit.UnitId);

            // Clear all active statuses
            _statusManager?.ClearAllStatuses(unit.UnitId);

            // Publish death event before animation (so UI/turn checks update immediately)
            EventBus.Publish(new UnitDiedEvent
            {
                UnitId = unit.UnitId,
                Position = unit.GridPosition
            });

            // Play death animation, then destroy the GameObject
            if (_spawner != null)
                _spawner.DespawnWithDeathAnimation(unit.UnitId);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (!_initialized)
                return;

            ClearQueuedAction();

            // Process status effects at the start of every unit's turn
            var unit = _registry.GetUnit(evt.UnitId);
            if (unit != null && _statusManager != null)
            {
                var tickResult = _statusManager.ProcessTurnStart(unit);

                if (tickResult.TotalDamage > 0)
                {
                    EventBus.Publish(new UnitDamagedEvent
                    {
                        AttackerUnitId = -1, // Status effect damage has no attacker
                        TargetUnitId = unit.UnitId,
                        DamageAmount = tickResult.TotalDamage,
                        RemainingHP = unit.CurrentHP,
                        WasCritical = false,
                        DidKill = tickResult.DidKill
                    });
                }

                if (tickResult.TotalHealing > 0)
                {
                    EventBus.Publish(new UnitHealedEvent
                    {
                        SourceUnitId = -1,
                        TargetUnitId = unit.UnitId,
                        HealAmount = tickResult.TotalHealing,
                        CurrentHP = unit.CurrentHP
                    });
                }

                if (tickResult.DidKill)
                {
                    HandleUnitDeath(unit);
                    return; // Don't proceed with turn for a dead unit
                }
            }

            // Apply surface effects for units starting their turn on a surface
            unit = _registry.GetUnit(evt.UnitId);
            if (unit != null && !unit.IsDead && _surfaceSystem != null)
            {
                var surfaceResult = _surfaceSystem.ApplyOnEnterEffects(unit, unit.GridPosition, _statusManager);
                if (surfaceResult.TotalDamage > 0)
                {
                    EventBus.Publish(new UnitDamagedEvent
                    {
                        AttackerUnitId = -1,
                        TargetUnitId = unit.UnitId,
                        DamageAmount = surfaceResult.TotalDamage,
                        RemainingHP = unit.CurrentHP,
                        WasCritical = false,
                        DidKill = surfaceResult.DidKill
                    });
                }

                if (surfaceResult.DidKill)
                {
                    HandleUnitDeath(unit);
                    return;
                }
            }

            if (evt.IsPlayerControlled)
            {
                _state = CombatState.PlayerTurn;
                Debug.Log($"[CombatSceneController] -> Player turn (Unit id={evt.UnitId})");
            }
            else
            {
                _state = CombatState.EnemyTurn;
                Debug.Log($"[CombatSceneController] -> Enemy turn (Unit id={evt.UnitId})");

                unit = _registry.GetUnit(evt.UnitId);
                if (unit != null && _aiBrain != null)
                {
                    _aiBrain.ExecuteTurn(unit);
                }
                else
                {
                    // Fallback: end turn immediately if AI not available
                    EndCurrentTurn();
                }
            }
        }

        private void OnUnitMoveCompleted(UnitMoveCompletedEvent evt)
        {
            if (!IsCombatActive)
                return;

            // Apply surface on-enter effects to any unit that just moved
            var movedUnit = _registry.GetUnit(evt.UnitId);
            if (movedUnit != null && !movedUnit.IsDead && _surfaceSystem != null)
            {
                var surfaceResult = _surfaceSystem.ApplyOnEnterEffects(movedUnit, evt.FinalPosition, _statusManager);
                if (surfaceResult.TotalDamage > 0)
                {
                    EventBus.Publish(new UnitDamagedEvent
                    {
                        AttackerUnitId = -1,
                        TargetUnitId = movedUnit.UnitId,
                        DamageAmount = surfaceResult.TotalDamage,
                        RemainingHP = movedUnit.CurrentHP,
                        WasCritical = false,
                        DidKill = surfaceResult.DidKill
                    });

                    if (surfaceResult.DidKill)
                    {
                        HandleUnitDeath(movedUnit);
                        return;
                    }
                }
            }

            var unit = _turnManager.CurrentUnit;
            if (unit == null || unit.UnitId != evt.UnitId)
                return;

            // AI manages its own action spending — only handle player moves here
            if (unit.TeamId != TurnManager.PlayerTeamId)
                return;

            // Spend AP based on actual hexes moved
            _actionSystem.SpendMoveAP(unit, evt.HexesMoved);

            if (_actionSystem.IsTurnComplete(unit))
            {
                Debug.Log($"[CombatSceneController] All AP spent for {unit.Definition.UnitName}, auto-ending turn.");
                EndCurrentTurn();
            }
        }

        private void OnRoundEnded(RoundEndedEvent evt)
        {
            if (_surfaceSystem != null && _gridMap != null)
                _surfaceSystem.ProcessRoundEnd(_gridMap);
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
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
        }
    }
}
