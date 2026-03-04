using System;
using System.Collections.Generic;
using UnityEngine;
using TurnBasedTactics.Core;
using TurnBasedTactics.Units;

namespace TurnBasedTactics.Exploration
{
    /// <summary>
    /// Manages the exploration phase before combat.
    /// Handles player party spawning, enemy placement, movement,
    /// and encounter-triggered transition to combat.
    /// Config is passed via Initialize() since this component is added at runtime.
    /// </summary>
    public class ExplorationController : MonoBehaviour
    {
        /// <summary>Snapshot of an exploration unit's state, used for combat transition.</summary>
        public struct ExplorationUnitInfo
        {
            public UnitDefinition Definition;
            public Vector3 WorldPosition;
            public int TeamId;
        }

        // Config (set via Initialize)
        private Vector3 _partySpawnPosition;
        private float _partySpacing = 1.5f;
        private float _moveSpeed = 4f;
        private float _rotationSpeed = 10f;
        private UnitDefinition _leaderDefinition;
        private UnitDefinition[] _followerDefinitions;
        private RuntimeAnimatorController _animatorController;
        private Material _defaultMaterial;

        // Party runtime state
        private GameObject _leaderGO;
        private readonly List<GameObject> _followerGOs = new List<GameObject>();
        private readonly List<PartyFollower> _followers = new List<PartyFollower>();
        private ExplorationMovement _leaderMovement;
        private bool _isActive;

        // Enemy runtime state
        private readonly List<GameObject> _enemyGOs = new List<GameObject>();
        private readonly List<UnitDefinition> _enemyDefinitions = new List<UnitDefinition>();

        // Encounter trigger
        private Action _onEncounterTriggered;
        private float _encounterRange = 8f;
        private bool _encounterTriggered;

        public bool IsActive => _isActive;
        public GameObject Leader => _leaderGO;
        public Vector3 PartySpawnPosition => _partySpawnPosition;

        /// <summary>
        /// Initialize the exploration phase. Spawns party at the entrance.
        /// </summary>
        public void Initialize(
            UnitDefinition leader,
            UnitDefinition[] followers,
            Vector3 spawnPosition,
            RuntimeAnimatorController animController,
            Material defaultMaterial = null,
            float moveSpeed = 4f,
            float rotationSpeed = 10f,
            float partySpacing = 1.5f)
        {
            _leaderDefinition = leader;
            _followerDefinitions = followers;
            _partySpawnPosition = spawnPosition;
            _animatorController = animController;
            _defaultMaterial = defaultMaterial;
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _partySpacing = partySpacing;

            SpawnParty();
            _isActive = true;
            Debug.Log("[ExplorationController] Exploration phase started.");
        }

        /// <summary>
        /// Spawn visual-only enemy models at given world positions.
        /// Enemies patrol within a small radius and trigger combat on player proximity.
        /// </summary>
        public void SpawnEnemies(UnitDefinition[] definitions, Vector3[] positions)
        {
            if (definitions == null || positions == null) return;

            int count = Mathf.Min(definitions.Length, positions.Length);
            for (int i = 0; i < count; i++)
            {
                if (definitions[i] == null) continue;

                var enemyGO = SpawnExplorationUnit(definitions[i], positions[i],
                    $"ExplorationEnemy_{definitions[i].UnitName}_{i}");
                _enemyGOs.Add(enemyGO);
                _enemyDefinitions.Add(definitions[i]);

                // Attach patrol behavior
                var patrol = enemyGO.AddComponent<ExplorationPatrol>();
                patrol.Initialize(positions[i], radius: 2.5f, speed: 1.2f);
            }

            Debug.Log($"[ExplorationController] Spawned {_enemyGOs.Count} enemy NPCs with patrol.");
        }

        /// <summary>
        /// Register a callback to fire when the player gets close to any enemy.
        /// </summary>
        public void SetEncounterCallback(Action callback, float range = 8f)
        {
            _onEncounterTriggered = callback;
            _encounterRange = range;
        }

