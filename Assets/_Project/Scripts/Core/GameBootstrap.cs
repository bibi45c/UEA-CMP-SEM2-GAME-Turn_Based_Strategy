using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;
using TurnBasedTactics.Combat;
using TurnBasedTactics.UI;
using TacticalCam = global::TurnBasedTactics.Camera.TacticalCamera;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// Scene entry point. Initializes core services and wires up subsystems.
    /// Attach to the SceneContext GameObject in every gameplay scene.
    ///
    /// Initialization order:
    ///   1. GameSession (create or reuse)
    ///   2. EventBus (clear stale handlers)
    ///   3. Validate scene root references
    ///   4. Grid system
    ///   5. Unit system (spawner, selection, movement, input)
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene Root References")]
        [SerializeField] private Transform _systemsRoot;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Transform _unitsRoot;
        [SerializeField] private Transform _debugRoot;

        [Header("Subsystem Roots")]
        [SerializeField] private Transform _gridRoot;
        [SerializeField] private Transform _combatRoot;
        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private Transform _uiRoot;

        [Header("Test Spawn Data")]
        [SerializeField] private SpawnData[] _testSpawns;

        // Runtime references
        private UnitRegistry _registry;
        private CombatSceneController _combatController;

        // --- Public accessors for other systems ---
        public Transform SystemsRoot => _systemsRoot;
        public Transform WorldRoot => _worldRoot;
        public Transform UnitsRoot => _unitsRoot;
        public Transform DebugRoot => _debugRoot;
        public Transform GridRoot => _gridRoot;
        public Transform CombatRoot => _combatRoot;
        public Transform CameraRoot => _cameraRoot;
        public Transform UIRoot => _uiRoot;

        /// <summary>Singleton-like accessor for the current scene bootstrap.</summary>
        public static GameBootstrap Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameBootstrap] Duplicate bootstrap detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeCore();
        }

        private void InitializeCore()
        {
            Debug.Log("[GameBootstrap] === Initializing Core Systems ===");

            // 1. Session
            if (GameSession.Current == null)
            {
                GameSession.Create();
                Debug.Log("[GameBootstrap] New GameSession created.");
            }
            else
            {
                Debug.Log("[GameBootstrap] Reusing existing GameSession.");
            }

            // 2. EventBus
            EventBus.Clear();
            Debug.Log("[GameBootstrap] EventBus cleared.");

            // 3. Validate roots
            ValidateRoots();

            // 3b. Ensure EventSystem exists (required for uGUI raycasting)
            EnsureEventSystem();

            // 4. Grid
            InitializeGrid();

            // 5. Units
            InitializeUnits();

            // 6. Combat (TurnManager + ActionSystem)
            InitializeCombat();

            Debug.Log("[GameBootstrap] === Core initialization complete ===");
        }

        private void ValidateRoots()
        {
            if (_systemsRoot == null) Debug.LogError("[GameBootstrap] SystemsRoot not assigned!");
            if (_worldRoot == null) Debug.LogError("[GameBootstrap] WorldRoot not assigned!");
            if (_unitsRoot == null) Debug.LogError("[GameBootstrap] UnitsRoot not assigned!");
            if (_debugRoot == null) Debug.LogError("[GameBootstrap] DebugRoot not assigned!");
            if (_gridRoot == null) Debug.LogError("[GameBootstrap] GridRoot not assigned!");
            if (_combatRoot == null) Debug.LogError("[GameBootstrap] CombatRoot not assigned!");
            if (_cameraRoot == null) Debug.LogError("[GameBootstrap] CameraRoot not assigned!");
            if (_uiRoot == null) Debug.LogError("[GameBootstrap] UIRoot not assigned!");
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var esGO = new GameObject("EventSystem");
            esGO.transform.SetParent(_systemsRoot, false);
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[GameBootstrap] EventSystem created.");
        }

        private void InitializeGrid()
        {
            if (_gridRoot == null)
            {
                Debug.LogWarning("[GameBootstrap] GridRoot not assigned, skipping grid init.");
                return;
            }

            var gridMap = _gridRoot.GetComponentInChildren<HexGridMap>();
            if (gridMap == null)
            {
                Debug.LogWarning("[GameBootstrap] No HexGridMap found under GridRoot.");
                return;
            }

            gridMap.Initialize();

            var visualizer = gridMap.GetComponent<HexGridVisualizer>();
            if (visualizer != null)
            {
                visualizer.BuildCache();
                Debug.Log("[GameBootstrap] HexGridVisualizer cache built.");
            }

            Debug.Log($"[GameBootstrap] Grid initialized: {gridMap.CellCount} cells.");
        }

        private void InitializeUnits()
        {
            // Get grid map (required dependency)
            var gridMap = _gridRoot != null ? _gridRoot.GetComponentInChildren<HexGridMap>() : null;
            if (gridMap == null || !gridMap.IsInitialized)
            {
                Debug.LogWarning("[GameBootstrap] Grid not initialized, skipping unit init.");
                return;
            }

            // Create registry (plain C#) — store as field for combat init
            _registry = new UnitRegistry();
            var registry = _registry;

            // Find system MonoBehaviours
            var spawner = _systemsRoot.GetComponentInChildren<UnitSpawner>();
            var selectionMgr = _systemsRoot.GetComponentInChildren<UnitSelectionManager>();
            var movementSys = _systemsRoot.GetComponentInChildren<UnitMovementSystem>();
            var inputHandler = _systemsRoot.GetComponentInChildren<TacticalInputHandler>();

            // Range visualizer is on GridRoot (alongside HexGridVisualizer)
            var rangeVis = _gridRoot.GetComponentInChildren<MovementRangeVisualizer>();

            // Get camera
            var camera = _cameraRoot != null ? _cameraRoot.GetComponentInChildren<TacticalCam>() : null;

            // Validate
            if (spawner == null) { Debug.LogError("[GameBootstrap] UnitSpawner not found!"); return; }
            if (selectionMgr == null) { Debug.LogError("[GameBootstrap] UnitSelectionManager not found!"); return; }
            if (movementSys == null) { Debug.LogError("[GameBootstrap] UnitMovementSystem not found!"); return; }

            // Initialize in dependency order
            spawner.Initialize(gridMap, registry, _unitsRoot);
            selectionMgr.Initialize(registry);

            if (rangeVis != null)
                rangeVis.Initialize(gridMap, registry);

            movementSys.Initialize(gridMap, registry, selectionMgr, rangeVis, spawner);

            if (inputHandler != null && camera != null)
                inputHandler.Initialize(selectionMgr, movementSys, camera, gridMap, rangeVis);
            else
                Debug.LogWarning("[GameBootstrap] TacticalInputHandler or Camera not found — input disabled.");

            Debug.Log("[GameBootstrap] Unit system initialized.");

            // Spawn test units
            SpawnTestUnits(spawner, gridMap);

            // Focus camera on first player unit
            if (camera != null && registry.AllUnits.Count > 0)
            {
                foreach (var unit in registry.AllUnits)
                {
                    if (unit.TeamId == 0) // Focus on first player unit
                    {
                        Vector3 focusPos = gridMap.GetCellWorldPosition(unit.GridPosition);
                        camera.FocusOnPoint(focusPos);
                        var brain = spawner.GetBrain(unit.UnitId);
                        if (brain != null)
                            camera.SetFollowTarget(brain.transform);
                        Debug.Log($"[GameBootstrap] Camera focused on {unit.Definition.UnitName} at {focusPos}");
                        break;
                    }
                }
            }
        }

        private void SpawnTestUnits(UnitSpawner spawner, HexGridMap gridMap)
        {
            if (_testSpawns == null || _testSpawns.Length == 0)
            {
                Debug.Log("[GameBootstrap] No test spawns configured.");
                return;
            }

            foreach (var spawn in _testSpawns)
            {
                if (spawn.Definition == null)
                {
                    Debug.LogWarning("[GameBootstrap] Test spawn has null definition, skipping.");
                    continue;
                }

                var coord = new HexCoord(spawn.SpawnQ, spawn.SpawnR);

                // Verify cell is walkable
                if (gridMap.TryGetCell(coord, out HexCell cell) && cell.Walkable && !cell.IsOccupied)
                {
                    spawner.SpawnUnit(spawn.Definition, coord, spawn.TeamId);
                }
                else
                {
                    Debug.LogWarning($"[GameBootstrap] Cannot spawn {spawn.Definition.UnitName} at ({spawn.SpawnQ},{spawn.SpawnR}): " +
                                     "cell is null, unwalkable, or occupied.");
                }
            }
        }

        private void InitializeCombat()
        {
            if (_combatRoot == null)
            {
                Debug.LogWarning("[GameBootstrap] CombatRoot not assigned, skipping combat init.");
                return;
            }

            _combatController = _combatRoot.GetComponentInChildren<CombatSceneController>();
            if (_combatController == null)
            {
                Debug.LogWarning("[GameBootstrap] No CombatSceneController found under CombatRoot. " +
                                 "Add one to enable turn-based combat.");
                return;
            }

            if (_registry == null)
            {
                Debug.LogError("[GameBootstrap] UnitRegistry not created — cannot initialize combat.");
                return;
            }

            var gridMap = _gridRoot != null ? _gridRoot.GetComponentInChildren<HexGridMap>() : null;
            var spawner = _systemsRoot != null ? _systemsRoot.GetComponentInChildren<UnitSpawner>() : null;
            var selectionMgr = _systemsRoot != null ? _systemsRoot.GetComponentInChildren<UnitSelectionManager>() : null;

            if (gridMap == null)
            {
                Debug.LogError("[GameBootstrap] HexGridMap not found - cannot initialize combat.");
                return;
            }

            if (spawner == null || selectionMgr == null)
            {
                Debug.LogError("[GameBootstrap] Unit systems missing - cannot initialize combat.");
                return;
            }

            _combatController.Initialize(_registry, gridMap, spawner, selectionMgr);
            InitializeCombatHud();
            InitializeCombatWorldUI(spawner);
            InitializeTurnOrderBar();
            InitializePartyPortraits(selectionMgr);
            _combatController.StartCombat();
            Debug.Log("[GameBootstrap] Combat system initialized and started.");
        }

        private void InitializeCombatHud()
        {
            if (_uiRoot == null || _combatController == null)
                return;

            var hud = _uiRoot.GetComponent<CombatHudController>();
            if (hud == null)
                hud = _uiRoot.gameObject.AddComponent<CombatHudController>();

            hud.Initialize(_combatController);
            Debug.Log("[GameBootstrap] Combat HUD initialized.");
        }

        private void InitializeCombatWorldUI(UnitSpawner spawner)
        {
            if (_uiRoot == null || _registry == null || spawner == null)
                return;

            var combatUI = _uiRoot.GetComponent<CombatUIManager>();
            if (combatUI == null)
                combatUI = _uiRoot.gameObject.AddComponent<CombatUIManager>();

            combatUI.Initialize(_registry, spawner);
            Debug.Log("[GameBootstrap] Combat World UI (HP bars + floating text) initialized.");
        }

        private void InitializeTurnOrderBar()
        {
            if (_uiRoot == null || _combatController == null)
                return;

            var turnOrderBar = _uiRoot.GetComponent<TurnOrderBar>();
            if (turnOrderBar == null)
                turnOrderBar = _uiRoot.gameObject.AddComponent<TurnOrderBar>();

            turnOrderBar.Initialize(_combatController.TurnManager, _registry);
            Debug.Log("[GameBootstrap] Turn Order Bar initialized.");
        }

        private void InitializePartyPortraits(UnitSelectionManager selectionMgr)
        {
            if (_uiRoot == null || _registry == null || _combatController == null || selectionMgr == null)
                return;

            var panel = _uiRoot.GetComponent<PartyPortraitPanel>();
            if (panel == null)
                panel = _uiRoot.gameObject.AddComponent<PartyPortraitPanel>();

            panel.Initialize(_registry, selectionMgr, _combatController);
            Debug.Log("[GameBootstrap] Party Portrait Panel initialized.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// Serializable spawn data for test/encounter units.
    /// </summary>
    [Serializable]
    public struct SpawnData
    {
        public UnitDefinition Definition;
        public int SpawnQ;
        public int SpawnR;
        public int TeamId;
    }
}
