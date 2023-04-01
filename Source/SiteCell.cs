using System;

namespace Origin.Source
{
    public class SiteCell
    {
        public readonly int seed;
        public string WallID;
        public string EmbeddedWallID;
        public string FloorID;
        public string EmbeddedFloorID;
        public bool IsWallVisible { get; set; } = false;
        public bool IsFloorVisible { get; set; } = false;

        public SiteCell(string wallMatID = null, string floorMatID = null, string embWallMatID = null, string embFloorMatID = null)
        {
            WallID = wallMatID;
            FloorID = floorMatID;
            seed = Seeder.Random.Next();
            EmbeddedWallID = embWallMatID;
            EmbeddedFloorID = embFloorMatID;
        }
    }
}