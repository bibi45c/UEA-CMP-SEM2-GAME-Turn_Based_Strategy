using System.Collections;
using UnityEngine;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// Manages ambient/exploration BGM. Plays an optional intro clip then loops the main track.
    /// Provides FadeOut() for smooth transition when entering combat.
    /// Attach to CombatRoot; wired by GameBootstrap.
    /// </summary>
    public class ExplorationAudioManager : MonoBehaviour
    {
        private ExplorationAudioConfig _config;

        // Two music sources for crossfading (intro → loop)
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _musicAIsActive = true;

        private float _bgmMultiplier = 1f;

        private Coroutine _fadeCoroutine;
        private bool _isPlaying;

        public void Initialize(ExplorationAudioConfig config)
        {
            _config = config;

            if (_config == null)
            {
                Debug.LogWarning("[ExplorationAudio] No config assigned. Exploration music disabled.");
                return;
            }

            CreateAudioSources();
            StartMusic();
            Debug.Log("[ExplorationAudio] Initialized.");
        }

        private void CreateAudioSources()
        {
            _musicSourceA = CreateSource("ExplMusicA");
            _musicSourceB = CreateSource("ExplMusicB");
            _musicSourceA.volume = _config.MusicVolume;
            _musicSourceB.volume = 0f;
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject($"AudioSrc_{name}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D audio
            src.loop = false;
            return src;
        }

        private void StartMusic()
        {
            if (_config.ExplorationIntro != null)
            {
                // Play intro once, then crossfade to loop
                PlayClip(_musicSourceA, _config.ExplorationIntro, false);
                _musicAIsActive = true;
                _isPlaying = true;

                if (_config.ExplorationLoop != null)
                    StartCoroutine(CrossfadeToLoopAfterIntro(_config.ExplorationIntro.length));
            }
            else if (_config.ExplorationLoop != null)
            {
                // No intro, go straight to loop
                PlayClip(_musicSourceA, _config.ExplorationLoop, true);
                _musicAIsActive = true;
                _isPlaying = true;
            }
        }

        private void PlayClip(AudioSource source, AudioClip clip, bool loop)
        {
            source.clip   = clip;
            source.loop   = loop;
            source.volume = _config.MusicVolume * _bgmMultiplier;
            source.Play();
        }

        private IEnumerator CrossfadeToLoopAfterIntro(float introLength)
        {
            // Wait for most of the intro to play, then crossfade
            float crossfadeStart = Mathf.Max(0f, introLength - _config.CrossfadeDuration);
            yield return new WaitForSeconds(crossfadeStart);

            // Crossfade from intro (A) to loop (B)
            var outgoing = _musicSourceA;
            var incoming = _musicSourceB;
            _musicAIsActive = false;

            incoming.clip = _config.ExplorationLoop;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            float duration = _config.CrossfadeDuration;
            float startVol = outgoing.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(startVol, 0f, t);
                incoming.volume = Mathf.Lerp(0f, _config.MusicVolume * _bgmMultiplier, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            incoming.volume = _config.MusicVolume * _bgmMultiplier;
        }

        /// <summary>Apply BGM volume multiplier from the settings menu (0–1).</summary>
        public void ApplyVolume(float bgm, float sfx)
        {
            _bgmMultiplier = Mathf.Clamp01(bgm);
            var active = _musicAIsActive ? _musicSourceA : _musicSourceB;
            if (active != null && active.isPlaying && _config != null)
                active.volume = _config.MusicVolume * _bgmMultiplier;
        }

        /// <summary>
        /// Smoothly fade out exploration music. Call before transitioning to combat.
        /// </summary>
        public void FadeOut()
        {
            if (!_isPlaying || _config == null) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            float duration = _config.FadeOutDuration;
            float startVolA = _musicSourceA.volume;
            float startVolB = _musicSourceB.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _musicSourceA.volume = Mathf.Lerp(startVolA, 0f, t);
                _musicSourceB.volume = Mathf.Lerp(startVolB, 0f, t);
                yield return null;
            }

            _musicSourceA.Stop();
            _musicSourceB.Stop();
            _isPlaying = false;

            Debug.Log("[ExplorationAudio] Music faded out.");
        }

        /// <summary>
        /// Immediately stop all exploration music.
        /// </summary>
        public void StopImmediate()
        {
            StopAllCoroutines();
            if (_musicSourceA != null) { _musicSourceA.Stop(); _musicSourceA.volume = 0f; }
            if (_musicSourceB != null) { _musicSourceB.Stop(); _musicSourceB.volume = 0f; }
            _isPlaying = false;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
