using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TurnBasedTactics.Grid;
using TurnBasedTactics.Units;
using TurnBasedTactics.Combat;
using TurnBasedTactics.UI;
using TurnBasedTactics.Exploration;
using TacticalCam = global::TurnBasedTactics.Camera.TacticalCamera;
using CameraShake = global::TurnBasedTactics.Camera.CameraShake;

namespace TurnBasedTactics.Core
{
    public enum GamePhase { Exploration, Combat }

    /// <summary>
    /// Scene entry point. Initializes core services and wires up subsystems.
    /// Supports two-phase gameplay: Exploration (free movement) → Combat (grid-based turns).
    ///
    /// Initialization order:
    ///   1. GameSession (create or reuse)
    ///   2. EventBus (clear stale handlers)
    ///   3. Validate scene root references
    ///   4. Grid system
    ///   5. Exploration OR Combat depending on _startInExploration flag
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

        [Header("Audio")]
        [SerializeField] private CombatAudioConfig _audioConfig;

        [Header("UI")]
        [SerializeField] private HUDSpriteConfig _hudSpriteConfig;

        [Header("Game Phase")]
        [Tooltip("Start in exploration mode before combat")]
        [SerializeField] private bool _startInExploration = true;

        [Header("Exploration Config")]
        [Tooltip("World position for the party to spawn (dungeon entrance)")]
        [SerializeField] private Vector3 _explorationSpawnPos = new Vector3(-3.57f, -15.01f, -266.54f);

        [Tooltip("Party leader unit definition (controlled by player)")]
        [SerializeField] private UnitDefinition _explorationLeader;

        [Tooltip("Follower unit definitions (auto-follow the leader)")]
        [SerializeField] private UnitDefinition[] _explorationFollowers;

        [Header("Test Spawn Data")]
        [SerializeField] private SpawnData[] _testSpawns;

        // Runtime references
        private UnitRegistry _registry;
        private CombatSceneController _combatController;
        private ExplorationController _explorationController;
        private ExplorationHUD _explorationHUD;
        private ExplorationMinimap _explorationMinimap;
        private SpawnData[] _combatSpawnOverrides;
        private GamePhase _currentPhase;

        // --- Public accessors for other systems ---
        public Transform SystemsRoot => _systemsRoot;
        public Transform WorldRoot => _worldRoot;
        public Transform UnitsRoot => _unitsRoot;
        public Transform DebugRoot => _debugRoot;
        public Transform GridRoot => _gridRoot;
        public Transform CombatRoot => _combatRoot;
        public Transform CameraRoot => _cameraRoot;
        public Transform UIRoot => _uiRoot;
        public GamePhase CurrentPhase => _currentPhase;
        public ExplorationController ExplorationCtrl => _explorationController;

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

        private void Start()
        {
            // Apply exploration camera zoom after all Awake() calls have finished
            // (TacticalCamera.Awake resets zoom to defaults, so we must set ours in Start)
            if (_currentPhase == GamePhase.Exploration)
            {
                var camera = _cameraRoot != null ? _cameraRoot.GetComponentInChildren<TacticalCam>() : null;
                if (camera != null)
                    camera.SetZoom(5f, instant: true);
            }
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

            // 3c. Wire HUD sprite config into DOS2Theme
            if (_hudSpriteConfig != null)
            {
                DOS2Theme.Sprites = _hudSpriteConfig;
                Debug.Log("[GameBootstrap] HUD sprite config wired into DOS2Theme.");
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] No HUDSpriteConfig assigned — UI will use fallback rectangles.");
            }

            // 4. Grid (always init — needed for combat later even if starting in exploration)
            InitializeGrid();

            // 5. Phase-dependent initialization
            if (_startInExploration)
            {
                _currentPhase = GamePhase.Exploration;
                InitializeExploration();
            }
            else
            {
                _currentPhase = GamePhase.Combat;
                InitializeUnits();
                InitializeCombat();
            }

            Debug.Log($"[GameBootstrap] === Core initialization complete (Phase: {_currentPhase}) ===");
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

