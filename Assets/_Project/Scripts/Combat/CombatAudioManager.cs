using System.Collections;
using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Subscribes to combat EventBus events and plays appropriate audio.
    /// Three-layer audio: Music (BGM), SFX (combat effects), UI (interface).
    /// Attach to CombatRoot; wired by GameBootstrap.
    /// </summary>
    public class CombatAudioManager : MonoBehaviour
    {
        [SerializeField] private CombatAudioConfig _config;

        // Two music sources for crossfading
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _musicAIsActive = true;

        // Pooled SFX sources for polyphony
        private const int SFXPoolSize = 8;
        private AudioSource[] _sfxPool;
        private int _sfxPoolIndex;

        // Movement footstep tracking
        private Coroutine _footstepCoroutine;
        private int _movingUnitId = -1;

        public void Initialize(CombatAudioConfig config)
        {
            if (config != null) _config = config;

            if (_config == null)
            {
                Debug.LogWarning("[CombatAudioManager] No CombatAudioConfig assigned. Audio disabled.");
                return;
            }

            CreateAudioSources();
            SubscribeEvents();
            Debug.Log("[CombatAudioManager] Initialized.");
        }

        private void CreateAudioSources()
        {
            // Music sources (two for crossfade)
            _musicSourceA = CreateSource("MusicA", true);
            _musicSourceB = CreateSource("MusicB", true);
            _musicSourceA.volume = _config.MusicVolume;
            _musicSourceB.volume = 0f;

            // SFX pool
            _sfxPool = new AudioSource[SFXPoolSize];
            for (int i = 0; i < SFXPoolSize; i++)
                _sfxPool[i] = CreateSource($"SFX_{i}", false);
        }

        private AudioSource CreateSource(string name, bool isMusic)
        {
            var go = new GameObject($"AudioSrc_{name}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D audio
            if (isMusic) src.loop = false;
            return src;
        }

        // ── Event Subscriptions ──────────────────────────────

        private void SubscribeEvents()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
            EventBus.Subscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Subscribe<StatusAppliedEvent>(OnStatusApplied);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
            EventBus.Unsubscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Unsubscribe<StatusAppliedEvent>(OnStatusApplied);
        }

        // ── BGM ──────────────────────────────────────────────

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            if (_config.CombatIntro != null)
            {
                PlayMusic(_config.CombatIntro, false);
                // After intro ends, start the loop
                if (_config.CombatLoop != null)
                    StartCoroutine(PlayLoopAfterDelay(_config.CombatIntro.length));
            }
            else if (_config.CombatLoop != null)
            {
                PlayMusic(_config.CombatLoop, true);
            }
        }

        private IEnumerator PlayLoopAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayMusic(_config.CombatLoop, true);
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            StopFootsteps();

            // Crossfade to victory or defeat music
            bool playerWon = evt.WinningTeamId == 0;
            var clip = playerWon ? _config.VictoryMusic : _config.DefeatMusic;

            if (clip != null)
                CrossfadeToMusic(clip, false);
            else
                FadeOutMusic();
        }

        private void PlayMusic(AudioClip clip, bool loop)
        {
            var active = _musicAIsActive ? _musicSourceA : _musicSourceB;
            active.clip = clip;
            active.loop = loop;
            active.volume = _config.MusicVolume;
            active.Play();
        }

        private void CrossfadeToMusic(AudioClip newClip, bool loop)
        {
            StartCoroutine(CrossfadeCoroutine(newClip, loop));
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newClip, bool loop)
        {
            var outgoing = _musicAIsActive ? _musicSourceA : _musicSourceB;
            var incoming = _musicAIsActive ? _musicSourceB : _musicSourceA;
            _musicAIsActive = !_musicAIsActive;

            incoming.clip = newClip;
            incoming.loop = loop;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            float duration = _config.MusicCrossfadeDuration;
            float startVolume = outgoing.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(startVolume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, _config.MusicVolume, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            incoming.volume = _config.MusicVolume;
        }

        private void FadeOutMusic()
        {
            StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            var active = _musicAIsActive ? _musicSourceA : _musicSourceB;
            float startVol = active.volume;
            float elapsed = 0f;
            float duration = _config.MusicCrossfadeDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                active.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            active.Stop();
            active.volume = 0f;
        }

        // ── Combat SFX ───────────────────────────────────────

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            // Play element-specific or weapon impact sound
            if (evt.Element != ElementType.None)
            {
                var clips = _config.GetElementClips(evt.Element);
                PlaySFX(_config.RandomClip(clips));
            }
            else
            {
                // Physical attack: swing + hit layered
                PlaySFX(_config.RandomClip(_config.MeleeHitClips));
            }
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            PlaySFX(_config.RandomClip(_config.HealClips));
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            PlaySFX(_config.RandomClip(_config.UnitDeathClips));
        }

        private void OnStatusApplied(StatusAppliedEvent evt)
        {
            PlaySFX(_config.RandomClip(_config.BuffAppliedClips));
        }

        // ── Turn & Selection ─────────────────────────────────

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (evt.IsPlayerControlled)
                PlaySFX(_config.RandomClip(_config.TurnStartClips));
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            PlaySFX(_config.RandomClip(_config.UnitSelectClips));
        }

        // ── Movement ─────────────────────────────────────────

        private void OnUnitMoveStarted(UnitMoveStartedEvent evt)
        {
            _movingUnitId = evt.UnitId;
            if (_config.FootstepClips != null && _config.FootstepClips.Length > 0)
                _footstepCoroutine = StartCoroutine(FootstepLoop());
        }

        private void OnUnitMoveCompleted(UnitMoveCompletedEvent evt)
        {
            if (evt.UnitId == _movingUnitId)
                StopFootsteps();
        }

        private IEnumerator FootstepLoop()
        {
            while (true)
            {
                PlaySFX(_config.RandomClip(_config.FootstepClips));
                yield return new WaitForSeconds(_config.FootstepInterval);
            }
        }

        private void StopFootsteps()
        {
            if (_footstepCoroutine != null)
            {
                StopCoroutine(_footstepCoroutine);
                _footstepCoroutine = null;
            }
            _movingUnitId = -1;
        }

        // ── Public API (for UI buttons) ──────────────────────

        /// <summary>Play a UI confirm sound. Call from button onClick.</summary>
        public void PlayUIConfirm()
        {
            PlaySFX(_config.RandomClip(_config.UIConfirmClips));
        }

        /// <summary>Play a one-shot SFX clip with pitch variation.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _config == null) return;

            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % SFXPoolSize;

            source.clip = clip;
            source.volume = _config.SFXVolume;
            source.pitch = 1f + Random.Range(-_config.PitchVariation, _config.PitchVariation);
            source.Play();
        }

        // ── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            StopFootsteps();
            StopAllCoroutines();
            UnsubscribeEvents();
        }
    }
}
