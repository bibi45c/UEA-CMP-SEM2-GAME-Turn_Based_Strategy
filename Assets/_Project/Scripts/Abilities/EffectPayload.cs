using System;
using UnityEngine;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// One effect entry within an ability. Multiple effects can be stacked
    /// on a single AbilityDefinition (e.g., damage + apply status).
    /// </summary>
    [Serializable]
    public struct EffectPayload
    {
        public AbilityEffectType EffectType;

        [Tooltip("Flat base value before scaling (e.g., 4 for a heal with 4 + INT*0.5)")]
        public int BaseValue;

        [Tooltip("Which stat drives the scaling portion of this effect")]
        public ScalingStat ScalingStat;

        [Tooltip("Multiplier applied to the scaling stat (e.g., 0.5 means stat * 0.5)")]
        public float ScalingFactor;

        [Tooltip("StatusDefinition to apply when EffectType is ApplyStatus")]
        public StatusDefinition StatusToApply;

        [Tooltip("Legacy string ID (kept for backwards compatibility)")]
        public string StatusId;

        [Tooltip("SurfaceDefinition to create when EffectType is CreateSurface")]
        public SurfaceDefinition SurfaceToCreate;
    }
}
