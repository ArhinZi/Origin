using Arch.Core;
using Arch.Core.Extensions;
using Arch.Bus;

using Microsoft.Xna.Framework;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Render;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;
using Arch.Buffer;
using System.Collections;

namespace Origin.Source.ECS.Render
{
    // TODO Fix grass. Show when does not
    public class RenderUpdateTilesSystem : TickSystem
    {
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new(0, 0, 0, 100);

        private Site site;
        private SiteRenderer renderer;

        public RenderUpdateTilesSystem(Site site) : base(site)
        {
            this.site = site;
            renderer = site.DrawComponent.SiteRenderer;
        }

        public override void Initialize()
        {
            List<RenderData> list = new List<RenderData>();
            var commands = new CommandBuffer();

            var construction = new QueryDescription().WithAll<IsTile, BaseConstruction>();
            site.ArchWorld.Add<SpriteLocatorsConstruction>(construction);
            _site.ArchWorld.Query(in construction,
                (Entity ent, ref IsTile tile, ref BaseConstruction bcc, ref SpriteLocatorsConstruction locators) =>
            {
                locators = InitAddConstruction(GetConstructionRenderData(ent, ref tile, ref bcc));
            });

            var fluid = new QueryDescription().WithAll<IsTile, FluidParticle>();
            site.ArchWorld.Add<SpriteLocatorsFluid>(fluid);
            _site.ArchWorld.Query(in fluid,
                (Entity ent, ref IsTile tile, ref FluidParticle fluid, ref SpriteLocatorsFluid locators) =>
            {
                locators = InitAddFluid(GetFluidRenderData(ent, ref tile, ref fluid));
            });

            renderer.StaticDrawer.SetChunks();
        }

        public override void Update(in ulong t)
        {
            // REMOVE
            var clear = new QueryDescription().WithAll<UpdateTileRenderSelfRequest, IsTile>();
            _site.ArchWorld.Query(in clear, (Entity ent, ref IsTile tile) =>
            {
                var item = tile.Position;
                if (ent.TryGet(out SpriteLocatorsConstruction locatorsc))
                {
                    renderer.StaticDrawer.ScheduleRemove(locatorsc.List, item);
                    locatorsc.List.Clear();
                }
                if (ent.TryGet(out SpriteLocatorsFluid locatorsf))
                {
                    renderer.StaticDrawer.ScheduleRemove(locatorsf.List, item);
                    locatorsf.List.Clear();
                }
                renderer.HiddenDrawer.ClearHidden(item);
            });

            renderer.StaticDrawer.RemoveSprites();

            // ADD
            var commands = new CommandBuffer();
            var construction = new QueryDescription().WithAll<UpdateTileRenderSelfRequest, IsTile, BaseConstruction>();
            var fluid = new QueryDescription().WithAll<UpdateTileRenderSelfRequest, IsTile, FluidParticle>();

            site.ArchWorld.Query(in construction, (Entity ent, ref IsTile tile, ref BaseConstruction bcc) =>
            {
                var data = UpdateAddConstruction(GetConstructionRenderData(ent, ref tile, ref bcc));
                if (ent.Has<SpriteLocatorsConstruction>())
                    commands.Set(ent, data);
                else
                    commands.Add(ent, data);
            });
            site.ArchWorld.Query(in fluid, (Entity ent, ref IsTile tile, ref FluidParticle fluid) =>
            {
                var data = UpdateAddFluid(GetFluidRenderData(ent, ref tile, ref fluid));
                if (ent.Has<SpriteLocatorsFluid>())
                    commands.Set(ent, data);
                else
                    commands.Add(ent, data);
            });

            renderer.StaticDrawer.AddSprites();
            renderer.HiddenDrawer.Set();

            site.ArchWorld.Remove<UpdateTileRenderSelfRequest>(construction);
            site.ArchWorld.Remove<UpdateTileRenderSelfRequest>(fluid);
            commands.Playback(site.ArchWorld);
        }

