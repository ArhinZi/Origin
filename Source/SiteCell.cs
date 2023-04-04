using Origin.Source.Utils;

using System;

namespace Origin.Source
{
    [Flags]
    public enum CellVisual : short
    {
        None = 0,
        Visible = 0b1,
        Discovered = 0b10
    }

    public class SiteCell
    {
        public readonly int seed;
        public string WallID;
        public string EmbeddedWallID;
        public string FloorID;
        public string EmbeddedFloorID;

        public Site ParentSite { get; private set; }
        public Point3 Position { get; private set; }
        public CellVisual WallVisual { get; set; } = 0;
        public CellVisual FloorVisual { get; set; } = 0;

        public SiteCell(Site site, Point3 pos,
            string wallMatID = null, string floorMatID = null, string embWallMatID = null, string embFloorMatID = null)
        {
            ParentSite = site;
            Position = pos;
            WallID = wallMatID;
            FloorID = floorMatID;
            seed = Seeder.Random.Next();
            EmbeddedWallID = embWallMatID;
            EmbeddedFloorID = embFloorMatID;
        }

        public void RemoveWall()
        {
            WallID = TerrainMaterial.AIR_NULL_MAT_ID;
            EmbeddedWallID = null;
        }

        public void RemoveFloor()
        {
            FloorID = TerrainMaterial.AIR_NULL_MAT_ID;
            EmbeddedFloorID = null;
            RemoveWall();
        }

        public void RemoveBlock()
        {
            RemoveBlock();
            RemoveFloor();
        }
    }
}