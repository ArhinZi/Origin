using Arch.Core;
using Arch.Core.Extensions;

using MonoGame.Extended;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Render;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.Model.Generators
{
    public class SiteGeneratorService
    {
        private List<AbstractPass> passes;

        private Site.Site _site;
        private int _seed = 553;

        public Point3 Size { get; private set; }

        public SiteGeneratorService(Site.Site site, Point3 size)
        {
            _site = site;
            Size = size;

            passes =
            [
                new SurfacePass(Size, _seed)
            ];
        }

        public void Visit(Point3 startPos, bool visitStart = true, bool upd = false)
        {
            Dictionary<int, bool[,]> visited = [];
            List<Entity> visitedEnts = new List<Entity>();
            Stack<Point3> stack = new();
            foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(visitStart))
            {
                stack.Push(startPos + p);
            }

            // Fill surface tiles
            while (stack.Count > 0)
            {
                Point3 pos = stack.Pop();

                if (!visited.ContainsKey(pos.Z)) visited.Add(pos.Z, new bool[Size.X, Size.Y]);
                if (!pos.InBounds(Point3.Zero, Size))
                    continue;
                if (visited[pos.Z][pos.X, pos.Y])
                    continue;
                if (_site.Map[pos.X, pos.Y, pos.Z] != Entity.Null)
                    continue;

                // Creating Root Entity
                Entity tileEnt = _site.ArchWorld.Create(new IsTile() { Position = pos });
                _site.Map[pos] = tileEnt;
                visitedEnts.Add(tileEnt);

                if (pos.X == 1 && pos.Y == 1 && pos.Z == 93)
                {
                }

                // Passes
                foreach (var pass in passes)
                {
                    pass.Pass(tileEnt, pos);
                }

                bool isAir = true;
                if (tileEnt.Has<ConstructionBase>())
                    isAir = false;
                else
                {
                    tileEnt.Add<IsAirTile>();
                }
                //Checking Path Ability
                Entity tmp;
                if (isAir && _site.Map.TryGet(pos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && tmp.Has<ConstructionBase>())
                {
                    tileEnt.Add<IsWalkAbleTile>();
                }
                if (tileEnt.Has<ConstructionBase>() &&
                    _site.Map.TryGet(pos + new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && !tmp.Has<ConstructionBase>())
                {
                    tmp.Add<IsWalkAbleTile>();
                }

                if (upd)
                {
                    tileEnt.Add<SelfRequestUpdateTileRender>();
                }

                // Visit neighbours
                visited[pos.Z][pos.X, pos.Y] = true;
                if (isAir)
                {
                    foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(false))
                    {
                        stack.Push(pos + p);
                    }
                }
            }

            // Fill ramps
            for (int i = 0; i < visitedEnts.Count; i++)
            {
                Entity current = visitedEnts[i];
                Point3 pos = current.Get<IsTile>().Position;

                if (current.Has<ConstructionBase>()) continue;

                Entity below;
                if (!_site.Map.TryGet(pos + Point3.Down, out below)) continue;
                Entity above;
                if (!_site.Map.TryGet(pos + Point3.Up, out above)) continue;

                if (above.Has<ConstructionBase>()) continue;
                if (!below.TryGet(out ConstructionBase bcb)) continue;
                if (bcb.Construction.Type != "WallFloor") continue;

                Material rmat = below.Get<ConstructionBase>().Material;

                Entity north;
                if (!_site.Map.TryGet(pos + Point3.North, out north)) north = Entity.Null;
                Entity south;
                if (!_site.Map.TryGet(pos + Point3.South, out south)) south = Entity.Null;
                Entity west;
                if (!_site.Map.TryGet(pos + Point3.West, out west)) west = Entity.Null;
                Entity east;
                if (!_site.Map.TryGet(pos + Point3.East, out east)) east = Entity.Null;

                Point3 rdir = Point3.Zero;
                if (north != Entity.Null && (north.TryGet(out ConstructionBase ncb) && ncb.Construction.Type == "WallFloor")) rdir += Point3.North;
                if (south != Entity.Null && (south.TryGet(out ConstructionBase scb) && scb.Construction.Type == "WallFloor")) rdir += Point3.South;
                if (west != Entity.Null && (west.TryGet(out ConstructionBase wcb) && wcb.Construction.Type == "WallFloor")) rdir += Point3.West;
                if (east != Entity.Null && (east.TryGet(out ConstructionBase ecb) && ecb.Construction.Type == "WallFloor")) rdir += Point3.East;

                rdir = rdir * -1;

                Global.Direction rddir = Point3.DirByPoint(rdir);

                if (rddir != Global.Direction.NONE)
                {
                    if (rddir == Global.Direction.NORTH || rddir == Global.Direction.SOUTH || rddir == Global.Direction.EAST || rddir == Global.Direction.WEST)
                    {
                        current.Add(new ConstructionBase()
                        {
                            ConstructionID = "SoilRamp",
                            MaterialID = rmat.ID
                        });
                        current.Add(new ECS.Construction.ConstructionShape()
                        {
                            Name = "Slope"
                        });
                        current.Add(new ConstructionRotation()
                        {
                            Direction = rddir
                        });
                    }
                    else if (rddir == Global.Direction.NORTHEAST || rddir == Global.Direction.NORTHWEST ||
                        rddir == Global.Direction.SOUTHEAST || rddir == Global.Direction.SOUTHWEST)
                    {
                        current.Add(new ConstructionBase()
                        {
                            ConstructionID = "SoilRamp",
                            MaterialID = rmat.ID
                        });
                        current.Add(new ECS.Construction.ConstructionShape()
                        {
                            Name = "CornerIn"
                        });
                        current.Add(new ConstructionRotation()
                        {
                            Direction = rddir
                        });
                    }
                    else
                    {
                    }
                }
                else
                {
                    //if (_site.Map.TryGet(pos + Point3.NorthWest, out Entity nw) && nw != Entity.Null && (nw.TryGet(out ConstructionBase nwcb) && nwcb.Construction.Type == "WallFloor"))
                    //    rddir = Global.Direction.SOUTHEAST;
                    //else if (_site.Map.TryGet(pos + Point3.NorthEast, out Entity ne) && ne != Entity.Null && (ne.TryGet(out ConstructionBase necb) && necb.Construction.Type == "WallFloor"))
                    //    rddir = Global.Direction.SOUTHWEST;
                    //else if (_site.Map.TryGet(pos + Point3.SouthWest, out Entity sw) && sw != Entity.Null && (sw.TryGet(out ConstructionBase swcb) && swcb.Construction.Type == "WallFloor"))
                    //    rddir = Global.Direction.NORTHEAST;
                    //else if (_site.Map.TryGet(pos + Point3.SouthEast, out Entity se) && se != Entity.Null && (se.TryGet(out ConstructionBase secb) && secb.Construction.Type == "WallFloor"))
                    //    rddir = Global.Direction.NORTHWEST;

                    //if (rddir != Global.Direction.NONE)
                    //{
                    //    current.Add(new ConstructionBase()
                    //    {
                    //        ConstructionID = "SoilRamp",
                    //        MaterialID = rmat.ID
                    //    });
                    //    current.Add(new ECS.Construction.ConstructionShape()
                    //    {
                    //        Name = "CornerOut"
                    //    });
                    //    current.Add(new ConstructionRotation()
                    //    {
                    //        Direction = rddir
                    //    });
                    //}
                }
            }
        }
    }
}