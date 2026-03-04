using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Manages all active surfaces on the hex grid.
    /// Handles creation, removal, duration ticking, on-enter effects, and surface reactions.
    /// Plain C# class — owned by CombatSceneController.
    /// </summary>
    public class SurfaceSystem
    {
        private readonly Dictionary<HexCoord, SurfaceInstance> _activeSurfaces = new();
        private readonly Dictionary<SurfaceType, SurfaceDefinition> _definitionLookup = new();
        private readonly Dictionary<(SurfaceType existing, SurfaceType incoming), SurfaceReaction> _reactionTable = new();

        // Surfaces created during reaction processing (deferred to avoid collection modification)
        private readonly List<(HexCoord coord, SurfaceDefinition def, int sourceId)> _pendingCreations = new();

        public IReadOnlyDictionary<HexCoord, SurfaceInstance> ActiveSurfaces => _activeSurfaces;
        public bool IsDirty { get; private set; }

        public SurfaceSystem()
        {
            BuildReactionTable();
        }

        /// <summary>
        /// Register a SurfaceDefinition so the system can look up definitions by SurfaceType
        /// (needed for reaction-created surfaces).
        /// </summary>
        public void RegisterDefinition(SurfaceDefinition def)
        {
            if (def != null)
                _definitionLookup[def.SurfaceType] = def;
        }

        /// <summary>
        /// Look up a registered SurfaceDefinition by type.
        /// </summary>
        public SurfaceDefinition GetDefinition(SurfaceType type)
        {
            _definitionLookup.TryGetValue(type, out var def);
            return def;
        }

        /// <summary>
        /// Create a surface on a hex cell. Handles reactions with existing surfaces.
        /// </summary>
        public void CreateSurface(HexCoord coord, SurfaceDefinition def, int sourceUnitId, HexGridMap gridMap)
        {
            if (def == null || gridMap == null) return;

            var cell = gridMap.GetCell(coord);
            if (cell == null) return;

            // Check for reaction with existing surface
            if (_activeSurfaces.TryGetValue(coord, out var existing))
            {
                var reaction = GetReaction(existing.Definition.SurfaceType, def.SurfaceType);
                if (reaction.HasReaction)
                {
                    ProcessReaction(coord, reaction, sourceUnitId, gridMap);
                    return;
                }

                // No reaction — replace existing surface
                RemoveSurface(coord, gridMap);
            }

            // Place new surface
            var instance = new SurfaceInstance(def, coord, sourceUnitId);
            _activeSurfaces[coord] = instance;
            cell.Surface = def.SurfaceType;
            IsDirty = true;

            Debug.Log($"[SurfaceSystem] Created {def.DisplayName} at {coord} ({def.DefaultDuration} rounds)");

            // Check neighbors for chain reactions (e.g., Fire spreads to adjacent Oil)
            CheckNeighborReactions(coord, def, sourceUnitId, gridMap);

            // Process any pending creations from chain reactions
            ProcessPendingCreations(gridMap);
        }

        /// <summary>
        /// Remove a surface from a hex cell.
        /// </summary>
        public void RemoveSurface(HexCoord coord, HexGridMap gridMap)
        {
            if (!_activeSurfaces.Remove(coord))
                return;

            var cell = gridMap?.GetCell(coord);
            if (cell != null)
                cell.Surface = SurfaceType.None;

            IsDirty = true;
        }

        /// <summary>
        /// Get the surface instance at a given cell, or null.
        /// </summary>
        public SurfaceInstance GetSurface(HexCoord coord)
        {
            _activeSurfaces.TryGetValue(coord, out var instance);
            return instance;
        }

        /// <summary>
        /// Get the movement cost modifier for a cell's surface.
        /// </summary>
        public float GetMovementCostModifier(HexCoord coord)
        {
            if (_activeSurfaces.TryGetValue(coord, out var instance))
                return instance.Definition.MovementCostModifier;
            return 0f;
        }

        /// <summary>
        /// Process all surfaces at the end of a round: tick durations, remove expired.
        /// </summary>
        public void ProcessRoundEnd(HexGridMap gridMap)
        {
            var toRemove = new List<HexCoord>();

            foreach (var kvp in _activeSurfaces)
            {
                bool expired = kvp.Value.TickDuration();
                if (expired)
                {
                    Debug.Log($"[SurfaceSystem] {kvp.Value.Definition.DisplayName} expired at {kvp.Key}");
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var coord in toRemove)
            {
                RemoveSurface(coord, gridMap);
            }
        }

        /// <summary>
        /// Apply surface effects when a unit enters or starts its turn on a cell.
        /// Returns damage dealt (for event publishing).
        /// </summary>
        public SurfaceTickResult ApplyOnEnterEffects(
            UnitRuntime unit, HexCoord coord,
            StatusManager statusManager)
        {
            var result = new SurfaceTickResult();

            if (unit == null || unit.IsDead)
                return result;

            if (!_activeSurfaces.TryGetValue(coord, out var surface))
                return result;

            var def = surface.Definition;

            // Apply tick damage
            if (def.TickDamage > 0)
            {
                int damage = def.TickDamage;
                if (!def.TickIgnoresArmor)
                {
                    int armor = def.TickElement == Abilities.ElementType.None
                        ? unit.Stats.PhysicalArmor
                        : unit.Stats.MagicResistance;
                    damage = Mathf.Max(1, damage - armor);
                }

                unit.TakeDamage(damage);
                result.TotalDamage += damage;
                Debug.Log($"[SurfaceSystem] {def.DisplayName} deals {damage} damage to unit {unit.UnitId}");

                if (unit.IsDead)
                    result.DidKill = true;
            }

            // Apply on-enter status
            if (def.OnEnterStatus != null && statusManager != null && !unit.IsDead)
            {
                statusManager.ApplyStatus(def.OnEnterStatus, unit, surface.SourceUnitId);
                result.StatusApplied = def.OnEnterStatus.DisplayName;
            }

            return result;
        }

        /// <summary>
        /// Clear the dirty flag after the visualizer has rebuilt.
        /// </summary>
        public void ClearDirty()
        {
            IsDirty = false;
        }

        /// <summary>
        /// Remove all surfaces (e.g., combat end cleanup).
        /// </summary>
        public void ClearAll(HexGridMap gridMap)
        {
            var coords = new List<HexCoord>(_activeSurfaces.Keys);
            foreach (var coord in coords)
                RemoveSurface(coord, gridMap);
        }

        // ------------------------------------------------------------------
        // Reaction Table
        // ------------------------------------------------------------------

        private void BuildReactionTable()
        {
            // Fire + Oil → Fire (replaces oil with fire)
            _reactionTable[(SurfaceType.Oil, SurfaceType.Fire)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.Fire,
                SpreadToNeighbors = true
            };

            // Fire + Ice → Water
            _reactionTable[(SurfaceType.Ice, SurfaceType.Fire)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.Water
            };

            // Fire + Water → None (steam)
            _reactionTable[(SurfaceType.Water, SurfaceType.Fire)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.None
            };
            _reactionTable[(SurfaceType.Fire, SurfaceType.Water)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.None
            };

            // Fire + Poison → Fire (explosion, consumes poison)
            _reactionTable[(SurfaceType.Poison, SurfaceType.Fire)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.Fire,
                SpreadToNeighbors = true
            };

            // Electricity + Water → Electricity (spreads)
            _reactionTable[(SurfaceType.Water, SurfaceType.Electricity)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.Electricity,
                SpreadToNeighbors = true
            };

            // Ice + Fire → Water (reverse direction)
            _reactionTable[(SurfaceType.Fire, SurfaceType.Ice)] = new SurfaceReaction
            {
                ResultSurface = SurfaceType.Water
            };
        }

        private SurfaceReaction GetReaction(SurfaceType existing, SurfaceType incoming)
        {
            if (_reactionTable.TryGetValue((existing, incoming), out var reaction))
                return reaction;
            return default;
        }

        private void ProcessReaction(HexCoord coord, SurfaceReaction reaction, int sourceUnitId, HexGridMap gridMap)
        {
            // Remove existing surface
            RemoveSurface(coord, gridMap);

            if (reaction.ResultSurface == SurfaceType.None)
            {
                Debug.Log($"[SurfaceSystem] Reaction at {coord}: surfaces neutralized");
                return;
            }

            // Create result surface if we have a definition for it
            if (_definitionLookup.TryGetValue(reaction.ResultSurface, out var resultDef))
            {
                var instance = new SurfaceInstance(resultDef, coord, sourceUnitId);
                _activeSurfaces[coord] = instance;
                var cell = gridMap.GetCell(coord);
                if (cell != null) cell.Surface = resultDef.SurfaceType;
                IsDirty = true;

                Debug.Log($"[SurfaceSystem] Reaction at {coord}: created {resultDef.DisplayName}");

                if (reaction.SpreadToNeighbors)
                {
                    CheckNeighborReactions(coord, resultDef, sourceUnitId, gridMap);
                }
            }
        }

        private void CheckNeighborReactions(HexCoord center, SurfaceDefinition newSurface, int sourceUnitId, HexGridMap gridMap)
        {
            var neighbors = gridMap.GetNeighbors(center);
            foreach (var neighborCell in neighbors)
            {
                var neighborCoord = neighborCell.Coord;
                if (!_activeSurfaces.TryGetValue(neighborCoord, out var neighborSurface))
                    continue;

                var reaction = GetReaction(neighborSurface.Definition.SurfaceType, newSurface.SurfaceType);
                if (reaction.HasReaction && reaction.SpreadToNeighbors)
                {
                    // Defer creation to avoid modifying collection during iteration
                    if (_definitionLookup.TryGetValue(reaction.ResultSurface, out var resultDef))
                    {
                        _pendingCreations.Add((neighborCoord, resultDef, sourceUnitId));
                    }
                }
            }
        }

        private void ProcessPendingCreations(HexGridMap gridMap)
        {
            if (_pendingCreations.Count == 0) return;

            // Copy to avoid recursion issues
            var batch = new List<(HexCoord coord, SurfaceDefinition def, int sourceId)>(_pendingCreations);
            _pendingCreations.Clear();

            foreach (var (coord, def, sourceId) in batch)
            {
                // Remove old surface and place new one (no further chain reactions to keep it bounded)
                RemoveSurface(coord, gridMap);
                var instance = new SurfaceInstance(def, coord, sourceId);
                _activeSurfaces[coord] = instance;
                var cell = gridMap.GetCell(coord);
                if (cell != null) cell.Surface = def.SurfaceType;
                IsDirty = true;

                Debug.Log($"[SurfaceSystem] Chain reaction: {def.DisplayName} spread to {coord}");
            }
        }
    }

    /// <summary>
    /// Result of a surface reaction lookup.
    /// </summary>
    public struct SurfaceReaction
    {
        public SurfaceType ResultSurface;
        public bool SpreadToNeighbors;
        public bool HasReaction => ResultSurface != SurfaceType.None || SpreadToNeighbors;
    }

    /// <summary>
    /// Result of applying surface effects to a unit.
    /// </summary>
    public struct SurfaceTickResult
    {
        public int TotalDamage;
        public bool DidKill;
        public string StatusApplied;
    }
}
