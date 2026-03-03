using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Manages all active status effects on all units.
    /// Call ProcessTurnStart() at the beginning of each unit's turn
    /// to tick DoTs/HoTs and expire finished statuses.
    /// Call RecalculateBuffs() after status changes to update UnitStats.
    /// </summary>
    public class StatusManager
    {
        // UnitId -> list of active status instances
        private readonly Dictionary<int, List<StatusInstance>> _activeStatuses = new();

        /// <summary>
        /// Apply a status effect to a unit.
        /// If non-stackable and already present, refreshes duration instead.
        /// </summary>
        public void ApplyStatus(StatusDefinition definition, UnitRuntime target, int sourceUnitId)
        {
            if (definition == null || target == null || target.IsDead)
                return;

            if (!_activeStatuses.TryGetValue(target.UnitId, out var list))
            {
                list = new List<StatusInstance>();
                _activeStatuses[target.UnitId] = list;
            }

            // Check for existing instance of the same status
            if (!definition.Stackable)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Definition == definition)
                    {
                        // Refresh: replace with new instance (resets duration)
                        list[i] = new StatusInstance(definition, sourceUnitId);
                        Debug.Log($"[StatusManager] Refreshed {definition.DisplayName} on unit {target.UnitId}");
                        RecalculateBuffs(target);
                        return;
                    }
                }
            }

            list.Add(new StatusInstance(definition, sourceUnitId));
            Debug.Log($"[StatusManager] Applied {definition.DisplayName} to unit {target.UnitId} ({definition.Duration} turns)");
            RecalculateBuffs(target);
        }

        /// <summary>
        /// Remove all instances of a specific status from a unit.
        /// </summary>
        public void RemoveStatus(StatusDefinition definition, UnitRuntime target)
        {
            if (definition == null || target == null)
                return;

            if (!_activeStatuses.TryGetValue(target.UnitId, out var list))
                return;

            list.RemoveAll(s => s.Definition == definition);
            RecalculateBuffs(target);
        }

        /// <summary>
        /// Process all status effects at the start of a unit's turn.
        /// Returns total tick damage dealt (for event publishing).
        /// </summary>
        public StatusTickResult ProcessTurnStart(UnitRuntime unit)
        {
            var result = new StatusTickResult();

            if (unit == null || unit.IsDead)
                return result;

            if (!_activeStatuses.TryGetValue(unit.UnitId, out var list) || list.Count == 0)
                return result;

            // Process each status: apply tick effects, then decrement duration
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var instance = list[i];
                var def = instance.Definition;

                // Apply tick damage/healing
                if (def.TickDamage != 0)
                {
                    if (def.TickDamage > 0)
                    {
                        // Damage
                        int damage = def.TickDamage;
                        if (!def.TickIgnoresArmor)
                        {
                            int armor = def.TickElement == ElementType.None
                                ? unit.Stats.PhysicalArmor
                                : unit.Stats.MagicResistance;
                            damage = Mathf.Max(1, damage - armor);
                        }

                        unit.TakeDamage(damage);
                        result.TotalDamage += damage;
                        Debug.Log($"[StatusManager] {def.DisplayName} deals {damage} tick damage to unit {unit.UnitId}");

                        if (unit.IsDead)
                        {
                            result.DidKill = true;
                            break; // Stop processing further statuses
                        }
                    }
                    else
                    {
                        // Heal (negative tick damage = healing)
                        int healAmount = -def.TickDamage;
                        int prevHp = unit.CurrentHP;
                        unit.Heal(healAmount);
                        result.TotalHealing += unit.CurrentHP - prevHp;
                        Debug.Log($"[StatusManager] {def.DisplayName} heals {healAmount} on unit {unit.UnitId}");
                    }
                }

                // Tick duration
                bool expired = instance.TickDuration();
                if (expired)
                {
                    Debug.Log($"[StatusManager] {def.DisplayName} expired on unit {unit.UnitId}");
                    list.RemoveAt(i);
                }
            }

            // Recalculate buffs after any changes
            if (result.TotalDamage > 0 || result.TotalHealing > 0 || list.Count != _activeStatuses[unit.UnitId].Count)
                RecalculateBuffs(unit);

            return result;
        }

        /// <summary>
        /// Get all active statuses on a unit.
        /// </summary>
        public IReadOnlyList<StatusInstance> GetStatuses(int unitId)
        {
            if (_activeStatuses.TryGetValue(unitId, out var list))
                return list;
            return System.Array.Empty<StatusInstance>();
        }

        /// <summary>
        /// Check if a unit has a specific status.
        /// </summary>
        public bool HasStatus(int unitId, StatusDefinition definition)
        {
            if (!_activeStatuses.TryGetValue(unitId, out var list))
                return false;

            foreach (var s in list)
            {
                if (s.Definition == definition)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any active status prevents movement for this unit.
        /// </summary>
        public bool IsMovementPrevented(int unitId)
        {
            if (!_activeStatuses.TryGetValue(unitId, out var list))
                return false;

            foreach (var s in list)
            {
                if (s.Definition.PreventsMovement)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any active status prevents actions for this unit.
        /// </summary>
        public bool IsActionPrevented(int unitId)
        {
            if (!_activeStatuses.TryGetValue(unitId, out var list))
                return false;

            foreach (var s in list)
            {
                if (s.Definition.PreventsActions)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove all statuses from a unit (e.g., on death).
        /// </summary>
        public void ClearAllStatuses(int unitId)
        {
            _activeStatuses.Remove(unitId);
        }

        /// <summary>
        /// Recalculate buff bonuses on UnitStats from all active statuses.
        /// </summary>
        private void RecalculateBuffs(UnitRuntime unit)
        {
            if (unit == null) return;

            int str = 0, fin = 0, intel = 0, con = 0, wits = 0, move = 0;

            if (_activeStatuses.TryGetValue(unit.UnitId, out var list))
            {
                foreach (var instance in list)
                {
                    var def = instance.Definition;
                    str += def.StrengthMod;
                    fin += def.FinesseMod;
                    intel += def.IntelligenceMod;
                    con += def.ConstitutionMod;
                    wits += def.WitsMod;
                    move += def.MovementMod;
                }
            }

            unit.Stats.SetBuffBonuses(str, fin, intel, con, wits, move);
        }
    }

    /// <summary>
    /// Result of processing status effects at turn start.
    /// </summary>
    public struct StatusTickResult
    {
        public int TotalDamage;
        public int TotalHealing;
        public bool DidKill;
    }
}
