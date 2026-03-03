namespace TurnBasedTactics.Abilities
{
    public enum TargetingType
    {
        SingleEnemy,
        SingleAlly,
        Self,
        CircleAOE
    }

    public enum AbilityEffectType
    {
        PhysicalDamage,
        MagicDamage,
        Heal,
        ApplyStatus,
        CreateSurface
    }

    public enum ScalingStat
    {
        None,
        Strength,
        Finesse,
        Intelligence,
        Constitution,
        Wits
    }

    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy
    }
}
