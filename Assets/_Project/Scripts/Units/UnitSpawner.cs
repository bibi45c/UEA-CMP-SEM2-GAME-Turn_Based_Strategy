using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Core;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Factory that creates unit GameObjects, wires components, and registers
    /// units in the grid. Maintains a lookup from UnitId to UnitBrain.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private RuntimeAnimatorController _defaultAnimator;
        [SerializeField] private Material _defaultMaterial;

        private HexGridMap _gridMap;
        private UnitRegistry _registry;
        private Transform _unitsRoot;
        private readonly Dictionary<int, UnitBrain> _brainLookup = new Dictionary<int, UnitBrain>();

        public void Initialize(HexGridMap gridMap, UnitRegistry registry, Transform unitsRoot)
        {
            _gridMap = gridMap;
            _registry = registry;
            _unitsRoot = unitsRoot;
        }

        /// <summary>
        /// Get the UnitBrain MonoBehaviour for a given unit ID.
        /// </summary>
        public UnitBrain GetBrain(int unitId)
        {
            _brainLookup.TryGetValue(unitId, out UnitBrain brain);
            return brain;
        }

        public bool DespawnUnit(int unitId)
        {
            if (!_brainLookup.TryGetValue(unitId, out UnitBrain brain))
                return false;

            _brainLookup.Remove(unitId);

            if (brain != null)
                Destroy(brain.gameObject);

            return true;
        }

        /// <summary>
        /// Spawn a unit on the grid at the specified hex position.
        /// Creates GO, wires all components, sets grid occupancy.
        /// </summary>
        public UnitBrain SpawnUnit(UnitDefinition definition, HexCoord position, int teamId)
        {
            if (definition == null)
            {
                Debug.LogError("[UnitSpawner] Cannot spawn unit: definition is null!");
                return null;
            }

            // 1. Generate unique ID and create runtime
            int unitId = _registry.GenerateId();
            var runtime = new UnitRuntime(unitId, definition, teamId, position);
            _registry.Register(runtime);

            // 2. Create root GO at grid world position
            Vector3 worldPos = _gridMap.GetCellWorldPosition(position);
            var unitGO = new GameObject($"Unit_{definition.UnitName}_{unitId}");
            unitGO.transform.SetParent(_unitsRoot);
            unitGO.transform.position = worldPos;

            // Set layer for raycast selection
            int unitsLayer = LayerMask.NameToLayer("Units");
            if (unitsLayer >= 0)
                unitGO.layer = unitsLayer;

            // 3. Add UnitBrain (thin binding)
            var brain = unitGO.AddComponent<UnitBrain>();
            brain.Initialize(runtime);

            // 4. Add UnitVisual (presentation)
            var visual = unitGO.AddComponent<UnitVisual>();

            // 5. Instantiate model as child
            Animator animator = null;
            if (definition.ModelPrefab != null)
            {
                var model = Instantiate(definition.ModelPrefab, unitGO.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // Set layer recursively for raycast
                SetLayerRecursive(model, unitGO.layer);

                // Find and configure Animator
                animator = model.GetComponentInChildren<Animator>();
                if (animator != null && _defaultAnimator != null)
                {
                    animator.runtimeAnimatorController = _defaultAnimator;
                }

                // Apply material workaround (missing Synty shader) unless model has proper materials
                if (_defaultMaterial != null && !definition.UseOriginalMaterials)
                {
                    ApplyMaterialWorkaround(model);
                }
            }

            visual.Initialize(animator, teamId);

            // 5b. Attach weapon if defined
            if (definition.WeaponPrefab != null && unitGO.transform.childCount > 0)
            {
                AttachWeapon(unitGO.transform.GetChild(0).gameObject, definition);
            }

            // 6. Add CapsuleCollider for selection raycasting
            var capsule = unitGO.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.8f, 0f);
            capsule.radius = 0.3f;
            capsule.height = 1.6f;

            // 7. Set grid occupancy
            _gridMap.SetOccupant(position, unitId);

            // 8. Cache brain lookup
            _brainLookup[unitId] = brain;

            // 9. Publish event
            EventBus.Publish(new UnitSpawnedEvent
            {
                UnitId = unitId,
                Position = position
            });

            Debug.Log($"[UnitSpawner] Spawned {definition.UnitName} (ID:{unitId}) at {position}, " +
                      $"Team:{teamId}, HP:{runtime.CurrentHP}, Move:{runtime.Stats.MovementPoints}");

            return brain;
        }

        private void AttachWeapon(GameObject model, UnitDefinition definition)
        {
            string boneName = definition.WeaponBoneName;
            Transform bone = FindBoneRecursive(model.transform, boneName);
            if (bone == null)
            {
                Debug.LogWarning($"[UnitSpawner] Could not find bone '{boneName}' for weapon attachment on {definition.UnitName}.");
                return;
            }

            var weapon = Instantiate(definition.WeaponPrefab, bone);
            weapon.name = "Weapon";
            weapon.transform.localPosition = definition.WeaponPositionOffset;
            weapon.transform.localRotation = Quaternion.Euler(definition.WeaponRotationOffset);
            weapon.transform.localScale = Vector3.one;

            // Set layer recursively
            SetLayerRecursive(weapon, model.layer);

            Debug.Log($"[UnitSpawner] Attached weapon to '{boneName}' on {definition.UnitName}.");
        }

        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            // Check if name contains the bone name (handles prefixes like "mixamorig:")
            if (parent.name.Contains(boneName))
                return parent;

            foreach (Transform child in parent)
            {
                var found = FindBoneRecursive(child, boneName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ApplyMaterialWorkaround(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _defaultMaterial;
                r.sharedMaterials = mats;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
