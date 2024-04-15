using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;

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

using static Origin.Source.Resources.Global;

namespace Origin.Source.ECS.Render
{
    public class SystemUpdateRenderTiles : TickSystem
    {
        private Sprite lborderSprite = GlobalResources.Sprites["LeftBorder"];
        private Sprite rborderSprite = GlobalResources.Sprites["RightBorder"];
        private Color borderColor = new(0, 0, 0, 100);

        private Site site;
        private SiteRenderer renderer;

        public SystemUpdateRenderTiles(Site site) : base(site)
        {
            this.site = site;
            renderer = site.DrawComponent.SiteRenderer;
        }

        public override void Initialize()
        {
            // INIT MAIN SPRITES
            List<RenderData> list = new List<RenderData>();
            var commands = new CommandBuffer();

            var construction = new QueryDescription().WithAll<IsTile, ConstructionBase>();
            site.ArchWorld.Add<SpriteLocatorsConstruction>(construction);
            _site.ArchWorld.Query(in construction,
                (Entity ent, ref IsTile tile, ref ConstructionBase bcc, ref SpriteLocatorsConstruction locators) =>
            {
                locators = InitAdd<SpriteLocatorsConstruction>(GetConstructionRenderData(ent, ref tile, ref bcc));
            });

            var fluid = new QueryDescription().WithAll<IsTile, FluidParticle>();
            site.ArchWorld.Add<SpriteLocatorsFluid>(fluid);
            _site.ArchWorld.Query(in fluid,
                (Entity ent, ref IsTile tile, ref FluidParticle fluid, ref SpriteLocatorsFluid locators) =>
            {
                locators = InitAdd<SpriteLocatorsFluid>(GetFluidRenderData(ent, ref tile, ref fluid));
            });

            renderer.StaticDrawer.SetChunks();

            // INIT HIDDEN SPRITES
            for (int z = 0; z < site.Size.Z; z++)
                for (int x = 0; x < site.Size.X; x++)
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new(x, y, z);
                        Entity tile = site.Map[tilePos];

                        if (tile == Entity.Null)
                        {
                            renderer.HiddenDrawer.MakeHidden(tilePos);
                        }
                    }
            renderer.HiddenDrawer.Set();
        }

        public override void Update(in ulong t)
        {
            // REMOVE
            var clear = new QueryDescription().WithAll<SelfRequestUpdateTileRender, IsTile>();
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
            var construction = new QueryDescription().WithAll<SelfRequestUpdateTileRender, IsTile, ConstructionBase>();
            var fluid = new QueryDescription().WithAll<SelfRequestUpdateTileRender, IsTile, FluidParticle>();

            site.ArchWorld.Query(in construction, (Entity ent, ref IsTile tile, ref ConstructionBase bcc) =>
            {
                var data = UpdateAdd<SpriteLocatorsConstruction>(GetConstructionRenderData(ent, ref tile, ref bcc));
                if (ent.Has<SpriteLocatorsConstruction>())
                    commands.Set(ent, data);
                else
                    commands.Add(ent, data);
            });
            site.ArchWorld.Query(in fluid, (Entity ent, ref IsTile tile, ref FluidParticle fluid) =>
            {
                var data = UpdateAdd<SpriteLocatorsFluid>(GetFluidRenderData(ent, ref tile, ref fluid));
                if (ent.Has<SpriteLocatorsFluid>())
                    commands.Set(ent, data);
                else
                    commands.Add(ent, data);
            });

            renderer.StaticDrawer.AddSprites();
            renderer.HiddenDrawer.Set();

            site.ArchWorld.Remove<SelfRequestUpdateTileRender>(construction);
            site.ArchWorld.Remove<SelfRequestUpdateTileRender>(fluid);
            commands.Playback(site.ArchWorld);
        }

