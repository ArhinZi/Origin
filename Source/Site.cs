using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Relationships;

using info.lundin.math;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Generators;
using Origin.Source.Pathfind;
using Origin.Source.Resources;
using Origin.Source.Tools;
using Origin.Source.Utils;

using Roy_T.AStar.Primitives;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public World ECSWorld { get; private set; }
        public SiteTileContainer Blocks { get; set; }
        public Point3 Size { get; private set; }
        public Camera2D Camera { get; private set; }
        public MainWorld World { get; private set; }

        public SiteToolController Tools { get; private set; }
        public SiteGenController Generator { get; private set; }

        //public Entity SelectedBlock { get; private set; }

        /*private PathFinder _pathfinder;
        public Node startPathNode;
        public Node endPathNode;
        public Roy_T.AStar.Paths.Path currPath;*/

        private PathfinderSystem _pathfinderSystem;
        public Point3 startPath;
        public Point3 endPath;
        public List<Point3> currPath2;

        private int _currentLevel;
        public int PreviousLevel { get; private set; }

        public float SiteTime = 0.5f;

        public Texture2D hmt;

        public Site(MainWorld world, Point3 size)
        {
            ECSWorld = Arch.Core.World.Create();
            World = world;
            Size = size;
            Blocks = new SiteTileContainer(Size);

            CurrentLevel = (int)(Size.Z * 0.8f);

            Camera = new Camera2D();
            Camera.Position += (new Vector2(0,
                -(CurrentLevel * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset)
                    - GlobalResources.Settings.TileSize.Y * (Size.X / 2)
                 )));

            Tools = new(this);

            Generator = new SiteGenController(OriginGame.Instance.GraphicsDevice, this, Size);
            Generator.Visit(new Utils.Point3(0, 0, 127));

            //hmt = Generator.HeightMapToTexture2D(10);

            InitPathFinder();
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

        /*public void SetSelected(Point3 pos)
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
        }*/

        public Entity GetOrNull(Point3 pos)
        {
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 &&
                pos.X < Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
                return Blocks[pos.X, pos.Y, pos.Z];
            return Entity.Null;
        }

        public void Update(GameTime gameTime)
        {
            Tools.Update(gameTime);
            Point m = Mouse.GetState().Position;
            Point3 sel = WorldUtils.MouseScreenToMap(Camera, m, CurrentLevel);
            sel = WorldUtils.MouseScreenToMapSurface(Camera, m, CurrentLevel, this);

            if (ECSWorld.CountEntities(new QueryDescription().WithAll<WaitingForUpdateTileLogic>()) > 0)
            {
                // Update pathes
                var query = new QueryDescription().WithAll<WaitingForUpdateTileLogic, IsTile>();
                var commands = new CommandBuffer(ECSWorld);
                var visited = new HashSet<Point3>();
                ECSWorld.Query(in query, (Entity entity, ref IsTile rootComp) =>
                {
                    var p = rootComp.Position;

                    foreach (var n in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
                    {
                        Point3 nPos = p + n;
                        Entity rootN;
                        if (Blocks.TryGet(nPos, out rootN) && rootN != Entity.Null)
                        {
                            // Remove path if Construction is on Tile
                            if (rootN.Has<BaseConstruction>())
                            {
                                commands.Remove<TilePathAble>(rootN);
                            }
                            else
                            {
                                // Check a construction under the Tile
                                Entity tmp;
                                if (Blocks.TryGet(nPos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null)
                                {
                                    if (tmp.Has<BaseConstruction>())
                                    {
                                        commands.Add<TilePathAble>(rootN);
                                    }
                                    else
                                    {
                                        commands.Remove<TilePathAble>(rootN);
                                    }
                                }
                            }
                        }
                        visited.Add(nPos);
                        commands.Remove<WaitingForUpdateTileLogic>(entity);
                    }
                });
                commands.Playback();
                foreach (var item in visited)
                {
                    UpdatePathNode(item);
                }
            }
            //SetSelected(sel);
            /*EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["SelectedBlock"] = sel.ToString(),
                ["Layer"] = CurrentLevel.ToString(),
                ["DayTime"] = (SiteTime).ToString("#.##")
            }));*/
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
        }

        private void InitPathFinder()
        {
            var query = new QueryDescription().WithAll<TilePathAble, IsTile>();

            _pathfinderSystem = new PathfinderSystem();
            ECSWorld.Query(in query, (ref TilePathAble pn, ref IsTile tile) =>
            {
                Point3 pos = tile.Position;
                SetPathNode(pos);
            });
        }

        private void SetPathNode(Point3 pos)
        {
            if (Blocks[pos.X, pos.Y, pos.Z] != Entity.Null && Blocks[pos.X, pos.Y, pos.Z].Has<TilePathAble>())
            {
                if (!_pathfinderSystem.HasNode(pos))
                    _pathfinderSystem.AddNode(pos, 1);

                int i = 0;
                for (int x = pos.X - 1; x <= pos.X + 1; x++)
                {
                    for (int y = pos.Y - 1; y <= pos.Y + 1; y++)
                    {
                        for (int z = pos.Z - 1; z <= pos.Z + 1; z++)
                        {
                            if (x >= 0 && y >= 0 && z >= 0 &&
                                x < Size.X && y < Size.Y && z < Size.Z &&
                                (x != pos.X || y != pos.Y || z != pos.Z))
                            {
                                float v = 1;
                                if (x != pos.X && y != pos.Y) v = 1.414f;
                                if (z != pos.Z) v = 3;

                                if (Blocks[x, y, z] != Entity.Null && Blocks[x, y, z].Has<TilePathAble>())
                                {
                                    Point3 otherPos = new Point3(x, y, z);

                                    if (!_pathfinderSystem.HasNode(otherPos))
                                        _pathfinderSystem.AddNode(otherPos, 1);
                                    _pathfinderSystem.AddNodeIntersections(pos,
                                        new RelationWith[] {new RelationWith() {
                                            position = otherPos ,
                                            traversalType = TraversalTypes.Walk,
                                            cost = v
                                        } });
                                    i++;
                                    //otherNode.Connect(node, Velocity.FromMetersPerSecond(1));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdatePathNode(Point3 pos)
        {
            if (_pathfinderSystem.HasNode(pos))
            {
                _pathfinderSystem.RemoveNode(pos);
            }
            SetPathNode(pos);
        }

        public PathInfo FindPath(Point3 start, Point3 end, bool debug = false)
        {
            long a, b;
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            //List<Point3> path;
            //for (int i = 0; i < 10; i++)

            var currPath2 = _pathfinderSystem.FindPath(start,
            end, debug);

            watch.Stop();
            b = watch.ElapsedMilliseconds;
            if (currPath2 != null)
                Debug.WriteLine(String.Format("Path Found with Len={0} in {1}ms looked {2} Nodes", currPath2.path.Count, b.ToString(), _pathfinderSystem.LastVisitedCount));
            return currPath2;
        }

        public void RemoveBlock(Point3 pos)
        {
            Generator.Visit(pos, false);

            Entity ent;
            if (Blocks.TryGet(pos, out ent) && ent == Entity.Null)
            {
                ent = Blocks[pos];
            }

            if (ent.Has<BaseConstruction>())
            {
                ent.Remove<BaseConstruction>();
                ent.Add<WaitingForUpdateTileLogic, WaitingForUpdateTileRender>();
            }

            /*RemoveWall(pos);
            RemoveFloor(pos);*/
        }

        public void Dispose()
        {
        }
    }
}