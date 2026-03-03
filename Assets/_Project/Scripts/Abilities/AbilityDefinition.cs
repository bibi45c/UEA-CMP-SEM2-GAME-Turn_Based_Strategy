using UnityEngine;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Data-driven ability template. One ScriptableObject asset per ability.
    /// Defines targeting rules, range, effects, and cost.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "TurnBasedTactics/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _abilityName = "New Ability";
        [SerializeField] [TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Targeting")]
        [SerializeField] private TargetingType _targetingType = TargetingType.SingleEnemy;
        [SerializeField] private int _range = 1;
        [Tooltip("Radius for CircleAOE targeting (in hex distance from center)")]
        [SerializeField] private int _aoeRadius;

        [Header("Cost")]
        [Tooltip("Action point cost (Phase 1: always 1; reserved for future AP system)")]
        [SerializeField] private int _apCost = 1;
        [Tooltip("Cooldown in turns (0 = no cooldown)")]
        [SerializeField] private int _cooldown;

        [Header("Element")]
        [SerializeField] private ElementType _element = ElementType.None;

        [Header("Effects")]
        [SerializeField] private EffectPayload[] _effects;

        [Header("Presentation")]
        [Tooltip("Animator trigger name to play when this ability is used")]
        [SerializeField] private string _animationTrigger = "Action";

        // --- Public API (read-only) ---

        public string AbilityName => _abilityName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public TargetingType TargetingType => _targetingType;
        public int Range => _range;
        public int AoeRadius => _aoeRadius;
        public int ApCost => _apCost;
        public int Cooldown => _cooldown;
        public ElementType Element => _element;
        public EffectPayload[] Effects => _effects;
        public string AnimationTrigger => _animationTrigger;

        /// <summary>
        /// Returns true if any effect in this ability deals damage.
        /// </summary>
        public bool IsDamaging
        {
            get
            {
                if (_effects == null) return false;
                foreach (var e in _effects)
                {
                    if (e.EffectType == AbilityEffectType.PhysicalDamage ||
                        e.EffectType == AbilityEffectType.MagicDamage)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if any effect in this ability heals.
        /// </summary>
        public bool IsHealing
        {
            get
            {
                if (_effects == null) return false;
                foreach (var e in _effects)
                {
                    if (e.EffectType == AbilityEffectType.Heal)
                        return true;
                }
                return false;
            }
        }

        private void OnValidate()
        {
            _range = Mathf.Max(0, _range);
            _aoeRadius = Mathf.Max(0, _aoeRadius);
            _apCost = Mathf.Max(1, _apCost);
            _cooldown = Mathf.Max(0, _cooldown);
        }
    }
}
