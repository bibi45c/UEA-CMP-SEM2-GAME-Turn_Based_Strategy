using UnityEngine;
using TurnBasedTactics.Abilities;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Data template for a surface effect that can exist on hex cells.
    /// Surfaces persist across turns, damage/debuff units, and react with each other.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSurface", menuName = "TurnBasedTactics/Surface Definition")]
    public class SurfaceDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private SurfaceType _surfaceType = SurfaceType.Fire;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(2, 3)] private string _description;

        [Header("Visuals")]
        [SerializeField] private Color _tintColor = new Color(1f, 0.4f, 0f, 0.5f);

        [Header("Duration")]
        [Tooltip("Number of rounds this surface lasts (0 = permanent until dispelled)")]
        [SerializeField] private int _defaultDuration = 3;

        [Header("Per-Turn Damage (to unit standing on this cell)")]
        [SerializeField] private int _tickDamage;
        [SerializeField] private ElementType _tickElement = ElementType.None;
        [SerializeField] private bool _tickIgnoresArmor;

        [Header("On-Enter Status")]
        [Tooltip("Status applied to a unit when it enters or starts its turn on this surface")]
        [SerializeField] private StatusDefinition _onEnterStatus;

        [Header("Movement")]
        [Tooltip("Added to base movement cost (negative = faster, positive = slower)")]
        [SerializeField] private float _movementCostModifier;
        [SerializeField] private bool _blockMovement;

        // --- Public API ---
        public SurfaceType SurfaceType => _surfaceType;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Color TintColor => _tintColor;
        public int DefaultDuration => _defaultDuration;
        public int TickDamage => _tickDamage;
        public ElementType TickElement => _tickElement;
        public bool TickIgnoresArmor => _tickIgnoresArmor;
        public StatusDefinition OnEnterStatus => _onEnterStatus;
        public float MovementCostModifier => _movementCostModifier;
        public bool BlockMovement => _blockMovement;

        private void OnValidate()
        {
            _defaultDuration = Mathf.Max(0, _defaultDuration);
            if (string.IsNullOrEmpty(_displayName))
                _displayName = _surfaceType.ToString();
        }
    }
}
