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

        public int WaterLevel;

        public Site ParentSite { get; private set; }
        public Point3 Position { get; private set; }
        public CellVisual WallVisual { get; set; } = 0;
        public CellVisual FloorVisual { get; set; } = 0;

        public SiteCell(Site site, Point3 pos,
            string wallMatID = null, string floorMatID = null,
            string embWallMatID = null, string embFloorMatID = null,
            int waterLevel = 0)
        {
            ParentSite = site;
            Position = pos;
            WallID = wallMatID;
            FloorID = floorMatID;
            seed = Seeder.Random.Next();
            EmbeddedWallID = embWallMatID;
            EmbeddedFloorID = embFloorMatID;
            WaterLevel = waterLevel;
        }

        public bool PassAbility()
        {
            if (WallID == TerrainMaterial.AIR_NULL_MAT_ID)
            {
                SiteCell sc = ParentSite.GetOrNull(Position + new Point3(0, 0, -1));
                if (sc != null && sc.FloorID != TerrainMaterial.AIR_NULL_MAT_ID)
                    return true;
            }
            return false;
        }

        public bool RemoveWall()
        {
            bool res = WallID != TerrainMaterial.AIR_NULL_MAT_ID;
            WallID = TerrainMaterial.AIR_NULL_MAT_ID;
            EmbeddedWallID = null;
            return res;
        }

        public bool RemoveFloor()
        {
            bool res = WallID != TerrainMaterial.AIR_NULL_MAT_ID;
            FloorID = TerrainMaterial.AIR_NULL_MAT_ID;
            EmbeddedFloorID = null;
            RemoveWall();
            return res;
        }

        public void RemoveBlock()
        {
            RemoveBlock();
            RemoveFloor();
        }

        public SiteCell GetNextCellByDirection(IsometricDirection dir)
        {
            SiteCell sc = null;
            if (dir == IsometricDirection.TL)
                sc = ParentSite.GetOrNull(Position + new Point3(-1, 0, 0));
            else if (dir == IsometricDirection.TR)
                sc = ParentSite.GetOrNull(Position + new Point3(0, -1, 0));
            else if (dir == IsometricDirection.BL)
                sc = ParentSite.GetOrNull(Position + new Point3(0, +1, 0));
            else if (dir == IsometricDirection.BR)
                sc = ParentSite.GetOrNull(Position + new Point3(+1, 0, 0));

            if (sc != null)
            {
                if (sc.PassAbility()) return sc;
                sc = ParentSite.GetOrNull(sc.Position + new Point3(0, 0, +1));
                if (sc != null)
                {
                    if (sc.PassAbility()) return sc;
                    sc = ParentSite.GetOrNull(sc.Position + new Point3(0, 0, -2));
                    if (sc != null)
                    {
                        if (sc.PassAbility()) return sc;
                    }
                }
            }
            return this;
        }
    }
}