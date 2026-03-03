using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Per-map configuration for the hex grid.
    /// Each combat map gets its own HexGridConfig asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHexGridConfig", menuName = "TurnBasedTactics/Hex Grid Config")]
    public class HexGridConfig : ScriptableObject
    {
        [Header("Hex Geometry")]
        [Tooltip("Outer radius (center to vertex) in meters")]
        [SerializeField] private float _hexOuterRadius = 0.75f;

        [Header("Grid Bounds")]
        [Tooltip("World-space origin of the grid (bottom-left corner)")]
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;

        [Tooltip("Number of hex columns (Q axis)")]
        [SerializeField] private int _gridWidth = 20;

        [Tooltip("Number of hex rows (R axis)")]
        [SerializeField] private int _gridHeight = 20;

        [Header("Height Scanning")]
        [Tooltip("World Y to start raycasts from (above highest terrain)")]
        [SerializeField] private float _scanStartY = 50f;

        [Tooltip("Maximum raycast distance downward")]
        [SerializeField] private float _scanMaxDistance = 100f;

        [Tooltip("Layer mask for walkable terrain")]
        [SerializeField] private LayerMask _walkableLayer = ~0;

        [Tooltip("Number of discrete height levels to divide the vertical range into")]
        [SerializeField] private int _heightLevels = 5;

        [Header("Display")]
        [Tooltip("Show the hex grid overlay by default")]
        [SerializeField] private bool _showGridByDefault = true;

        [Tooltip("Color for the hex wireframe overlay")]
        [SerializeField] private Color _gridLineColor = new Color(0f, 1f, 0.6f, 0.4f);

        [Tooltip("Color for unwalkable cells")]
        [SerializeField] private Color _unwalkableColor = new Color(1f, 0f, 0f, 0.3f);

        [Tooltip("Line width for the hex wireframe")]
        [SerializeField] private float _gridLineWidth = 0.02f;

        // --- Public API (read-only) ---

        public float HexOuterRadius => _hexOuterRadius;
        public float HexInnerRadius => _hexOuterRadius * 0.866025f; // sqrt(3)/2
        public Vector3 GridOrigin => _gridOrigin;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float ScanStartY => _scanStartY;
        public float ScanMaxDistance => _scanMaxDistance;
        public LayerMask WalkableLayer => _walkableLayer;
        public int HeightLevels => _heightLevels;
        public bool ShowGridByDefault => _showGridByDefault;
        public Color GridLineColor => _gridLineColor;
        public Color UnwalkableColor => _unwalkableColor;
        public float GridLineWidth => _gridLineWidth;

        /// <summary>
        /// Validate config values in the editor.
        /// </summary>
        private void OnValidate()
        {
            _hexOuterRadius = Mathf.Max(0.1f, _hexOuterRadius);
            _gridWidth = Mathf.Max(1, _gridWidth);
            _gridHeight = Mathf.Max(1, _gridHeight);
            _heightLevels = Mathf.Clamp(_heightLevels, 1, 20);
            _gridLineWidth = Mathf.Max(0.005f, _gridLineWidth);
        }
    }
}
