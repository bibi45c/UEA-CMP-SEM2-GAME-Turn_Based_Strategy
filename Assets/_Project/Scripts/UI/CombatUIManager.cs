using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Manages combat world-space UI: HP bars above units and floating damage/heal text.
    /// Subscribes to EventBus events for damage and healing.
    /// MonoBehaviour for scene lifecycle.
    /// </summary>
    public class CombatUIManager : MonoBehaviour
    {
        private UnitRegistry _registry;
        private UnitSpawner _spawner;
        private readonly Dictionary<int, UnitWorldUI> _hpBars = new();

        private static readonly Color DamageColor = new Color(1f, 0.3f, 0.2f, 1f);
        private static readonly Color CritColor = new Color(1f, 0.85f, 0f, 1f);
        private static readonly Color HealColor = new Color(0.3f, 1f, 0.4f, 1f);

        public void Initialize(UnitRegistry registry, UnitSpawner spawner)
        {
            _registry = registry;
            _spawner = spawner;

            // Create HP bars for all existing units
            foreach (var unit in registry.AllUnits)
            {
                CreateHPBar(unit);
            }

            // Subscribe to events
            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);

            Debug.Log("[CombatUIManager] Initialized with HP bars and floating text system.");
        }

        private void CreateHPBar(UnitRuntime unit)
        {
            if (_hpBars.ContainsKey(unit.UnitId))
                return;

            var brain = _spawner.GetBrain(unit.UnitId);
            if (brain == null)
                return;

            var hpBarGO = new GameObject($"HPBar_{unit.Definition.UnitName}");
            hpBarGO.transform.SetParent(brain.transform, false);

            var worldUI = hpBarGO.AddComponent<UnitWorldUI>();
            worldUI.Initialize(unit.UnitId, unit.TeamId, unit.Definition.UnitName);
            worldUI.UpdateHP((float)unit.CurrentHP / unit.Stats.MaxHP);

            _hpBars[unit.UnitId] = worldUI;
        }

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            // Update HP bar
            var target = _registry.GetUnit(evt.TargetUnitId);
            if (target != null && _hpBars.TryGetValue(evt.TargetUnitId, out var hpBar))
            {
                hpBar.UpdateHP((float)target.CurrentHP / target.Stats.MaxHP);
            }

            // Spawn floating damage text
            SpawnFloatingText(
                evt.TargetUnitId,
                $"-{evt.DamageAmount}",
                evt.WasCritical ? CritColor : DamageColor,
                evt.WasCritical);
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            // Update HP bar
            var target = _registry.GetUnit(evt.TargetUnitId);
            if (target != null && _hpBars.TryGetValue(evt.TargetUnitId, out var hpBar))
            {
                hpBar.UpdateHP((float)target.CurrentHP / target.Stats.MaxHP);
            }

            // Spawn floating heal text
            SpawnFloatingText(
                evt.TargetUnitId,
                $"+{evt.HealAmount}",
                HealColor,
                false);
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            // Remove HP bar
            if (_hpBars.TryGetValue(evt.UnitId, out var hpBar))
            {
                if (hpBar != null)
                    Destroy(hpBar.gameObject);
                _hpBars.Remove(evt.UnitId);
            }
        }

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            var unit = _registry.GetUnit(evt.UnitId);
            if (unit != null)
                CreateHPBar(unit);
        }

        private void SpawnFloatingText(int unitId, string message, Color color, bool isCritical)
        {
            var brain = _spawner.GetBrain(unitId);
            if (brain == null)
                return;

            Vector3 spawnPos = brain.transform.position + new Vector3(0f, 2.2f, 0f);

            var textGO = new GameObject("FloatingText");
            textGO.transform.position = spawnPos;

            var floater = textGO.AddComponent<FloatingDamageText>();
            floater.Initialize(message, color, isCritical);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
        }
    }
}
