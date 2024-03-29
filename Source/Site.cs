﻿using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Utils;

using Roy_T.AStar.Graphs;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;

using SharpDX.Direct2D1.Effects;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public World ECSWorld { get; private set; }
        public SiteTileContainer Blocks { get; set; }
        public Point3 Size { get; private set; }
        public Camera2D Camera { get; private set; }

        public MainWorld World { get; private set; }
        public Entity SelectedBlock { get; private set; }
        public Point3 SelectedPosition { get; private set; }

        private PathFinder _pathfinder;

        public Node startPathNode;
        public Node endPathNode;
        public Path currPath;

        private int _currentLevel;
        public int PreviousLevel { get; private set; }

        public float SiteTime = 0.5f;

        public List<Point3> BlocksToReload { get; private set; }

        public Site(MainWorld world, Point3 size)
        {
            ECSWorld = Arch.Core.World.Create();
            World = world;
            Size = size;
            Blocks = new SiteTileContainer(Size);

            CurrentLevel = (int)(Size.Z * 0.8f);

            Camera = new Camera2D();
            Camera.Move(new Vector2(0,
                -(CurrentLevel * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                    - Sprite.TILE_SIZE.Y * (Size.X / 2)
                 )));

            BlocksToReload = new List<Point3>();
        }

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (_currentLevel != value)
                {
                    PreviousLevel = _currentLevel;
                    if (value < 0) _currentLevel = 0;
                    else if (value > Size.Z - 1) _currentLevel = Size.Z - 1;
                    else _currentLevel = value;
                }
            }
        }

        public void SetSelected(Point3 pos)
        {
            if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
            {
                SelectedBlock = Entity.Null;
                SelectedPosition = Point3.Zero;
            }
            else
            {
                SelectedBlock = Blocks[pos.X, pos.Y, pos.Z];
                SelectedPosition = pos;
            }
        }

        public Entity GetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[pos.X, pos.Y, pos.Z];
            return Entity.Null;
        }

        public void Update(GameTime gameTime)
        {
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            sel = WorldUtils.MouseScreenToMapSurface(Camera, m, CurrentLevel, this);
            SetSelected(sel);
            /*EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["SelectedBlock"] = sel.ToString(),
                ["Layer"] = CurrentLevel.ToString(),
                ["DayTime"] = (SiteTime).ToString("#.##")
            }));*/
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
        }

        public void InitPathFinder()
        {
            _pathfinder = new PathFinder();

            var query = new QueryDescription().WithAll<TileHasPathNode>();
            ECSWorld.Query(in query, (ref TileHasPathNode pn) =>
            {
                var node = pn.node;
                for (int x = (int)node.Position.X - 1; x <= node.Position.X + 1; x++)
                {
                    for (int y = (int)node.Position.Y - 1; y <= node.Position.Y + 1; y++)
                    {
                        for (int z = (int)node.Position.Z - 1; z <= node.Position.Z + 1; z++)
                        {
                            if (x >= 0 && y >= 0 && z >= 0 &&
                                x < Size.X && y < Size.Y && z < Size.Z &&
                                (x != node.Position.X || y != node.Position.Y || z != node.Position.Z))
                            {
                                Velocity v = Velocity.FromMetersPerSecond(1);
                                if (x != node.Position.X && y != node.Position.Y) v = Velocity.FromMetersPerSecond(0.70710678118f);
                                if (z != node.Position.Z) v = Velocity.FromMetersPerSecond(0.333f);

                                if (Blocks[x, y, z].Has<TileHasPathNode>())
                                {
                                    Node otherNode = (Blocks[x, y, z].Get<TileHasPathNode>()).node;
                                    node.Connect(otherNode, v);
                                    //otherNode.Connect(node, Velocity.FromMetersPerSecond(1));
                                }
                            }
                        }
                    }
                }
            });
        }

        public void FindPath()
        {
            currPath = _pathfinder.FindPath(startPathNode, endPathNode, Velocity.FromMetersPerSecond(2));
            Debug.WriteLine(currPath);
        }

        public bool RemoveWall(Entity ent)
        {
            var onSite = ent.Get<OnSitePosition>();
            Point3 pos = onSite.position;
            if (Blocks[pos.X, pos.Y, pos.Z].Has<TileStructure>())
            {
                ref var structure = ref Blocks[pos.X, pos.Y, pos.Z].Get<TileStructure>();
                structure.WallMaterial = null;
                structure.WallEmbeddedMaterial = null;
                if (structure == TileStructure.Null)
                    Blocks[pos.X, pos.Y, pos.Z].Remove<TileStructure>();
                return true;
            }
            return false;
        }

        public bool RemoveFloor(Entity ent)
        {
            var onSite = ent.Get<OnSitePosition>();
            Point3 pos = onSite.position;
            if (Blocks[pos.X, pos.Y, pos.Z].Has<TileStructure>())
            {
                ref var structure = ref Blocks[pos.X, pos.Y, pos.Z].Get<TileStructure>();
                structure.FloorMaterial = null;
                structure.FloorEmbeddedMaterial = null;
                if (structure == TileStructure.Null)
                    Blocks[pos.X, pos.Y, pos.Z].Remove<TileStructure>();
                return true;
            }
            return false;
        }

        public void RemoveBlock(Entity ent)
        {
            RemoveBlock(ent);
            RemoveFloor(ent);
        }

        public void Dispose()
        {
        }
    }
}