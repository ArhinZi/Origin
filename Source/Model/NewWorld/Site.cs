using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Origin.Source.Model.Generators;
using Origin.Source.Model.Map;
using Origin.Source.Model.Map.Light;
using Origin.Source.Model.Map.Tools;
using Origin.Source.Model.NewWorld.Map;
using Origin.Source.Model.NewWorld.Systems.Light;
using Origin.Source.Model.NewWorld.Systems.Vegetation;
using Origin.Source.Model.Pathfind;
using Origin.Source.Render;
using Origin.Source.Render.State;
using Origin.Source.Resources;
using Origin.Source.Save;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source.Model.NewWorld
{
    public class Site : IDisposable, ITickKeeper
    {
        public World World { get; private set; }
        public readonly int ID;

        public TileContainer Map { get; set; }
        public SiteRenderState RenderState { get; private set; }
        public ArchWorld ArchWorld { get; set; }

        public Camera2D Camera { get; private set; }
        public Point3 Size { get; private set; }
        public WorldRotation Rotation { get; private set; } = WorldRotation.TR;

        public SiteGeneratorService MapGenerator { get; private set; }
        public SitePathfindingComponent Pathfinder { get; private set; }

        public SiteDrawComponent DrawComponent { get; private set; }
        public LightComponent LightControl { get; private set; }
        internal SystemUpdateSunlight SunlightSystem { get; set; }
        internal SystemUpdateArtificialLight ArtificialLightSystem { get; set; }
        // Централізований ECS-системний маркер зміни середовища для рослинності.
        internal VegetationEnvironmentDirtySystem VegetationEnvironmentDirtySystem { get; set; }

        public SiteToolsComponent Tools { get; private set; }

        private int _currentLevel;
        private bool _renderDirty = true;

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

        public int PreviousLevel { get; private set; }
        public bool RenderDirty => _renderDirty;

        public Site(World world, Point3 size, int iD, bool createRenderAndTools = true, System.Action<float, string> progress = null, int generationSeed = 553, SiteGenerationSettings generationSettings = null)
        {
            World = world;
            Size = size;
            Rotation = WorldRotation.TR;

            CurrentLevel = (int)(Size.Z * 0.8f);

            ArchWorld = ArchWorld.Create();
            Map = new TileContainer(Size);
            RenderState = new SiteRenderState(Size);

            Camera = new Camera2D();
            Camera.Position += new Vector2(0,
                -(CurrentLevel * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset)
                    - GlobalResources.Settings.TileSize.Y * (Size.X / 2)
                 ));

            progress?.Invoke(0.1f, "Generating tiles");
            MapGenerator = new SiteGeneratorService(this, Size, generationSeed, generationSettings);
            MapGenerator.Visit(new Point3(0, 0, Size.Z - 1));
            Trace.WriteLine("End map gen");

            progress?.Invoke(0.45f, "Preparing light data");
            LightControl = new LightComponent(this);
            Trace.WriteLine("End light init");
            ID = iD;

            progress?.Invoke(0.6f, "Building pathfinding data");
            Pathfinder = new SitePathfindingComponent(this, Size);
            Trace.WriteLine("End pathfinder init");

            if (createRenderAndTools)
            {
                InitializeRenderAndTools();
            }
        }

        public void InitializeRenderAndTools()
        {
            if (DrawComponent == null)
            {
                DrawComponent = new SiteDrawComponent(this);
                Trace.WriteLine("End creating render");
            }

            if (Tools == null)
            {
                Tools = new SiteToolsComponent(this);
            }
        }

        public Site(World world, SaveSiteDump dump, ArchWorld arch)
        {
            World = world;
            Size = dump.Size;
            ID = dump.ID;
            CurrentLevel = dump.CurrentLevel;
            Rotation = dump.Rotation;

            ArchWorld = arch ?? ArchWorld.Create();
            Map = new TileContainer(Size);
            RenderState = new SiteRenderState(Size);

            if (dump.Camera != null)
            {
                Camera = dump.Camera;
            }
            else
            {
                Camera = new Camera2D();
                Camera.Position += new Vector2(0,
                    -(CurrentLevel * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset)
                        - GlobalResources.Settings.TileSize.Y * (Size.X / 2)
                     ));
            }

            MapGenerator = new SiteGeneratorService(this, Size, world.Seed, world.GenerationSettings);
            MapGenerator.Visit(new Point3(0, 0, Size.Z - 1));
            Trace.WriteLine("End map gen");

            LightControl = new LightComponent(this);
            Trace.WriteLine("End light init");

            Pathfinder = new SitePathfindingComponent(this, Size);
            Trace.WriteLine("End pathfinder init");

            DrawComponent = new SiteDrawComponent(this);
            Trace.WriteLine("End creating render");

            Tools = new SiteToolsComponent(this);
        }

        public void RotateLeft()
        {
            Rotation = Rotation switch
            {
                WorldRotation.TR => WorldRotation.TL,
                WorldRotation.TL => WorldRotation.BL,
                WorldRotation.BL => WorldRotation.BR,
                _ => WorldRotation.TR
            };
            InvalidateRender();
        }

        public void RotateRight()
        {
            Rotation = Rotation switch
            {
                WorldRotation.TR => WorldRotation.BR,
                WorldRotation.BR => WorldRotation.BL,
                WorldRotation.BL => WorldRotation.TL,
                _ => WorldRotation.TR
            };
            InvalidateRender();
        }

        public void InvalidateRender()
        {
            _renderDirty = true;
        }

        public void InvalidateRender(Point3 pos)
        {
            RenderState.MarkDirtyAround(pos);
        }

        public void InvalidateRender(IEnumerable<Point3> positions, bool includeNeighbours = false)
        {
            RenderState.MarkDirty(positions, includeNeighbours);
        }

        public void ClearRenderDirty()
        {
            _renderDirty = false;
            RenderState.ClearDirty();
        }

        public SaveSiteDump Dump()
        {
            SaveSiteDump ssd = new SaveSiteDump()
            {
                ID = ID,
                Camera = Camera,
                CurrentLevel = CurrentLevel,
                Size = Size,
                Rotation = Rotation
            };
            return ssd;
        }

        public void PostInit()
        {
        }

        public void Update(GameTime gameTime)
        {
            Tools?.Update(gameTime);
            DrawComponent?.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            Tools?.Draw(gameTime);
            DrawComponent?.Draw(gameTime);
        }

        public void UpdateWalkabilityAt(Point3 pos)
        {
            if (!Map.InBounds(pos))
                return;

            ref Tile tile = ref Map.GetRef(pos);
            if (!tile.Exists)
                return;

            tile.IsWalkable = false;
            tile.WalkableConstructionBelowMetaID = 0;

            if (!tile.HasConstruction && Map.InBounds(pos + Point3.Down))
            {
                Tile below = Map[pos + Point3.Down];
                if (below.Exists && below.HasConstruction)
                {
                    tile.IsWalkable = true;
                    tile.WalkableConstructionBelowMetaID = below.Construction.ConstructionMetaID;
                }
            }
        }

        public void UpdateWalkabilityAround(Point3 pos)
        {
            foreach (var offset in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
            {
                UpdateWalkabilityAt(pos + offset);
            }
        }

        private void UpdatePathAround(Point3 pos)
        {
            if (Pathfinder == null)
                return;

            foreach (var offset in WorldUtils.TOP_BOTTOM_NEIGHBOUR_PATTERN())
            {
                var nodePos = pos + offset;
                if (nodePos.InBounds(Point3.Zero, Size))
                {
                    Pathfinder.UpdatePathNode(nodePos);
                }
            }
        }

        public void RemoveConstruction(Point3 pos)
        {
            MapGenerator.Visit(pos, true, true);

            if (!Map.InBounds(pos))
                return;

            ref Tile tile = ref Map.GetRef(pos);
            if (!tile.Exists || !tile.HasConstruction)
                return;

            tile.HasConstruction = false;
            tile.IsRamp = false;
            tile.HasConstructionOver = false;
            tile.HasConstructionRotation = false;
            tile.HasConstructionShape = false;
            tile.IsFluidBlocker = false;

            UpdateWalkabilityAround(pos);
            UpdatePathAround(pos);
            UpdateVegsOnConstructionRemovedSystem.Apply(this, pos);
            SunlightSystem?.OnConstructionRemoved(pos);
            ArtificialLightSystem?.OnConstructionRemoved(pos);
            InvalidateRender(pos);
        }

        public void PlaceConstruction(Point3 pos, Construction constr, Material mat)
        {
            if (!Map.InBounds(pos))
                return;

            ref Tile tile = ref Map.GetRef(pos);
            if (!tile.Exists)
                return;

            TileConstruction bcc = new()
            {
                ConstructionMetaID = GlobalResources.Constructions.IndexOf(constr.ID),
                MaterialMetaID = GlobalResources.Materials.IndexOf(mat.ID)
            };

            if (tile.HasConstruction && !constr.OverAble || constr.OverAble && tile.HasConstructionOver)
            {
                Debug.WriteLine($"Cant place construction {constr.ID}");
                return;
            }

            if (!tile.HasConstruction)
            {
                tile.HasConstruction = true;
                tile.Construction = bcc;
                tile.IsRamp = constr.Type == "Ramp";
                tile.IsFluidBlocker = !tile.IsRamp;
            }
            else if (!tile.HasConstructionOver && constr.OverAble)
            {
                tile.HasConstructionOver = true;
                tile.ConstructionOver = new TileConstructionOver
                {
                    ConstructionMetaID = bcc.ConstructionMetaID,
                    MaterialMetaID = bcc.MaterialMetaID
                };
            }

            UpdateWalkabilityAround(pos);
            UpdatePathAround(pos);
            UpdateVegsOnConstructionPlacedSystem.Apply(this, pos);
            SunlightSystem?.OnConstructionPlaced(pos);
            ArtificialLightSystem?.OnConstructionPlaced(pos);
            InvalidateRender(pos);
        }

        public void Dispose()
        {
            ArchWorld?.Dispose();
            ArchWorld = null;
            Map = null;
            RenderState = null;
            DrawComponent = null;
            LightControl = null;
            Pathfinder = null;
            MapGenerator = null;
            Tools = null;
            GC.SuppressFinalize(this);
        }

        public void BeforeTick()
        {
        }

        public void AfterTick()
        {
        }

        public void TickTricky(int mult)
        {
        }
    }
}