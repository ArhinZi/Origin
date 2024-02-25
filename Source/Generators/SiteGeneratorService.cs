using Arch.Core;
using Arch.Core.Extensions;

using MonoGame.Extended;

using Origin.Source.Components;
using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.Generators
{
    public class SiteGeneratorService
    {
        private List<AbstractPass> passes;

        private Site _site;
        private int _seed = 553;

        public Point3 Size { get; private set; }

        public SiteGeneratorService(Site site, Point3 size)
        {
            _site = site;
            Size = size;

            passes = new List<AbstractPass>()
            {
                new SurfacePass(Size, _seed)
            };
        }

        public void Visit(Point3 startPos, bool visitStart = true, bool upd = false)
        {
            Dictionary<int, bool[,]> visited = new Dictionary<int, bool[,]>();
            Stack<Point3> stack = new Stack<Point3>();
            if (visitStart)
            {
                stack.Push(startPos);
            }
            else
            {
                foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(false))
                {
                    stack.Push(startPos + p);
                }
            }

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

                if (pos.X == 1 && pos.Y == 1 && pos.Z == 93)
                {
                }

                // Passes
                foreach (var pass in passes)
                {
                    pass.Pass(tileEnt, pos);
                }

                bool isAir = true;
                if (tileEnt.Has<BaseConstruction>())
                    isAir = false;
                else
                {
                    tileEnt.Add<IsAirTile>();
                }
                //Checking Path Ability
                Entity tmp;
                if (isAir && _site.Map.TryGet(pos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && tmp.Has<BaseConstruction>())
                {
                    tileEnt.Add<IsWalkAbleTile>();
                }
                if (tileEnt.Has<BaseConstruction>() &&
                    _site.Map.TryGet(pos + new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && !tmp.Has<BaseConstruction>())
                {
                    tmp.Add<IsWalkAbleTile>();
                }

                if (upd)
                {
                    tileEnt.Add<ECS.UpdateTileRenderSelfRequest>();
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
        }
    }
}