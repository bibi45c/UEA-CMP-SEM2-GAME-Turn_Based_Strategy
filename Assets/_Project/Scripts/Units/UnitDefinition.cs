using UnityEngine;
using TurnBasedTactics.Abilities;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Immutable template defining a unit archetype (Warrior, Archer, Mage, etc.).
    /// One ScriptableObject asset per unit type. Runtime state lives in UnitRuntime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitDefinition", menuName = "TurnBasedTactics/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _unitName = "Unit";
        [SerializeField] private Sprite _portrait;
        [SerializeField] private GameObject _modelPrefab;

        [Header("Equipment")]
        [SerializeField] private GameObject _weaponPrefab;
        [Tooltip("Name of the hand bone to attach the weapon to (e.g., Hand_R)")]
        [SerializeField] private string _weaponBoneName = "Hand_R";
        [Tooltip("Local position offset for the weapon relative to the bone")]
        [SerializeField] private Vector3 _weaponPositionOffset = Vector3.zero;
        [Tooltip("Local rotation offset (Euler) for the weapon relative to the bone")]
        [SerializeField] private Vector3 _weaponRotationOffset = Vector3.zero;

        [Header("Rendering")]
        [Tooltip("If true, keep the model's original materials instead of applying the default material workaround")]
        [SerializeField] private bool _useOriginalMaterials = false;

        [Header("Base Attributes")]
        [SerializeField] private int _strength = 10;
        [SerializeField] private int _finesse = 10;
        [SerializeField] private int _intelligence = 10;
        [SerializeField] private int _constitution = 10;
        [SerializeField] private int _wits = 10;

        [Header("Combat")]
        [SerializeField] private int _baseMovementPoints = 4;
        [SerializeField] private int _baseActionPoints = 6;
        [SerializeField] private int _level = 1;

        [Header("Abilities")]
        [SerializeField] private AbilityDefinition[] _abilities;

        // --- Public API (read-only) ---

        public string UnitName => _unitName;
        public Sprite Portrait => _portrait;
        public GameObject ModelPrefab => _modelPrefab;
        public GameObject WeaponPrefab => _weaponPrefab;
        public string WeaponBoneName => _weaponBoneName;
        public Vector3 WeaponPositionOffset => _weaponPositionOffset;
        public Vector3 WeaponRotationOffset => _weaponRotationOffset;
        public bool UseOriginalMaterials => _useOriginalMaterials;

        public int Strength => _strength;
        public int Finesse => _finesse;
        public int Intelligence => _intelligence;
        public int Constitution => _constitution;
        public int Wits => _wits;

        public int BaseMovementPoints => _baseMovementPoints;
        public int BaseActionPoints => _baseActionPoints;
        public int Level => _level;
        public AbilityDefinition[] Abilities => _abilities;

        private void OnValidate()
        {
            _strength = Mathf.Max(1, _strength);
            _finesse = Mathf.Max(1, _finesse);
            _intelligence = Mathf.Max(1, _intelligence);
            _constitution = Mathf.Max(1, _constitution);
            _wits = Mathf.Max(1, _wits);
            _baseMovementPoints = Mathf.Clamp(_baseMovementPoints, 1, 20);
            _baseActionPoints = Mathf.Clamp(_baseActionPoints, 1, 20);
            _level = Mathf.Max(1, _level);
        }
    }
}