        private SpriteLocatorsConstruction InitAddConstruction(List<RenderData> data)
        {
            SpriteLocatorsConstruction locators = new SpriteLocatorsConstruction();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.AddTileSprite(data[i]));
            }
            return locators;
        }

        private SpriteLocatorsFluid InitAddFluid(List<RenderData> data)
        {
            SpriteLocatorsFluid locators = new SpriteLocatorsFluid();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.AddTileSprite(data[i]));
            }
            return locators;
        }

        private SpriteLocatorsConstruction UpdateAddConstruction(List<RenderData> data)
        {
            SpriteLocatorsConstruction locators = new SpriteLocatorsConstruction();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.ScheduleUpdate(data[i]));
            }
            return locators;
        }

        private SpriteLocatorsFluid UpdateAddFluid(List<RenderData> data)
        {
            SpriteLocatorsFluid locators = new SpriteLocatorsFluid();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.ScheduleUpdate(data[i]));
            }
            return locators;
        }

        private List<RenderData> GetConstructionRenderData(Entity ent, ref IsTile tile, ref BaseConstruction bcc)
        {
            var list = new List<RenderData>();

            Point3 tilePos = tile.Position;
            var constr = bcc.Construction;
            Material mat = bcc.Material;
            int rand = Global.World.Random.Next();
            {
                byte LAYER = (int)DrawBufferLayer.Back;
                string spart = "Wall";
                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(LAYER, tilePos, sprite, col, Vector3.Zero));

                // Draw borders of Wall
                LAYER = (int)DrawBufferLayer.BackNoLight;
                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                        !tmp.Has<BaseConstruction>())
                    list.Add(new RenderData(LAYER, tilePos, lborderSprite, borderColor,
                        new Vector3(0, 0, 0)));
                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                        !tmp.Has<BaseConstruction>())
                    list.Add(new RenderData(LAYER, tilePos, rborderSprite, borderColor,
                        new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0)));
            }
            {
                byte LAYER = (int)DrawBufferLayer.Front;
                string spart = "Floor";
                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                        constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                // Draw borders of Floor
                LAYER = (int)DrawBufferLayer.FrontNoLight;
                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                    list.Add(new RenderData(LAYER, tilePos, lborderSprite, borderColor,
                        new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                        !tmp.Has<BaseConstruction>())
                    list.Add(new RenderData(LAYER, tilePos, rborderSprite, borderColor,
                        new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset, 0)));

                // Draw Vegetation
                LAYER = (int)DrawBufferLayer.FrontOver;
                if (ent.TryGet(out BaseVegetation hveg) && ent.Has<GrownUpVegetation>())
                {
                    var veg = GlobalResources.Vegetations[hveg.VegetationMetaID];
                    List<string> spritesIDs;
                    if (Resources.Vegetation.VegetationSpritesByConstrCategory.TryGetValue((veg, constr.ID), out spritesIDs))
                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);
                    else if (Resources.Vegetation.VegetationSpritesByConstruction.TryGetValue((veg, constr.ID), out spritesIDs))
                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);

                    if (spritesIDs != null)
                        list.Add(new RenderData(LAYER, tilePos, sprite, Color.White,
                            new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                }
            }

            return list;
        }

        public List<RenderData> GetFluidRenderData(Entity ent, ref IsTile tile, ref FluidParticle fluid)
        {
            var list = new List<RenderData>();

            Point3 tilePos = tile.Position;

            //ref SpriteLocatorsStatic locators = ref ent.Get<SpriteLocatorsStatic>();
            if (fluid.Volume > 0)
            {
                byte LAYER = (int)DrawBufferLayer.Water;
                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                "Water");
                Color col = Color.Blue;
                col.A = (byte)(255 - Math.Pow((FluidParticle.MaxVolume - fluid.Volume), 1.2));
                list.Add(new RenderData(LAYER, tilePos, sprite, col,
                                new Vector3(0, (FluidParticle.MaxVolume - fluid.Volume) / 2, 0)));

                //sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                //                "SolidWall");
                //col = Color.Blue;
                //col.A = (byte)(255 - (FluidParticle.MaxVolume - fluid.Volume)*2);
                //locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, col,
                //                new Vector3(0, (FluidParticle.MaxVolume - fluid.Volume) / 2, 0)));
            }

            return list;
        }
    }
}