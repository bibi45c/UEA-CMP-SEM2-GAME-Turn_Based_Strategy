namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Runtime data for a single hex cell in the grid.
    /// Owned and managed by HexGridMap.
    /// </summary>
    public class HexCell
    {
        public HexCoord Coord { get; }
        public int HeightLevel { get; set; }
        public float WorldY { get; set; }
        public bool Walkable { get; set; }
        public SurfaceType Surface { get; set; }
        public CoverType Cover { get; set; }
        public int OccupantId { get; set; }
        public bool HasRamp { get; set; }

        public bool IsOccupied => OccupantId >= 0;

        public HexCell(HexCoord coord)
        {
            Coord = coord;
            HeightLevel = 0;
            WorldY = 0f;
            Walkable = false;
            Surface = SurfaceType.None;
            Cover = CoverType.None;
            OccupantId = -1;
            HasRamp = false;
        }
    }
}