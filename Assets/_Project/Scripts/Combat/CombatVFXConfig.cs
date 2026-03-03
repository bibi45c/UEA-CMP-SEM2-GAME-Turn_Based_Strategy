using System;
using UnityEngine;
using TurnBasedTactics.Abilities;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Tunable parameters for all combat visual effects.
    /// Serialized inline on CombatVFXManager.
    /// </summary>
    [Serializable]
    public class CombatVFXConfig
    {
        [Header("Camera Shake — Light (normal hit)")]
        public float LightShakeIntensity = 0.08f;
        public float LightShakeDuration = 0.2f;

        [Header("Camera Shake — Medium (critical hit)")]
        public float MediumShakeIntensity = 0.18f;
        public float MediumShakeDuration = 0.35f;

        [Header("Camera Shake — Heavy (kill)")]
        public float HeavyShakeIntensity = 0.35f;
        public float HeavyShakeDuration = 0.5f;

        [Header("Camera Shake — General")]
        public float ShakeFrequency = 18f;

        [Header("Hit Flash")]
        public float FlashDuration = 0.12f;
        public Color PhysicalFlashColor = Color.white;

        [Header("Impact Particles")]
        public int ImpactBurstCount = 14;
        public float ImpactParticleLifetime = 0.6f;
        public float ImpactParticleSpeed = 2.5f;
        public float ImpactParticleSize = 0.08f;

        [Header("Heal Particles")]
        public int HealParticleCount = 20;
        public float HealParticleLifetime = 1.2f;
        public float HealRiseSpeed = 1.5f;
        public Color HealPrimaryColor = new Color(0.3f, 1f, 0.4f, 1f);
        public Color HealSecondaryColor = new Color(1f, 0.9f, 0.5f, 1f);

        /// <summary>
        /// Map ElementType to a representative color for VFX.
        /// </summary>
        public static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => new Color(1f, 0.4f, 0.1f),
                ElementType.Ice => new Color(0.4f, 0.85f, 1f),
                ElementType.Lightning => new Color(1f, 0.95f, 0.3f),
                ElementType.Poison => new Color(0.3f, 0.85f, 0.2f),
                ElementType.Holy => new Color(1f, 0.9f, 0.5f),
                _ => Color.white
            };
        }
    }
}
