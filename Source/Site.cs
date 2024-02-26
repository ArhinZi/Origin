using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.Generators;
using Origin.Source.Pathfind;
using Origin.Source.Render.GpuAcceleratedSpriteSystem;
using Origin.Source.Resources;
using Origin.Source.Tools;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Origin.Source
{
    public class Site : IDisposable
    {
        public MainWorld World { get; private set; }
        public SiteTileContainer Map { get; set; }
        public Point3 Size { get; private set; }

        public Camera2D Camera { get; private set; }

        public ArchWorld ArchWorld { get; private set; }

        public SiteGeneratorService MapGenerator { get; private set; }
        public SitePathfindingService Pathfinder { get; private set; }

        public SiteDrawControlService DrawControl { get; private set; }

        public SiteToolController Tools { get; private set; }

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

        public Site(MainWorld world, Point3 size)
        {
            World = world;
            Size = size;

            CurrentLevel = (int)(Size.Z * 0.8f);

            ArchWorld = ArchWorld.Create();
            Map = new SiteTileContainer(Size);

            Camera = new Camera2D();
            Camera.Position += (new Vector2(0,
                -(CurrentLevel * (GlobalResources.Settings.TileSize.Y + GlobalResources.Settings.FloorYoffset)
                    - GlobalResources.Settings.TileSize.Y * (Size.X / 2)
                 )));

            Tools = new SiteToolController(this);

            MapGenerator = new SiteGeneratorService(this, Size);
            MapGenerator.Visit(new Point3(0, 0, 127));
            Trace.WriteLine("End map gen");
        }

        public void PostInit()
        {
            Pathfinder = new SitePathfindingService(this, Size, ArchWorld);
            Trace.WriteLine("End pathfinder init");

            DrawControl = new SiteDrawControlService(this);
            Trace.WriteLine("End creating render");
        }

        public void Update(GameTime gameTime)
        {
            var query = new QueryDescription().WithAny<ConstructionRemovedEvent, ConstructionPlacedEvent>();
            ArchWorld.Destroy(query);

            Tools.Update(gameTime);

            Pathfinder.Update(gameTime);

            DrawControl.Update(gameTime);

            //SetSelected(sel);
            /*EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["SelectedBlock"] = sel.ToString(),
                ["Layer"] = CurrentLevel.ToString(),
                ["DayTime"] = (SiteTime).ToString("#.##")
            }));*/
            //SiteTime = ((float)gameTime.TotalGameTime.TotalMilliseconds % 100000) / 100000f;
        }

        public void Draw(GameTime gameTime)
        {
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
                ConstructionMetaID = GlobalResources.GetResourceMetaID<Construction>(GlobalResources.Constructions, "SoilWallFloor"),
                MaterialMetaID = GlobalResources.GetResourceMetaID<Material>(GlobalResources.Materials, "Dirt")
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
                    ConstructionMetaID = GlobalResources.GetResourceMetaID<Construction>(GlobalResources.Constructions, "SoilWallFloor"),
                    MaterialMetaID = GlobalResources.GetResourceMetaID<Material>(GlobalResources.Materials, "Dirt")
                });
            }
            ent.Add<UpdateTileRenderSelfRequest>();

            ArchWorld.Create(new ConstructionPlacedEvent()
            {
                Position = pos,
                ConstructionMetaID = bcc.ConstructionMetaID,
                MaterialMetaID = bcc.MaterialMetaID
            });
        }

        public void Dispose()
        {
        }
    }
}