using UnityEngine;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// Scene entry point. Initializes core services and wires up subsystems.
    /// Attach to the SceneContext GameObject in every gameplay scene.
    ///
    /// Initialization order:
    ///   1. GameSession (create or reuse)
    ///   2. EventBus (clear stale handlers)
    ///   3. Find scene root references
    ///   4. Future: Grid, Combat, Camera, UI subsystem init
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

            // 1. Session: create new or reuse existing
            if (GameSession.Current == null)
            {
                GameSession.Create();
                Debug.Log("[GameBootstrap] New GameSession created.");
            }
            else
            {
                Debug.Log("[GameBootstrap] Reusing existing GameSession.");
            }

            // 2. EventBus: clear stale handlers from previous scene
            EventBus.Clear();
            Debug.Log("[GameBootstrap] EventBus cleared.");

            // 3. Validate scene root references
            ValidateRoots();

            // 4. Future subsystem initialization hooks
            // InitializeGrid();
            // InitializeCombat();
            // InitializeCamera();
            // InitializeUI();

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
