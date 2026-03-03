using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// MonoBehaviour binding layer for the hex grid.
    /// Owns the cell dictionary, drives scanning, and provides public API.
    /// Attach to the GridSystem GameObject in the scene.
    /// </summary>
    public class HexGridMap : MonoBehaviour
    {
        [SerializeField] private HexGridConfig _config;

        private Dictionary<HexCoord, HexCell> _cells;
        private bool _isInitialized;

        // --- Public API ---

        public HexGridConfig Config => _config;
        public bool IsInitialized => _isInitialized;
        public int CellCount => _cells?.Count ?? 0;

        /// <summary>
        /// Initialize the grid: scan terrain and populate cells.
        /// Called by GameBootstrap or CombatSceneController.
        /// </summary>
        public void Initialize()
        {
            if (_config == null)
            {
                Debug.LogError("[HexGridMap] HexGridConfig is not assigned!");
                return;
            }

            _cells = HexGridScanner.Scan(_config);
            _isInitialized = true;

            Debug.Log($"[HexGridMap] Initialized with {_cells.Count} cells.");
        }

        /// <summary>
        /// Get a cell by its hex coordinate.
        /// </summary>
        public HexCell GetCell(HexCoord coord)
        {
            if (_cells != null && _cells.TryGetValue(coord, out HexCell cell))
                return cell;
            return null;
        }

        /// <summary>
        /// Try to get a cell. Returns false if cell doesn't exist.
        /// </summary>
        public bool TryGetCell(HexCoord coord, out HexCell cell)
        {
            cell = null;
            return _cells != null && _cells.TryGetValue(coord, out cell);
        }

        /// <summary>
        /// Get the world position for a cell (uses cell's stored WorldY).
        /// </summary>
        public Vector3 GetCellWorldPosition(HexCoord coord)
        {
            Vector3 flatPos = HexCoord.HexToWorld(coord, _config.HexOuterRadius, _config.GridOrigin);

            if (TryGetCell(coord, out HexCell cell))
            {
                flatPos.y = cell.WorldY;
            }

            return flatPos;
        }

        /// <summary>
        /// Convert a world position to the nearest hex coordinate.
        /// </summary>
        public HexCoord WorldToHex(Vector3 worldPos)
        {
            return HexCoord.WorldToHex(worldPos, _config.HexOuterRadius, _config.GridOrigin);
        }

        /// <summary>
        /// Get all walkable cells within a hex distance from center.
        /// Used for movement range display.
        /// </summary>
        public List<HexCell> GetWalkableCellsInRange(HexCoord center, int range)
        {
            var result = new List<HexCell>();
            var coords = HexCoord.GetRange(center, range);

            foreach (var coord in coords)
            {
                if (TryGetCell(coord, out HexCell cell) && cell.Walkable)
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all neighbors of a cell (only existing cells).
        /// </summary>
        public List<HexCell> GetNeighbors(HexCoord coord)
        {
            var result = new List<HexCell>(6);
            var neighborCoords = new List<HexCoord>(6);
            coord.GetNeighbors(neighborCoords);

            foreach (var nc in neighborCoords)
            {
                if (TryGetCell(nc, out HexCell cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        /// <summary>
        /// Set occupant on a cell. -1 clears occupancy.
        /// </summary>
        public void SetOccupant(HexCoord coord, int occupantId)
        {
            if (TryGetCell(coord, out HexCell cell))
            {
                cell.OccupantId = occupantId;
            }
        }

        /// <summary>
        /// Enumerate all cells. Use sparingly — prefer targeted lookups.
        /// </summary>
        public IEnumerable<KeyValuePair<HexCoord, HexCell>> AllCells =>
            _cells ?? new Dictionary<HexCoord, HexCell>();

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_isInitialized || _cells == null) return;

            foreach (var kv in _cells)
            {
                var cell = kv.Value;
                Vector3 pos = GetCellWorldPosition(kv.Key);
                pos.y += 0.05f; // Slight offset above terrain

                Gizmos.color = cell.Walkable
                    ? new Color(0f, 1f, 0.5f, 0.3f)
                    : new Color(1f, 0f, 0f, 0.3f);

                Gizmos.DrawWireSphere(pos, _config.HexOuterRadius * 0.3f);
            }
        }
#endif
    }
}
