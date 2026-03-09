using UnityEngine;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// ScriptableObject holding exploration BGM clip assignments and volume settings.
    /// Assign clips from ThirdParty/Audio/Big Fantasy RPG Music Bundle/Exploration-Dungeons/.
    /// </summary>
    [CreateAssetMenu(fileName = "ExplorationAudioConfig", menuName = "TurnBasedTactics/Exploration Audio Config")]
    public class ExplorationAudioConfig : ScriptableObject
    {
        [Header("BGM — Exploration Music")]
        [Tooltip("Short intro clip played once when exploration starts")]
        public AudioClip ExplorationIntro;

        [Tooltip("Main exploration loop (plays after intro, loops indefinitely)")]
        public AudioClip ExplorationLoop;

        [Header("Volume & Crossfade")]
        [Range(0f, 1f)] public float MusicVolume = 0.35f;

        [Tooltip("Seconds to crossfade between intro and loop")]
        public float CrossfadeDuration = 2f;

        [Tooltip("Seconds to fade out when transitioning to combat")]
        public float FadeOutDuration = 1.5f;
    }
}
