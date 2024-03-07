using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.Model;
using Origin.Source.Model.Generators;
using Origin.Source.Model.Site.Tools;
using Origin.Source.Pathfind;
using Origin.Source.Render.GpuAcceleratedSpriteSystem;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source.Model.Site
{
    public class Site : IDisposable
    {
        public World World { get; private set; }

        public SiteTileContainer Map { get; set; }
        public ArchWorld ArchWorld { get; private set; }

        public Camera2D Camera { get; private set; }
        public Point3 Size { get; private set; }

        public SiteGeneratorService MapGenerator { get; private set; }
        public SitePathfindingService Pathfinder { get; private set; }

        public SiteDrawComponent DrawControl { get; private set; }

        public SiteToolsComponent Tools { get; private set; }

        private int _currentLevel;

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

        public Site(World world, Point3 size)
        {
            World = world;
            Size = size;

            CurrentLevel = (int)(Size.Z * 0.8f);

            ArchWorld = ArchWorld.Create();
            Map = new SiteTileContainer(Size);

            Camera = new Camera2D();
            Camera.Position += new Vector2(0,
                -(CurrentLevel * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset)
                    - GlobalResources.Settings.TileSize.Y * (Size.X / 2)
                 ));

            Tools = new SiteToolsComponent(this);

            MapGenerator = new SiteGeneratorService(this, Size);
            MapGenerator.Visit(new Point3(0, 0, 127));
            Trace.WriteLine("End map gen");
        }

        public void PostInit()
        {
            Pathfinder = new SitePathfindingService(this, Size, ArchWorld);
            Trace.WriteLine("End pathfinder init");

            DrawControl = new SiteDrawComponent(this);
            Trace.WriteLine("End creating render");
        }

        public void Update(GameTime gameTime)
        {
            var query = new QueryDescription().WithAny<ConstructionRemovedEvent, ConstructionPlacedEvent>();
            ArchWorld.Destroy(query);

            Tools.Update(gameTime);

            Pathfinder.Update(gameTime);

            DrawControl.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            Tools.Draw(gameTime);
            DrawControl.Draw(gameTime);
        }

        public void RemoveConstruction(Point3 pos)
        {
            MapGenerator.Visit(pos, true, true);

            if (Map.TryGet(pos, out Entity ent) && ent != Entity.Null && ent.TryGet(out BaseConstruction bcc))
            {
                ent.Remove<BaseConstruction>();

                ArchWorld.Create(new ConstructionRemovedEvent()
                {
                    Position = pos,
                    ConstructionMetaID = bcc.ConstructionMetaID,
                    MaterialMetaID = bcc.MaterialMetaID
                });

                foreach (var item in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(true))
                {
                    var pos2 = item + pos;
                    if (Map.TryGet(pos2, out Entity e) && !e.Has<UpdateTileRenderSelfRequest>())
                        e.Add<UpdateTileRenderSelfRequest>();
                }
            }
        }

        public void PlaceConstruction(Point3 pos, Construction constr, Material mat)
        {
            Entity ent = Map[pos];
            BaseConstruction bcc = new BaseConstruction()
            {
                ConstructionMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Constructions, "SoilWallFloor"),
                MaterialMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Materials, "Dirt")
            };

            if (ent.Has<BaseConstruction>() && !constr.OverAble || constr.OverAble && ent.Has<OverConstruction>())
            {
                Debug.WriteLine(string.Format("Cant place construction {0}", constr.ID));
                return;
            }
            if (!ent.Has<BaseConstruction>())
            {
                ent.Add(bcc);
            }
            else if (!ent.Has<OverConstruction>() && constr.OverAble)
            {
                throw new Exception("Something went wrong");
                ent.Add(new OverConstruction()
                {
                    ConstructionMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Constructions, "SoilWallFloor"),
                    MaterialMetaID = GlobalResources.GetResourceMetaID(GlobalResources.Materials, "Dirt")
                });
            }

            ArchWorld.Create(new ConstructionPlacedEvent()
            {
                Position = pos,
                ConstructionMetaID = bcc.ConstructionMetaID,
                MaterialMetaID = bcc.MaterialMetaID
            });
            foreach (var item in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(true))
            {
                var pos2 = item + pos;
                if (Map.TryGet(pos2, out Entity e) && !e.Has<UpdateTileRenderSelfRequest>())
                    e.Add<UpdateTileRenderSelfRequest>();
            }
        }

        public void Dispose()
        {
        }
    }
}