        /// <summary>
        /// Capture current positions and definitions of all exploration units.
        /// Call BEFORE DespawnParty to preserve positions for combat spawn.
        /// </summary>
        public List<ExplorationUnitInfo> GetAllUnitData()
        {
            var data = new List<ExplorationUnitInfo>();

            // Leader
            if (_leaderGO != null && _leaderDefinition != null)
            {
                data.Add(new ExplorationUnitInfo
                {
                    Definition = _leaderDefinition,
                    WorldPosition = _leaderGO.transform.position,
                    TeamId = 0
                });
            }

            // Followers
            if (_followerDefinitions != null)
            {
                for (int i = 0; i < _followerGOs.Count && i < _followerDefinitions.Length; i++)
                {
                    if (_followerGOs[i] != null && _followerDefinitions[i] != null)
                    {
                        data.Add(new ExplorationUnitInfo
                        {
                            Definition = _followerDefinitions[i],
                            WorldPosition = _followerGOs[i].transform.position,
                            TeamId = 0
                        });
                    }
                }
            }

            // Enemies
            for (int i = 0; i < _enemyGOs.Count && i < _enemyDefinitions.Count; i++)
            {
                if (_enemyGOs[i] != null && _enemyDefinitions[i] != null)
                {
                    data.Add(new ExplorationUnitInfo
                    {
                        Definition = _enemyDefinitions[i],
                        WorldPosition = _enemyGOs[i].transform.position,
                        TeamId = 1
                    });
                }
            }

            return data;
        }

        /// <summary>
        /// End exploration and prepare for combat transition.
        /// </summary>
        public void EndExploration()
        {
            _isActive = false;

            if (_leaderMovement != null)
                _leaderMovement.enabled = false;

            foreach (var follower in _followers)
            {
                if (follower != null)
                    follower.enabled = false;
            }

            Debug.Log("[ExplorationController] Exploration phase ended.");
        }

        /// <summary>
        /// Despawn all exploration objects (party + enemies).
        /// Called before combat spawns grid-based units.
        /// </summary>
        public void DespawnParty()
        {
            foreach (var go in _followerGOs)
            {
                if (go != null) Destroy(go);
            }
            _followerGOs.Clear();
            _followers.Clear();

            if (_leaderGO != null)
            {
                Destroy(_leaderGO);
                _leaderGO = null;
            }

            _leaderMovement = null;

            // Also despawn enemy visuals
            foreach (var go in _enemyGOs)
            {
                if (go != null) Destroy(go);
            }
            _enemyGOs.Clear();
            _enemyDefinitions.Clear();
        }

        /// <summary>
        /// Get the current world positions of all party members.
        /// Index 0 = leader, rest = followers.
        /// </summary>
        public List<Vector3> GetPartyPositions()
        {
            var positions = new List<Vector3>();
            if (_leaderGO != null)
                positions.Add(_leaderGO.transform.position);
            foreach (var go in _followerGOs)
            {
                if (go != null)
                    positions.Add(go.transform.position);
            }
            return positions;
        }

        /// <summary>Get transforms of all party followers (for minimap).</summary>
        public List<Transform> GetFollowerTransforms()
        {
            var transforms = new List<Transform>();
            foreach (var go in _followerGOs)
            {
                if (go != null) transforms.Add(go.transform);
            }
            return transforms;
        }

        /// <summary>Get transforms of all exploration enemies (for minimap).</summary>
        public List<Transform> GetEnemyTransforms()
        {
            var transforms = new List<Transform>();
            foreach (var go in _enemyGOs)
            {
                if (go != null) transforms.Add(go.transform);
            }
            return transforms;
        }

        private void Update()
        {
            if (!_isActive || _encounterTriggered || _leaderGO == null) return;

            CheckEncounterProximity();
        }

