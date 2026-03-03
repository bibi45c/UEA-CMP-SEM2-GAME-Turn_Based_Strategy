using UnityEngine;
using TurnBasedTactics.Abilities;
using TurnBasedTactics.Camera;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Combat
{
    /// <summary>
    /// Subscribes to combat events and dispatches visual effects:
    /// camera shake, hit flash, impact particles, heal particles.
    /// Attach to CombatRoot; wired by GameBootstrap.
    /// </summary>
    public class CombatVFXManager : MonoBehaviour
    {
        [SerializeField] private CombatVFXConfig _config = new CombatVFXConfig();

        private UnitSpawner _spawner;
        private CameraShake _cameraShake;

        public void Initialize(UnitSpawner spawner, CameraShake cameraShake)
        {
            _spawner = spawner;
            _cameraShake = cameraShake;

            EventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Subscribe<UnitHealedEvent>(OnUnitHealed);

            Debug.Log("[CombatVFXManager] Initialized.");
        }

        private void OnUnitDamaged(UnitDamagedEvent evt)
        {
            Vector3 targetPos = GetUnitWorldPosition(evt.TargetUnitId);
            if (targetPos == Vector3.zero) return;

            // 1. Camera shake — intensity scales with event severity
            if (_cameraShake != null)
            {
                if (evt.DidKill)
                    _cameraShake.Shake(_config.HeavyShakeIntensity, _config.HeavyShakeDuration, _config.ShakeFrequency);
                else if (evt.WasCritical)
                    _cameraShake.Shake(_config.MediumShakeIntensity, _config.MediumShakeDuration, _config.ShakeFrequency);
                else
                    _cameraShake.Shake(_config.LightShakeIntensity, _config.LightShakeDuration, _config.ShakeFrequency);
            }

            // 2. Hit flash on damaged unit
            Color flashColor = evt.Element == ElementType.None
                ? _config.PhysicalFlashColor
                : CombatVFXConfig.GetElementColor(evt.Element);
            TriggerHitFlash(evt.TargetUnitId, flashColor);

            // 3. Impact particles at target position
            Color particleColor = CombatVFXConfig.GetElementColor(evt.Element);
            SpawnImpactParticles(targetPos, particleColor, evt.WasCritical);
        }

        private void OnUnitHealed(UnitHealedEvent evt)
        {
            Vector3 pos = GetUnitWorldPosition(evt.TargetUnitId);
            if (pos == Vector3.zero) return;

            SpawnHealParticles(pos);
        }

        private Vector3 GetUnitWorldPosition(int unitId)
        {
            var brain = _spawner != null ? _spawner.GetBrain(unitId) : null;
            return brain != null ? brain.transform.position + Vector3.up * 1f : Vector3.zero;
        }

        private void TriggerHitFlash(int unitId, Color color)
        {
            var brain = _spawner != null ? _spawner.GetBrain(unitId) : null;
            if (brain == null) return;

            var flash = brain.GetComponent<HitFlashEffect>();
            if (flash != null)
                flash.Flash(color, _config.FlashDuration);
        }

        // --- Procedural Particle Effects ---

        private void SpawnImpactParticles(Vector3 position, Color color, bool isCritical)
        {
            var go = new GameObject("ImpactVFX");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();

            // Stop default playback to configure first
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = _config.ImpactParticleLifetime;
            main.startSpeed = _config.ImpactParticleSpeed;
            main.startSize = isCritical ? _config.ImpactParticleSize * 1.5f : _config.ImpactParticleSize;
            main.startColor = color;
            int burstCount = isCritical ? Mathf.RoundToInt(_config.ImpactBurstCount * 1.5f) : _config.ImpactBurstCount;
            main.maxParticles = burstCount;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            // Shrink over lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Fade out over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Material — Particles/Standard Unlit works in Built-in RP
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                var mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.SetFloat("_Mode", 1f); // Additive
                mat.color = color;
                psr.material = mat;
            }

            ps.Play();
        }

        private void SpawnHealParticles(Vector3 position)
        {
            var go = new GameObject("HealVFX");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();

            // Stop default playback to configure first
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = _config.HealParticleLifetime;
            main.startSpeed = _config.HealRiseSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(_config.HealPrimaryColor, _config.HealSecondaryColor);
            main.maxParticles = _config.HealParticleCount;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = -0.15f; // Gentle upward drift

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)_config.HealParticleCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Point upward

            // Shrink over lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 0.8f, 1f, 0f));

            // Fade out
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(_config.HealPrimaryColor, 0f), new GradientColorKey(_config.HealSecondaryColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Material
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                var mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.SetFloat("_Mode", 1f); // Additive
                mat.color = _config.HealPrimaryColor;
                psr.material = mat;
            }

            ps.Play();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
            EventBus.Unsubscribe<UnitHealedEvent>(OnUnitHealed);
        }
    }
}
