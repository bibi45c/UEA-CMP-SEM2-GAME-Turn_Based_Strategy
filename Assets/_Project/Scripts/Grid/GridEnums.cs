namespace TurnBasedTactics.Grid
{
    public enum HexDirection
    {
        E = 0,
        NE = 1,
        NW = 2,
        W = 3,
        SW = 4,
        SE = 5
    }

    public enum SurfaceType
    {
        None = 0,
        Oil,
        Fire,
        Water,
        Poison,
        Ice,
        Electricity,
        Blood
    }

    public enum CoverType
    {
        None = 0,
        HalfCover,
        FullCover
    }
}