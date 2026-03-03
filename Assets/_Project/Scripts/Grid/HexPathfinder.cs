using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// A* pathfinding on the hex grid.
    /// Accounts for walkability, occupancy, and height difference costs.
    /// </summary>
    public static class HexPathfinder
    {
        /// <summary>
        /// Cost configuration for pathfinding.
        /// </summary>
        public struct PathConfig
        {
            public float BaseCost;
            public float HeightPenaltyPerLevel;
            public int MaxHeightDiff;         // If > this, impassable unless hasRamp
            public bool IgnoreOccupants;      // For AI preview paths

            public static PathConfig Default => new PathConfig
            {
                BaseCost = 1f,
                HeightPenaltyPerLevel = 0.5f,
                MaxHeightDiff = 1,
                IgnoreOccupants = false
            };
        }

        /// <summary>
        /// Find the shortest path from start to goal using A*.
        /// Returns null if no path found.
        /// </summary>
        public static List<HexCoord> FindPath(
            HexGridMap grid,
            HexCoord start,
            HexCoord goal,
            PathConfig config)
        {
            if (!grid.TryGetCell(start, out _) || !grid.TryGetCell(goal, out HexCell goalCell))
                return null;

            if (!goalCell.Walkable)
                return null;

            var openSet = new MinHeap<PathNode>(64);
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var gScore = new Dictionary<HexCoord, float>();
            var closedSet = new HashSet<HexCoord>();

            gScore[start] = 0f;
            openSet.Push(new PathNode(start, 0f, Heuristic(start, goal)));

            var neighborBuffer = new List<HexCoord>(6);

            while (!openSet.IsEmpty)
            {
                PathNode current = openSet.Pop();

                if (current.Coord == goal)
                {
                    return ReconstructPath(cameFrom, start, goal);
                }

                if (closedSet.Contains(current.Coord))
                    continue;

                closedSet.Add(current.Coord);

                HexCell currentCell = grid.GetCell(current.Coord);
                if (currentCell == null) continue;

                current.Coord.GetNeighbors(neighborBuffer);

                foreach (var neighborCoord in neighborBuffer)
                {
                    if (closedSet.Contains(neighborCoord)) continue;
                    if (!grid.TryGetCell(neighborCoord, out HexCell neighborCell)) continue;
                    if (!neighborCell.Walkable) continue;

                    // Occupancy check
                    if (!config.IgnoreOccupants && neighborCell.IsOccupied && neighborCoord != goal)
                        continue;

                    // Height difference check
                    int heightDiff = Math.Abs(neighborCell.HeightLevel - currentCell.HeightLevel);
                    if (heightDiff > config.MaxHeightDiff && !neighborCell.HasRamp)
                        continue;

                    // Movement cost
                    float moveCost = config.BaseCost + heightDiff * config.HeightPenaltyPerLevel;
                    float tentativeG = gScore[current.Coord] + moveCost;

                    if (gScore.TryGetValue(neighborCoord, out float existingG) && tentativeG >= existingG)
                        continue;

                    gScore[neighborCoord] = tentativeG;
                    cameFrom[neighborCoord] = current.Coord;

                    float h = Heuristic(neighborCoord, goal);
                    openSet.Push(new PathNode(neighborCoord, tentativeG, h));
                }
            }

            // No path found
            return null;
        }

        /// <summary>
        /// Get all reachable cells within a given movement budget.
        /// Used for displaying movement range.
        /// </summary>
        public static Dictionary<HexCoord, float> GetReachable(
            HexGridMap grid,
            HexCoord start,
            float movementBudget,
            PathConfig config)
        {
            var costs = new Dictionary<HexCoord, float>();
            var openSet = new MinHeap<PathNode>(64);

            costs[start] = 0f;
            openSet.Push(new PathNode(start, 0f, 0f));

            var neighborBuffer = new List<HexCoord>(6);

            while (!openSet.IsEmpty)
            {
                PathNode current = openSet.Pop();

                if (!costs.TryGetValue(current.Coord, out float currentCost))
                    continue;

                if (current.GCost > currentCost)
                    continue; // Stale entry

                HexCell currentCell = grid.GetCell(current.Coord);
                if (currentCell == null) continue;

                current.Coord.GetNeighbors(neighborBuffer);

                foreach (var neighborCoord in neighborBuffer)
                {
                    if (!grid.TryGetCell(neighborCoord, out HexCell neighborCell)) continue;
                    if (!neighborCell.Walkable) continue;
                    if (!config.IgnoreOccupants && neighborCell.IsOccupied) continue;

                    int heightDiff = Math.Abs(neighborCell.HeightLevel - currentCell.HeightLevel);
                    if (heightDiff > config.MaxHeightDiff && !neighborCell.HasRamp)
                        continue;

                    float moveCost = config.BaseCost + heightDiff * config.HeightPenaltyPerLevel;
                    float newCost = currentCost + moveCost;

                    if (newCost > movementBudget) continue;

                    if (costs.TryGetValue(neighborCoord, out float existingCost) && newCost >= existingCost)
                        continue;

                    costs[neighborCoord] = newCost;
                    openSet.Push(new PathNode(neighborCoord, newCost, 0f));
                }
            }

            return costs;
        }

        // --- Helpers ---

        private static float Heuristic(HexCoord a, HexCoord b)
        {
            return a.DistanceTo(b);
        }

        private static List<HexCoord> ReconstructPath(
            Dictionary<HexCoord, HexCoord> cameFrom,
            HexCoord start,
            HexCoord goal)
        {
            var path = new List<HexCoord>();
            HexCoord current = goal;

            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Internal node for the priority queue.
        /// </summary>
        private readonly struct PathNode : IComparable<PathNode>
        {
            public readonly HexCoord Coord;
            public readonly float GCost;
            public readonly float FCost;

            public PathNode(HexCoord coord, float gCost, float hCost)
            {
                Coord = coord;
                GCost = gCost;
                FCost = gCost + hCost;
            }

            public int CompareTo(PathNode other)
            {
                return FCost.CompareTo(other.FCost);
            }
        }
    }
}