        private void CheckEncounterProximity()
        {
            if (_onEncounterTriggered == null || _enemyGOs.Count == 0) return;

            Vector3 leaderPos = _leaderGO.transform.position;

            foreach (var enemyGO in _enemyGOs)
            {
                if (enemyGO == null) continue;

                float dist = Vector3.Distance(leaderPos, enemyGO.transform.position);
                if (dist < _encounterRange)
                {
                    _encounterTriggered = true;
                    Debug.Log($"[ExplorationController] Encounter triggered! Distance to {enemyGO.name}: {dist:F1}m");
                    _onEncounterTriggered.Invoke();
                    break;
                }
            }
        }

        private void SpawnParty()
        {
            if (_leaderDefinition == null)
            {
                Debug.LogError("[ExplorationController] Leader definition not assigned!");
                return;
            }

            // Spawn leader (mage)
            _leaderGO = SpawnExplorationUnit(_leaderDefinition, _partySpawnPosition, "ExplorationLeader");
            _leaderMovement = _leaderGO.AddComponent<ExplorationMovement>();
            _leaderMovement.Initialize(_moveSpeed, _rotationSpeed);

            // Spawn followers behind the leader
            if (_followerDefinitions != null)
            {
                for (int i = 0; i < _followerDefinitions.Length; i++)
                {
                    if (_followerDefinitions[i] == null) continue;

                    Vector3 offset = GetFollowerOffset(i);
                    Vector3 spawnPos = _partySpawnPosition + offset;
                    string name = $"ExplorationFollower_{i}";

                    var followerGO = SpawnExplorationUnit(_followerDefinitions[i], spawnPos, name);
                    _followerGOs.Add(followerGO);

                    var follower = followerGO.AddComponent<PartyFollower>();
                    float followDist = _partySpacing * (1.5f + i * 0.8f);
                    follower.Initialize(_leaderGO.transform, _moveSpeed * 1.1f, _rotationSpeed, followDist);
                    _followers.Add(follower);
                }
            }

            Debug.Log($"[ExplorationController] Party spawned: 1 leader + {_followerGOs.Count} followers at {_partySpawnPosition}");
        }

        private Vector3 GetFollowerOffset(int index)
        {
            float behindOffset = -_partySpacing * 1.5f;
            float sideOffset = (index % 2 == 0 ? -1f : 1f) * _partySpacing * 0.8f;
            return new Vector3(sideOffset, 0f, behindOffset * (1 + index * 0.5f));
        }

        private GameObject SpawnExplorationUnit(UnitDefinition definition, Vector3 position, string goName)
        {
            var unitGO = new GameObject(goName);
            unitGO.transform.position = position;

            // Instantiate model
            if (definition.ModelPrefab != null)
            {
                var model = Instantiate(definition.ModelPrefab, unitGO.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                var animator = model.GetComponentInChildren<Animator>();
                if (animator != null && _animatorController != null)
                {
                    animator.runtimeAnimatorController = _animatorController;
                }

                // Apply default material workaround for units without original materials
                if (!definition.UseOriginalMaterials && _defaultMaterial != null)
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
            }

            // Attach weapon
            if (definition.WeaponPrefab != null && unitGO.transform.childCount > 0)
            {
                var modelRoot = unitGO.transform.GetChild(0).gameObject;
                Transform bone = FindBoneRecursive(modelRoot.transform, definition.WeaponBoneName);
                if (bone != null)
                {
                    var weapon = Instantiate(definition.WeaponPrefab, bone);
                    weapon.name = "Weapon";
                    weapon.transform.localPosition = definition.WeaponPositionOffset;
                    weapon.transform.localRotation = Quaternion.Euler(definition.WeaponRotationOffset);
                    weapon.transform.localScale = Vector3.one;
                }
            }

            // Add capsule collider for ground raycast (trigger so it doesn't block physics)
            var capsule = unitGO.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.8f, 0f);
            capsule.radius = 0.3f;
            capsule.height = 1.6f;
            capsule.isTrigger = true;

            return unitGO;
        }

        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
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
    }
}
