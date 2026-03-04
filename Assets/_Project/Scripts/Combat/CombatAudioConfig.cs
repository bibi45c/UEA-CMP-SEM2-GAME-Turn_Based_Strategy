using System;
using UnityEngine;
using TurnBasedTactics.Abilities;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// ScriptableObject holding all combat audio clip assignments and tuning parameters.
    /// Assign clips in the Inspector from ThirdParty/Audio packs.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAudioConfig", menuName = "TurnBasedTactics/Combat Audio Config")]
    public class CombatAudioConfig : ScriptableObject
    {
        // ── BGM ──────────────────────────────────────────────
        [Header("BGM — Combat Music")]
        [Tooltip("Short intro clip played once when combat starts")]
        public AudioClip CombatIntro;

        [Tooltip("Main combat loop (plays after intro)")]
        public AudioClip CombatLoop;

        [Tooltip("Victory music sting")]
        public AudioClip VictoryMusic;

        [Tooltip("Defeat music sting")]
        public AudioClip DefeatMusic;

        [Range(0f, 1f)] public float MusicVolume = 0.4f;
        [Tooltip("Seconds to crossfade between BGM tracks")]
        public float MusicCrossfadeDuration = 1.5f;

        // ── Attack SFX ───────────────────────────────────────
        [Header("SFX — Melee Attacks")]
        [Tooltip("Sword swing sounds (Action_RPG_SFX/Attack/Sword Swing*)")]
        public AudioClip[] MeleeSwingClips;

        [Tooltip("Hit impact sounds (Action_RPG_SFX/Combat/Combat*_Hit_Cut*)")]
        public AudioClip[] MeleeHitClips;

        [Header("SFX — Ranged Attacks")]
        [Tooltip("Arrow release (Action_RPG_SFX/Attack/Shooting*_Archer*)")]
        public AudioClip[] RangedAttackClips;

        // ── Elemental SFX ────────────────────────────────────
        [Header("SFX — Elemental Magic")]
        public AudioClip[] FireCastClips;
        public AudioClip[] IceCastClips;
        public AudioClip[] LightningCastClips;
        public AudioClip[] PoisonCastClips;
        public AudioClip[] HolyCastClips;

        // ── Ability SFX ──────────────────────────────────────
        [Header("SFX — Abilities")]
        [Tooltip("Healing spell sounds")]
        public AudioClip[] HealClips;

        [Tooltip("Buff/status applied sounds")]
        public AudioClip[] BuffAppliedClips;

        // ── Unit Lifecycle ───────────────────────────────────
        [Header("SFX — Unit Events")]
        [Tooltip("Unit death sound (Action_RPG_SFX/Effects/Defeated*)")]
        public AudioClip[] UnitDeathClips;

        [Tooltip("Footstep sounds for movement")]
        public AudioClip[] FootstepClips;
        [Tooltip("Seconds between footstep sounds during movement")]
        public float FootstepInterval = 0.35f;

        // ── UI SFX ───────────────────────────────────────────
        [Header("SFX — UI & Turn")]
        [Tooltip("Unit selected / turn started (player)")]
        public AudioClip[] UnitSelectClips;

        [Tooltip("Button confirm click")]
        public AudioClip[] UIConfirmClips;

        [Tooltip("Turn start chime")]
        public AudioClip[] TurnStartClips;

        // ── Volume & Pitch ───────────────────────────────────
        [Header("Tuning")]
        [Range(0f, 1f)] public float SFXVolume = 0.7f;
        [Range(0f, 0.3f)] public float PitchVariation = 0.08f;

        /// <summary>
        /// Select a random clip from an array. Returns null if array is empty/null.
        /// </summary>
        public AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Get element-specific cast clips for the given element.
        /// Falls back to generic melee hit if no element clips assigned.
        /// </summary>
        public AudioClip[] GetElementClips(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => FireCastClips,
                ElementType.Ice => IceCastClips,
                ElementType.Lightning => LightningCastClips,
                ElementType.Poison => PoisonCastClips,
                ElementType.Holy => HolyCastClips,
                _ => MeleeHitClips
            };
        }
    }
}