        private void InitializeExploration()
        {
            Debug.Log("[GameBootstrap] === Initializing Exploration Phase ===");

            if (_explorationLeader == null)
            {
                Debug.LogError("[GameBootstrap] Exploration leader not assigned! Falling back to combat.");
                _currentPhase = GamePhase.Combat;
                InitializeUnits();
                InitializeCombat();
                return;
            }

            // Create ExplorationController on CombatRoot (reuse the root object)
            var explorationRoot = _combatRoot != null ? _combatRoot : _systemsRoot;
            _explorationController = explorationRoot.GetComponent<ExplorationController>();
            if (_explorationController == null)
                _explorationController = explorationRoot.gameObject.AddComponent<ExplorationController>();

            // Get animator controller and default material from UnitSpawner
            var spawner = _systemsRoot != null ? _systemsRoot.GetComponentInChildren<UnitSpawner>() : null;
            RuntimeAnimatorController animController = null;
            Material defaultMaterial = null;
            if (spawner != null)
            {
                var animField = typeof(UnitSpawner).GetField("_defaultAnimator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animField != null)
                    animController = animField.GetValue(spawner) as RuntimeAnimatorController;

                var matField = typeof(UnitSpawner).GetField("_defaultMaterial",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (matField != null)
                    defaultMaterial = matField.GetValue(spawner) as Material;
            }

            _explorationController.Initialize(
                _explorationLeader,
                _explorationFollowers,
                _explorationSpawnPos,
                animController,
                defaultMaterial);

            // Spawn enemy NPCs at their combat grid positions
            SpawnExplorationEnemies();

            // Set up encounter trigger — transition to combat when player approaches enemies
            _explorationController.SetEncounterCallback(TransitionToCombat, 8f);

            // Focus camera on the exploration leader
            var camera = _cameraRoot != null ? _cameraRoot.GetComponentInChildren<TacticalCam>() : null;
            if (camera != null && _explorationController.Leader != null)
            {
                camera.FocusOnPoint(_explorationController.PartySpawnPosition);
                camera.SetFollowTarget(_explorationController.Leader.transform);
            }

            // Initialize exploration UI
            InitializeExplorationHUD();
            InitializeExplorationMinimap();

            Debug.Log("[GameBootstrap] Exploration phase initialized.");
        }

        /// <summary>
        /// Transition from exploration to combat.
        /// Despawns exploration party, spawns grid-based combat units, starts combat.
        /// </summary>
        public void TransitionToCombat()
        {
            if (_currentPhase != GamePhase.Exploration)
            {
                Debug.LogWarning("[GameBootstrap] Not in exploration phase, cannot transition to combat.");
                return;
            }

            Debug.Log("[GameBootstrap] === Transitioning to Combat ===");

            // 1. Capture exploration unit positions BEFORE despawning
            List<ExplorationController.ExplorationUnitInfo> explorationUnits = null;
            if (_explorationController != null)
                explorationUnits = _explorationController.GetAllUnitData();

            // 2. End exploration and despawn visuals
            if (_explorationController != null)
            {
                _explorationController.EndExploration();
                _explorationController.DespawnParty();
            }

            // 3. Clean up exploration UI
            if (_explorationHUD != null)
            {
                _explorationHUD.Cleanup();
                _explorationHUD = null;
            }
            if (_explorationMinimap != null)
            {
                _explorationMinimap.Cleanup();
                _explorationMinimap = null;
            }

            // 4. Build dynamic spawn data from captured exploration positions
            if (explorationUnits != null && explorationUnits.Count > 0)
                _combatSpawnOverrides = BuildCombatSpawnsFromExploration(explorationUnits);

            _currentPhase = GamePhase.Combat;

            // 5. Initialize combat systems using exploration-derived positions
            InitializeUnits();
            InitializeCombat();

            // 6. Reset camera zoom from exploration (5) to combat (12)
            var camera = _cameraRoot != null ? _cameraRoot.GetComponentInChildren<TacticalCam>() : null;
            if (camera != null)
                camera.SetZoom(12f, instant: false);

            Debug.Log("[GameBootstrap] Combat transition complete.");
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

            // Apply manual cover data
            var coverSetup = gridMap.GetComponent<CoverSetup>();
            if (coverSetup != null)
                coverSetup.Initialize(gridMap);

            var visualizer = gridMap.GetComponent<HexGridVisualizer>();
            if (visualizer != null)
            {
                visualizer.BuildCache();
                Debug.Log("[GameBootstrap] HexGridVisualizer cache built.");
            }

            // Cover visual indicators
            var coverVis = gridMap.GetComponent<CoverVisualizer>();
            if (coverVis == null)
                coverVis = gridMap.gameObject.AddComponent<CoverVisualizer>();
            coverVis.Initialize(gridMap);
            Debug.Log("[GameBootstrap] CoverVisualizer initialized.");

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
            // Use exploration-derived positions if available, otherwise fall back to fixed test spawns
            var spawns = _combatSpawnOverrides ?? _testSpawns;
            _combatSpawnOverrides = null; // Clear after use

            if (spawns == null || spawns.Length == 0)
            {
                Debug.Log("[GameBootstrap] No spawns configured.");
                return;
            }

            foreach (var spawn in spawns)
            {
                if (spawn.Definition == null)
                {
                    Debug.LogWarning("[GameBootstrap] Spawn has null definition, skipping.");
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

            // Face each team's units toward the opposing team's center
            FaceUnitsTowardEnemies(spawner, gridMap);
        }

        private void FaceUnitsTowardEnemies(UnitSpawner spawner, HexGridMap gridMap)
        {
            if (_registry == null) return;

            // Compute center position per team
            var team0Center = Vector3.zero;
            var team1Center = Vector3.zero;
            int team0Count = 0;
            int team1Count = 0;

            foreach (var unit in _registry.AllUnits)
            {
                Vector3 pos = gridMap.GetCellWorldPosition(unit.GridPosition);
                if (unit.TeamId == 0)
                {
                    team0Center += pos;
                    team0Count++;
                }
                else
                {
                    team1Center += pos;
                    team1Count++;
                }
            }

            if (team0Count == 0 || team1Count == 0) return;

            team0Center /= team0Count;
            team1Center /= team1Count;

            // Rotate each unit to face the opposing team's center
            foreach (var unit in _registry.AllUnits)
            {
                var brain = spawner.GetBrain(unit.UnitId);
                if (brain == null) continue;

                Vector3 target = unit.TeamId == 0 ? team1Center : team0Center;
                Vector3 direction = target - brain.transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.001f)
                {
                    brain.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        private void CreateTestOilPuddles(HexGridMap gridMap)
        {
            if (_combatController == null || _combatController.SurfaceSystem == null)
            {
                Debug.LogWarning("[GameBootstrap] Cannot create oil puddles: SurfaceSystem not initialized.");
                return;
            }

            // Find the Oil surface definition from the registered definitions
            var oilDef = _combatController.SurfaceSystem.GetDefinition(SurfaceType.Oil);
            if (oilDef == null)
            {
                Debug.LogWarning("[GameBootstrap] OilSurface definition not registered in SurfaceSystem.");
                return;
            }

            // Create oil puddles ON and around enemy units for Fire Bolt testing
            var enemyUnits = _registry.GetTeamUnits(1);
            if (enemyUnits.Count == 0) return;

            int puddleCount = 0;

            // Place oil on each enemy's cell
            foreach (var enemy in enemyUnits)
            {
                _combatController.SurfaceSystem.CreateSurface(enemy.GridPosition, oilDef, -1, gridMap);
                puddleCount++;
                Debug.Log($"[GameBootstrap] Created oil puddle under {enemy.Definition.UnitName} at {enemy.GridPosition}");
            }

            // Also place oil on a few adjacent cells around first enemy
            var neighbors = gridMap.GetNeighbors(enemyUnits[0].GridPosition);
            foreach (var neighborCell in neighbors)
            {
                if (puddleCount >= 5) break;
                if (neighborCell.Walkable && !neighborCell.IsOccupied)
                {
                    _combatController.SurfaceSystem.CreateSurface(neighborCell.Coord, oilDef, -1, gridMap);
                    puddleCount++;
                }
            }

            Debug.Log($"[GameBootstrap] Created {puddleCount} test oil puddles for Fire Bolt testing.");
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
            InitializeSurfaceVisualizer(gridMap);
            InitializeCombatHud();
            InitializeActionBar();
            InitializeCombatWorldUI(spawner);
            InitializeCombatVFX(spawner);
            InitializeCombatAudio();
            InitializeTurnOrderBar();
            InitializePartyPortraits(selectionMgr);
            InitializeCombatLog();
            InitializeResultsScreen();
            CreateTestOilPuddles(gridMap);
            _combatController.StartCombat();
            Debug.Log("[GameBootstrap] Combat system initialized and started.");
        }

        private void InitializeSurfaceVisualizer(HexGridMap gridMap)
        {
            if (_combatController == null || _combatController.SurfaceSystem == null)
                return;

            var surfaceVis = gridMap.GetComponent<SurfaceVisualizer>();
            if (surfaceVis == null)
                surfaceVis = gridMap.gameObject.AddComponent<SurfaceVisualizer>();

            surfaceVis.Initialize(gridMap, _combatController.SurfaceSystem);
            Debug.Log("[GameBootstrap] SurfaceVisualizer initialized.");
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

        private void InitializeActionBar()
        {
            if (_uiRoot == null || _combatController == null)
                return;

            var actionBar = _uiRoot.GetComponent<ActionBar>();
            if (actionBar == null)
                actionBar = _uiRoot.gameObject.AddComponent<ActionBar>();

            actionBar.Initialize(_combatController);
            actionBar.WireButtonCallbacks();
            Debug.Log("[GameBootstrap] Action Bar (DOS2 HUD) initialized.");
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

        private void InitializeCombatVFX(UnitSpawner spawner)
        {
            if (_combatRoot == null || _cameraRoot == null)
                return;

            // Camera shake — sits on the camera GO alongside TacticalCamera
            CameraShake cameraShake = null;
            var cam = _cameraRoot.GetComponentInChildren<TacticalCam>();
            if (cam != null)
            {
                cameraShake = cam.GetComponent<CameraShake>();
                if (cameraShake == null)
                    cameraShake = cam.gameObject.AddComponent<CameraShake>();
            }

            // VFX manager — sits on CombatRoot, orchestrates all combat VFX
            var vfxManager = _combatRoot.GetComponent<CombatVFXManager>();
            if (vfxManager == null)
                vfxManager = _combatRoot.gameObject.AddComponent<CombatVFXManager>();

            vfxManager.Initialize(spawner, cameraShake);
            Debug.Log("[GameBootstrap] Combat VFX system initialized.");
        }

        private void InitializeCombatAudio()
        {
            if (_combatRoot == null) return;

            var audioManager = _combatRoot.GetComponent<CombatAudioManager>();
            if (audioManager == null)
                audioManager = _combatRoot.gameObject.AddComponent<CombatAudioManager>();

            audioManager.Initialize(_audioConfig);
            Debug.Log("[GameBootstrap] Combat Audio system initialized.");
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

        private void InitializeCombatLog()
        {
            if (_uiRoot == null || _registry == null)
                return;

            var combatLog = _uiRoot.GetComponent<CombatLog>();
            if (combatLog == null)
                combatLog = _uiRoot.gameObject.AddComponent<CombatLog>();

            combatLog.Initialize(_registry);
            Debug.Log("[GameBootstrap] Combat Log initialized.");
        }

        private void InitializeResultsScreen()
        {
            if (_uiRoot == null || _registry == null || _combatController == null)
                return;

            var resultsScreen = _uiRoot.GetComponent<CombatResultsScreen>();
            if (resultsScreen == null)
                resultsScreen = _uiRoot.gameObject.AddComponent<CombatResultsScreen>();

            resultsScreen.Initialize(_registry, _combatController);
            Debug.Log("[GameBootstrap] Combat Results Screen initialized.");
        }

        private void SpawnExplorationEnemies()
        {
            if (_testSpawns == null || _testSpawns.Length == 0 || _explorationController == null)
                return;

            var gridMap = _gridRoot != null ? _gridRoot.GetComponentInChildren<HexGridMap>() : null;
            if (gridMap == null || !gridMap.IsInitialized)
            {
                Debug.LogWarning("[GameBootstrap] Grid not ready, cannot spawn exploration enemies.");
                return;
            }

            var enemyDefs = new List<UnitDefinition>();
            var enemyPositions = new List<Vector3>();

            foreach (var spawn in _testSpawns)
            {
                if (spawn.Definition == null || spawn.TeamId == 0) continue;

                var coord = new HexCoord(spawn.SpawnQ, spawn.SpawnR);
                if (gridMap.TryGetCell(coord, out HexCell cell))
                {
                    enemyDefs.Add(spawn.Definition);
                    enemyPositions.Add(gridMap.GetCellWorldPosition(coord));
                }
            }

            if (enemyDefs.Count > 0)
            {
                _explorationController.SpawnEnemies(enemyDefs.ToArray(), enemyPositions.ToArray());
                Debug.Log($"[GameBootstrap] Spawned {enemyDefs.Count} enemies for exploration.");
            }
        }

        /// <summary>
        /// Convert exploration unit positions to hex-grid SpawnData for combat.
        /// Uses nearest walkable cell to each unit's current world position.
        /// </summary>
        private SpawnData[] BuildCombatSpawnsFromExploration(
            List<ExplorationController.ExplorationUnitInfo> units)
        {
            var gridMap = _gridRoot != null ? _gridRoot.GetComponentInChildren<HexGridMap>() : null;
            if (gridMap == null || !gridMap.IsInitialized)
            {
                Debug.LogWarning("[GameBootstrap] Grid not ready, falling back to fixed spawns.");
                return null;
            }

            var spawns = new List<SpawnData>();
            var claimed = new HashSet<HexCoord>();

            foreach (var unit in units)
            {
                HexCoord coord = FindNearestWalkableCell(gridMap, unit.WorldPosition, claimed);
                spawns.Add(new SpawnData
                {
                    Definition = unit.Definition,
                    SpawnQ = coord.Q,
                    SpawnR = coord.R,
                    TeamId = unit.TeamId
                });
            }

            Debug.Log($"[GameBootstrap] Built {spawns.Count} dynamic combat spawns from exploration positions.");
            return spawns.ToArray();
        }

        /// <summary>
        /// Find the nearest walkable, unoccupied hex cell to a world position.
        /// Tracks previously claimed cells to avoid overlap during batch spawn.
        /// </summary>
        private static HexCoord FindNearestWalkableCell(
            HexGridMap gridMap, Vector3 worldPos, HashSet<HexCoord> claimed)
        {
            HexCoord nearest = gridMap.WorldToHex(worldPos);

            // Try the direct cell first
            if (IsCellAvailable(gridMap, nearest, claimed))
            {
                claimed.Add(nearest);
                return nearest;
            }

            // Search expanding rings around the nearest cell
            for (int ring = 1; ring <= 5; ring++)
            {
                var ringCoords = HexCoord.GetRing(nearest, ring);
                float bestDist = float.MaxValue;
                HexCoord bestCoord = nearest;
                bool found = false;

                foreach (var coord in ringCoords)
                {
                    if (!IsCellAvailable(gridMap, coord, claimed)) continue;

                    float dist = Vector3.Distance(worldPos, gridMap.GetCellWorldPosition(coord));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestCoord = coord;
                        found = true;
                    }
                }

                if (found)
                {
                    claimed.Add(bestCoord);
                    return bestCoord;
                }
            }

            // Fallback: use nearest even if not ideal
            Debug.LogWarning($"[GameBootstrap] No walkable cell found near {worldPos}, using nearest hex.");
            claimed.Add(nearest);
            return nearest;
        }

        private static bool IsCellAvailable(HexGridMap gridMap, HexCoord coord, HashSet<HexCoord> claimed)
        {
            return gridMap.TryGetCell(coord, out var cell)
                && cell.Walkable
                && !cell.IsOccupied
                && !claimed.Contains(coord);
        }

        private void InitializeExplorationHUD()
        {
            if (_uiRoot == null || _explorationLeader == null) return;

            _explorationHUD = _uiRoot.GetComponent<ExplorationHUD>();
            if (_explorationHUD == null)
                _explorationHUD = _uiRoot.gameObject.AddComponent<ExplorationHUD>();

            _explorationHUD.Initialize(_explorationLeader, _explorationFollowers);
            Debug.Log("[GameBootstrap] Exploration HUD initialized.");
        }

        private void InitializeExplorationMinimap()
        {
            if (_uiRoot == null || _explorationController == null || _explorationController.Leader == null)
                return;

            _explorationMinimap = _uiRoot.GetComponent<ExplorationMinimap>();
            if (_explorationMinimap == null)
                _explorationMinimap = _uiRoot.gameObject.AddComponent<ExplorationMinimap>();

            _explorationMinimap.Initialize(
                _explorationController.Leader.transform,
                _explorationController.GetFollowerTransforms(),
                _explorationController.GetEnemyTransforms());
            Debug.Log("[GameBootstrap] Exploration Minimap initialized.");
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
