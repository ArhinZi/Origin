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

        public SiteCell(string wmatid = null, string fmatid = null, string embededWallID = null, string embededFloorID = null)
        {
            WallID = wmatid;
            FloorID = fmatid;
            seed = Seeder.Random.Next();
            EmbeddedWallID = embededWallID;
            EmbeddedFloorID = embededFloorID;
        }
    }
}