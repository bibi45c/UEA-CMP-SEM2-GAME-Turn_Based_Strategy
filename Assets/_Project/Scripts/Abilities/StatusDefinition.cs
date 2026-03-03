using UnityEngine;

namespace TurnBasedTactics.Abilities
{
    /// <summary>
    /// Defines a status effect that can be applied to units via abilities.
    /// Each status ticks once per turn start, dealing damage or healing,
    /// and may modify stats for its duration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatus", menuName = "TurnBasedTactics/Status Definition")]
    public class StatusDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _statusId;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(2, 3)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _tintColor = Color.white;

        [Header("Duration")]
        [Tooltip("Number of turn-starts this status lasts (0 = permanent until dispelled)")]
        [SerializeField] private int _duration = 3;

        [Header("Per-Turn Effect")]
        [Tooltip("Damage dealt each turn start (negative = heal)")]
        [SerializeField] private int _tickDamage;
        [SerializeField] private ElementType _tickElement = ElementType.None;
        [SerializeField] private bool _tickIgnoresArmor;

        [Header("Stat Modifiers (flat bonus while active)")]
        [SerializeField] private int _strengthMod;
        [SerializeField] private int _finesseMod;
        [SerializeField] private int _intelligenceMod;
        [SerializeField] private int _constitutionMod;
        [SerializeField] private int _witsMod;
        [SerializeField] private int _movementMod;

        [Header("Flags")]
        [SerializeField] private bool _preventsMovement;
        [SerializeField] private bool _preventsActions;
        [SerializeField] private bool _stackable;

        // --- Public API ---
        public string StatusId => _statusId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color TintColor => _tintColor;
        public int Duration => _duration;
        public int TickDamage => _tickDamage;
        public ElementType TickElement => _tickElement;
        public bool TickIgnoresArmor => _tickIgnoresArmor;
        public int StrengthMod => _strengthMod;
        public int FinesseMod => _finesseMod;
        public int IntelligenceMod => _intelligenceMod;
        public int ConstitutionMod => _constitutionMod;
        public int WitsMod => _witsMod;
        public int MovementMod => _movementMod;
        public bool PreventsMovement => _preventsMovement;
        public bool PreventsActions => _preventsActions;
        public bool Stackable => _stackable;

        public bool HasStatMods =>
            _strengthMod != 0 || _finesseMod != 0 || _intelligenceMod != 0 ||
            _constitutionMod != 0 || _witsMod != 0 || _movementMod != 0;

        private void OnValidate()
        {
            _duration = Mathf.Max(0, _duration);
            if (string.IsNullOrEmpty(_statusId) && !string.IsNullOrEmpty(_displayName))
                _statusId = _displayName.Replace(" ", "");
        }
    }
}
