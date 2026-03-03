using System;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Manual cover placement for Phase 1. Assigns CoverType to specific hex cells.
    /// Attach to GridRoot and configure entries in the Inspector.
    /// </summary>
    public class CoverSetup : MonoBehaviour
    {
        [SerializeField] private CoverEntry[] _coverEntries;

        /// <summary>
        /// Apply all configured cover entries to the grid.
        /// Call after HexGridMap.Initialize().
        /// </summary>
        public void Initialize(HexGridMap gridMap)
        {
            if (_coverEntries == null || _coverEntries.Length == 0 || gridMap == null)
                return;

            int applied = 0;
            foreach (var entry in _coverEntries)
            {
                var coord = new HexCoord(entry.Q, entry.R);
                var cell = gridMap.GetCell(coord);
                if (cell != null)
                {
                    cell.Cover = entry.CoverType;
                    applied++;
                }
                else
                {
                    Debug.LogWarning($"[CoverSetup] Cell ({entry.Q},{entry.R}) not found in grid.");
                }
            }

            Debug.Log($"[CoverSetup] Applied {applied} cover entries.");
        }
    }

    [Serializable]
    public struct CoverEntry
    {
        public int Q;
        public int R;
        public CoverType CoverType;
    }
}
