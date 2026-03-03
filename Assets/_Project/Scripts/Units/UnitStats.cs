using UnityEngine;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Centralized stat aggregation: base + equipment + buffs = final values.
    /// All derived formulas live HERE and nowhere else (per CLAUDE.md rules).
    /// Plain C# class — no MonoBehaviour.
    /// </summary>
    public class UnitStats
    {
        private readonly UnitDefinition _definition;
        private readonly int _level;

        // Equipment bonuses (Phase 1: stubs, populated when Items module is built)
        private int _equipStrength;
        private int _equipFinesse;
        private int _equipIntelligence;
        private int _equipConstitution;
        private int _equipWits;
        private int _equipMovement;

        // Buff bonuses (Phase 1: stubs)
        private int _buffStrength;
        private int _buffFinesse;
        private int _buffIntelligence;
        private int _buffConstitution;
        private int _buffWits;
        private int _buffMovement;

        public UnitStats(UnitDefinition definition)
        {
            _definition = definition;
            _level = definition.Level;
        }

        // --- Final Attributes (base + equipment + buff) ---

        public int Strength => _definition.Strength + _equipStrength + _buffStrength;
        public int Finesse => _definition.Finesse + _equipFinesse + _buffFinesse;
        public int Intelligence => _definition.Intelligence + _equipIntelligence + _buffIntelligence;
        public int Constitution => _definition.Constitution + _equipConstitution + _buffConstitution;
        public int Wits => _definition.Wits + _equipWits + _buffWits;

        // --- Derived Values ---

        public int MaxHP => 20 + Constitution * 3 + _level * 5;
        public int PhysicalArmor => Constitution / 2;
        public int MagicResistance => Intelligence / 2;
        public int MovementPoints => _definition.BaseMovementPoints + _equipMovement + _buffMovement;
        public int ActionPoints => _definition.BaseActionPoints;
        public float Initiative => Wits * 1.0f;
        public float CritChance => Mathf.Clamp01(Wits * 0.01f);

        // --- Equipment Modifier API (Phase 1 stubs) ---

        public void SetEquipmentBonuses(int str, int fin, int intel, int con, int wits, int move)
        {
            _equipStrength = str;
            _equipFinesse = fin;
            _equipIntelligence = intel;
            _equipConstitution = con;
            _equipWits = wits;
            _equipMovement = move;
        }

        public void ClearEquipmentBonuses()
        {
            _equipStrength = 0;
            _equipFinesse = 0;
            _equipIntelligence = 0;
            _equipConstitution = 0;
            _equipWits = 0;
            _equipMovement = 0;
        }

        // --- Buff Modifier API (Phase 1 stubs) ---

        public void SetBuffBonuses(int str, int fin, int intel, int con, int wits, int move)
        {
            _buffStrength = str;
            _buffFinesse = fin;
            _buffIntelligence = intel;
            _buffConstitution = con;
            _buffWits = wits;
            _buffMovement = move;
        }

        public void ClearBuffBonuses()
        {
            _buffStrength = 0;
            _buffFinesse = 0;
            _buffIntelligence = 0;
            _buffConstitution = 0;
            _buffWits = 0;
            _buffMovement = 0;
        }
    }
}
