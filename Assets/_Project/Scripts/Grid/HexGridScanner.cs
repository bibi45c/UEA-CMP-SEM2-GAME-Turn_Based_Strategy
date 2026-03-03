using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Scans terrain geometry via raycasts and populates HexCell data.
    /// Uses RaycastAll + surface normal filtering to find actual walkable floor.
    /// Dynamically discretizes height levels from each map's min/max Y.
    /// </summary>
    public static class HexGridScanner
    {
        // Minimum upward component of surface normal to count as "floor"
        // 0.5 ≈ 60° from horizontal — accepts stairs, rejects walls
        private const float MinFloorNormalY = 0.5f;

        /// <summary>
        /// Scan the terrain and create a dictionary of HexCells.
        /// Two-pass approach:
        ///   Pass 1 — RaycastAll on every hex, filter by surface normal,
        ///            collect floor hits, compute median floor Y.
        ///   Pass 2 — For each hex, pick the best floor hit and discretize.
        /// </summary>
        public static Dictionary<HexCoord, HexCell> Scan(HexGridConfig config)
        {
            var cells = new Dictionary<HexCoord, HexCell>();

            float outerRadius = config.HexOuterRadius;
            Vector3 origin = config.GridOrigin;
            float startY = config.ScanStartY;
            float maxDist = config.ScanMaxDistance;
            LayerMask mask = config.WalkableLayer;

            // ── Pass 1: RaycastAll, filter by normal, collect floor candidates ──

            // Store filtered floor hits per hex (only hits with upward normal)
            var floorHitsPerHex = new Dictionary<HexCoord, List<float>>();
            var allFloorYValues = new List<float>();

            for (int q = 0; q < config.GridWidth; q++)
            {
                for (int r = 0; r < config.GridHeight; r++)
                {
                    var coord = new HexCoord(q, r);
                    Vector3 worldPos = HexCoord.HexToWorld(coord, outerRadius, origin);
                    var rayOrigin = new Vector3(worldPos.x, startY, worldPos.z);

                    RaycastHit[] hits = Physics.RaycastAll(
                        rayOrigin, Vector3.down, maxDist, mask);

                    if (hits.Length > 0)
                    {
                        // Only keep hits where surface faces upward (floor-like)
                        var floorYs = new List<float>();
                        foreach (var hit in hits)
                        {
                            if (hit.normal.y >= MinFloorNormalY)
                            {
                                floorYs.Add(hit.point.y);
                            }
                        }

                        if (floorYs.Count > 0)
                        {
                            floorYs.Sort(); // ascending
                            floorHitsPerHex[coord] = floorYs;
                            // Use lowest floor-normal hit as floor candidate
                            allFloorYValues.Add(floorYs[0]);
                        }
                        else
                        {
                            // All hits were walls/steep — no walkable floor here
                            floorHitsPerHex[coord] = null;
                        }
                    }
                    else
                    {
                        floorHitsPerHex[coord] = null;
                    }
                }
            }

            // Compute median floor Y
            float medianFloorY = 0f;
            if (allFloorYValues.Count > 0)
            {
                allFloorYValues.Sort();
                medianFloorY = allFloorYValues[allFloorYValues.Count / 2];
            }

            // Compute IQR to determine "reasonable floor range"
            float q1Floor = allFloorYValues.Count > 0
                ? allFloorYValues[allFloorYValues.Count / 4]
                : medianFloorY;
            float q3Floor = allFloorYValues.Count > 0
                ? allFloorYValues[allFloorYValues.Count * 3 / 4]
                : medianFloorY;
            float iqr = q3Floor - q1Floor;
            // Generous bounds: median ± 3*IQR (or at least ±5m)
            float floorBandHalf = Mathf.Max(iqr * 3f, 5f);
            float floorMinY = medianFloorY - floorBandHalf;
            float floorMaxY = medianFloorY + floorBandHalf;

            Debug.Log($"[HexGridScanner] Pass 1: {allFloorYValues.Count} cells with floor hits. " +
                      $"Median floor Y: {medianFloorY:F1}, " +
                      $"Floor band: [{floorMinY:F1}, {floorMaxY:F1}]");

            // ── Pass 2: Select best floor hit per hex ──
            var rawData = new List<(HexCoord coord, float worldY)>();
            float globalMinY = float.MaxValue;
            float globalMaxY = float.MinValue;

            foreach (var kv in floorHitsPerHex)
            {
                var coord = kv.Key;
                var floorYs = kv.Value;

                if (floorYs == null || floorYs.Count == 0)
                {
                    cells[coord] = new HexCell(coord)
                    {
                        Walkable = false,
                        WorldY = startY - maxDist,
                        HeightLevel = 0
                    };
                    continue;
                }

                // Pick the lowest Y within the reasonable floor band
                float bestY = float.MaxValue;
                foreach (float y in floorYs)
                {
                    if (y >= floorMinY && y <= floorMaxY)
                    {
                        bestY = y; // sorted ascending, first valid = lowest
                        break;
                    }
                }

                // If none within band, check if any floor-normal hit is close
                if (bestY == float.MaxValue)
                {
                    // All floor-normal hits are outside band → likely obstacle top
                    // Mark as unwalkable
                    cells[coord] = new HexCell(coord)
                    {
                        Walkable = false,
                        WorldY = floorYs[0],
                        HeightLevel = 0
                    };
                    continue;
                }

                rawData.Add((coord, bestY));
                if (bestY < globalMinY) globalMinY = bestY;
                if (bestY > globalMaxY) globalMaxY = bestY;
            }

            // ── Discretize heights ──
            int heightLevels = config.HeightLevels;
            float yRange = globalMaxY - globalMinY;
            if (yRange < 0.01f) yRange = 1f;
            float levelStep = yRange / heightLevels;

            foreach (var (coord, worldY) in rawData)
            {
                float normalizedY = (worldY - globalMinY) / yRange;
                int level = Mathf.FloorToInt(normalizedY * heightLevels);
                level = Mathf.Clamp(level, 0, heightLevels - 1);

                var cell = new HexCell(coord)
                {
                    Walkable = true,
                    WorldY = worldY,
                    HeightLevel = level
                };

                // Slope check
                if (IsSlopeTooSteep(coord, worldY, config, medianFloorY))
                {
                    cell.Walkable = false;
                }

                cells[coord] = cell;
            }

            Debug.Log($"[HexGridScanner] Scanned {cells.Count} cells. " +
                      $"Walkable: {CountWalkable(cells)}. " +
                      $"Y range: [{globalMinY:F1}, {globalMaxY:F1}], " +
                      $"Level step: {levelStep:F2}m, Levels: {heightLevels}");

            return cells;
        }

        /// <summary>
        /// Find the best floor Y at a world position using RaycastAll + normal filter.
        /// </summary>
        private static float FindFloorY(
            Vector3 worldXZ, HexGridConfig config, float medianFloorY, float floorBandHalf)
        {
            var rayOrigin = new Vector3(worldXZ.x, config.ScanStartY, worldXZ.z);
            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin, Vector3.down, config.ScanMaxDistance, config.WalkableLayer);

            if (hits.Length == 0) return float.NaN;

            float floorMinY = medianFloorY - floorBandHalf;
            float floorMaxY = medianFloorY + floorBandHalf;

            // Sort by Y ascending
            System.Array.Sort(hits, (a, b) => a.point.y.CompareTo(b.point.y));

            // Prefer upward-facing hits within floor band
            foreach (var hit in hits)
            {
                if (hit.normal.y >= MinFloorNormalY &&
                    hit.point.y >= floorMinY &&
                    hit.point.y <= floorMaxY)
                {
                    return hit.point.y;
                }
            }

            // Fallback: any upward-facing hit
            foreach (var hit in hits)
            {
                if (hit.normal.y >= MinFloorNormalY)
                {
                    return hit.point.y;
                }
            }

            return float.NaN;
        }

        /// <summary>
        /// Check if terrain under a hex is too steep.
        /// </summary>
        private static bool IsSlopeTooSteep(
            HexCoord coord, float centerY, HexGridConfig config, float medianFloorY)
        {
            float outerRadius = config.HexOuterRadius;
            Vector3 origin = config.GridOrigin;

            Vector3 center = HexCoord.HexToWorld(coord, outerRadius, origin);
            float sampleOffset = outerRadius * 0.5f;
            float maxAllowedSlope = outerRadius * 2.5f; // Generous threshold

            float floorBandHalf = 10f;

            Vector3[] offsets =
            {
                new Vector3(sampleOffset, 0f, 0f),
                new Vector3(-sampleOffset * 0.5f, 0f, sampleOffset * 0.866f),
                new Vector3(-sampleOffset * 0.5f, 0f, -sampleOffset * 0.866f)
            };

            foreach (var off in offsets)
            {
                var samplePos = center + off;
                float sampleY = FindFloorY(samplePos, config, medianFloorY, floorBandHalf);

                if (!float.IsNaN(sampleY))
                {
                    if (Mathf.Abs(sampleY - centerY) > maxAllowedSlope)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CountWalkable(Dictionary<HexCoord, HexCell> cells)
        {
            int count = 0;
            foreach (var kv in cells)
            {
                if (kv.Value.Walkable) count++;
            }
            return count;
        }
    }
}