        private T InitAdd<T>(List<RenderData> data) where T : BaseSpriteLocatorsContainer, new()
        {
            T locators = new();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.AddTileSprite(data[i]));
            }
            return locators;
        }

        private T UpdateAdd<T>(List<RenderData> data) where T : BaseSpriteLocatorsContainer, new()
        {
            T locators = new();
            for (int i = 0; i < data.Count; i++)
            {
                locators.List.Add(renderer.StaticDrawer.ScheduleUpdate(data[i]));
            }
            return locators;
        }

        private List<RenderData> GetConstructionRenderData(Entity ent, ref IsTile tile, ref ConstructionBase bcc)
        {
            var list = new List<RenderData>();

            Point3 tilePos = tile.Position;
            var constr = bcc.Construction;
            Material mat = bcc.Material;
            float zoff = 0;
            int rand = Global.World.Random.Next();
            {
                byte LAYER = (int)DrawBufferLayer.Back;
                string spart = "Wall";
                Sprite sprite;
                if (ent.TryGet<Construction.ConstructionShape>(out var ccs))
                {
                    var sprs = constr.Shapes[ccs.Name].Sprites;
                    sprite = sprs[spart][rand % sprs[spart].Count];
                    if (ent.TryGet<ConstructionRotation>(out var rot))
                    {
                        var dlist = sprite.GetSpritesByDir(rot.Direction);
                        sprite = dlist[rand % dlist.Count];
                    }
                }
                else
                {
                    sprite = constr.Sprites[spart][rand % constr.Sprites[spart].Count];
                }

                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(LAYER, tilePos, sprite, col, new Vector3(0, 0, zoff)));

                // Draw borders of Wall
                if (bcc.Construction.Type == "WallFloor")
                {
                    LAYER = (int)DrawBufferLayer.BackNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                            (!tmp.TryGet<ConstructionBase>(out ConstructionBase tmpbcc) || tmpbcc.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(LAYER, tilePos, lborderSprite, borderColor,
                            new Vector3(0, 0, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            (!tmp.TryGet<ConstructionBase>(out tmpbcc) || tmpbcc.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(LAYER, tilePos, rborderSprite, borderColor,
                            new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0)));
                }
            }
            {
                byte LAYER = (int)DrawBufferLayer.Front;
                string spart = "Floor";
                Sprite sprite;
                if (ent.TryGet<Construction.ConstructionShape>(out var ccs))
                {
                    var sprs = constr.Shapes[ccs.Name].Sprites;
                    sprite = sprs[spart][rand % sprs[spart].Count];
                    if (ent.TryGet<ConstructionRotation>(out var rot))
                    {
                        var dlist = sprite.GetSpritesByDir(rot.Direction);
                        sprite = dlist[rand % dlist.Count];
                    }
                }
                else
                {
                    sprite = constr.Sprites[spart][rand % constr.Sprites[spart].Count];
                }
                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                // Draw borders of Floor
                if (bcc.Construction.Type == "WallFloor")
                {
                    LAYER = (int)DrawBufferLayer.FrontNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                                (!tmp.TryGet<ConstructionBase>(out ConstructionBase tmpbcc) || tmpbcc.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(LAYER, tilePos, lborderSprite, borderColor,
                            new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            (!tmp.TryGet<ConstructionBase>(out tmpbcc) || tmpbcc.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(LAYER, tilePos, rborderSprite, borderColor,
                            new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset, 0)));
                }

                // Draw Vegetation
                LAYER = (int)DrawBufferLayer.FrontOver;
                if (ent.TryGet(out BaseVegetation hveg) && ent.Has<GrownUpVegetation>())
                {
                    var veg = GlobalResources.Vegetations[hveg.VegetationMetaID];
                    List<string> spritesIDs;
                    if (Resources.Vegetation.VegetationSpritesByConstrCategory.TryGetValue((veg, constr.ID), out spritesIDs))
                        sprite = GlobalResources.Sprites[spritesIDs[rand % spritesIDs.Count]];
                    else if (Resources.Vegetation.VegetationSpritesByConstruction.TryGetValue((veg, constr.ID), out spritesIDs))
                        sprite = GlobalResources.Sprites[spritesIDs[rand % spritesIDs.Count]];

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
                float zoff = 0;
                if ((site.Map.TryGet(tilePos + new Utils.Point3(0, 1, 0), out var n1ent) && n1ent.Has<FluidParticle>()) &&
                    (site.Map.TryGet(tilePos + new Utils.Point3(1, 0, 0), out var n2ent) && n2ent.Has<FluidParticle>())) zoff = Global.Z_DIAGONAL_OFFSET;
                //else if (site.Map.TryGet(tilePos + new Utils.Point3(1, 0, 0), out var n2ent) && n2ent.Has<IsRamp>()) zoff = Global.Z_DIAGONAL_OFFSET;

                byte LAYER = (int)DrawBufferLayer.Water;
                Sprite sprite = GlobalResources.Sprites["Water"];
                Color col = Color.Blue;
                col.A = (byte)(255 - Math.Pow((FluidParticle.MaxVolume - fluid.Volume), 1.2));
                list.Add(new RenderData(LAYER, tilePos, sprite, col,
                                new Vector3(0, (FluidParticle.MaxVolume - fluid.Volume) / 2, zoff)));

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