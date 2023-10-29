using Arch.Core;
using Arch.Core.Extensions;
using Arch.Relationships;

using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using Roy_T.AStar.Graphs;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Point3 = Origin.Source.Utils.Point3;

namespace Origin.Source.Generators
{
    public class SiteGenController
    {
        private GraphicsDevice _device;
        private List<AbstractPass> passes;

        private Site _site;
        private int _seed = 553;

        public Point3 Size { get; private set; }

        public SiteGenController(GraphicsDevice device, Site site, Point3 size)
        {
            _device = device;
            _site = site;
            Size = size;

            passes = new List<AbstractPass>()
            {
                new SurfacePass(Size, _seed)
            };
        }

        public void Init()
        {
        }

        public void Visit(Point3 startPos, bool visitStart = true)
        {
            Dictionary<int, bool[,]> visited = new Dictionary<int, bool[,]>();
            Stack<Point3> stack = new Stack<Point3>();
            if (visitStart)
            {
                stack.Push(startPos);
            }
            else
            {
                foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L())
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
                if (_site.Blocks[pos.X, pos.Y, pos.Z] != Entity.Null)
                    continue;

                // Creating Root Entity
                Entity tileEnt = _site.ECSWorld.Create(new IsTile() { Position = pos });
                _site.Blocks[pos] = tileEnt;

                // Passes
                foreach (var pass in passes)
                {
                    pass.Pass(tileEnt, pos);
                }

                bool isAir = true;
                if (tileEnt.Has<BaseConstruction>())
                    isAir = false;
                //Checking Path Ability
                Entity tmp;
                if (isAir && _site.Blocks.TryGet(pos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && tmp.Has<BaseConstruction>())
                {
                    tileEnt.Add<TilePathAble>();
                }
                if (tileEnt.Has<BaseConstruction>() &&
                    _site.Blocks.TryGet(pos + new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && !tmp.Has<BaseConstruction>())
                {
                    tmp.Add<TilePathAble>();
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