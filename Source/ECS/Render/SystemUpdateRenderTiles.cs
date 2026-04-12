using Microsoft.Xna.Framework;
using Origin.Source.ECS;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld;
using Tile = Origin.Source.Model.NewWorld.Tile;
using Origin.Source.Render;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;

namespace Origin.Source.ECS.Render
{
    public class SystemUpdateRenderTiles : TickSystem
    {
        private Sprite lborderSprite = GlobalResources.Sprites["LeftBorder"];
        private Sprite rborderSprite = GlobalResources.Sprites["RightBorder"];
        private Color borderColor = new(0, 0, 0, 100);

        private Origin.Source.Model.Map.Site site;
        private SiteRenderer renderer;

        public SystemUpdateRenderTiles(Origin.Source.Model.Map.Site site) : base(site)
        {
            this.site = site;
            renderer = site.DrawComponent.SiteRenderer;
        }

        public override void Initialize()
        {
            RebuildAll();
        }

        public override void LoadInit()
        {
            base.LoadInit();
            RebuildAll();
        }

        public override void Draw(GameTime gameTime)
        {
            if (site.RenderDirty)
            {
                RebuildAll();
            }
            else if (site.RenderState.HasDirtyTiles)
            {
                UpdateDirtyTiles();
            }
        }

        private void RebuildAll()
        {
            renderer.StaticDrawer.ResetAll();
            renderer.HiddenDrawer.ResetAll();
            site.RenderState.Clear();

            for (int z = 0; z < site.Size.Z; z++)
            {
                for (int x = 0; x < site.Size.X; x++)
                {
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new(x, y, z);
                        Tile tile = site.Map[tilePos];

                        if (!tile.Exists)
                        {
                            renderer.HiddenDrawer.MakeHidden(tilePos);
                            continue;
                        }

                        var renderState = site.RenderState.GetOrCreate(tilePos);

                        if (tile.HasConstruction)
                        {
                            foreach (var data in GetConstructionRenderData(tilePos, tile))
                            {
                                renderState.ConstructionLocators.Add(renderer.StaticDrawer.AddTileSprite(data));
                            }
                        }

                        if (tile.HasFluid)
                        {
                            foreach (var data in GetFluidRenderData(tilePos, tile))
                            {
                                renderState.FluidLocators.Add(renderer.StaticDrawer.AddTileSprite(data));
                            }
                        }

                        if (tile.HasVegetation && tile.Vegetation.IsGrown)
                        {
                            foreach (var data in GetVegetationRenderData(tilePos, tile))
                            {
                                renderState.VegetationLocators.Add(renderer.StaticDrawer.AddTileSprite(data));
                            }
                        }

                        if (!renderState.HasAny)
                        {
                            site.RenderState.Remove(tilePos);
                        }
                    }
                }
            }

            renderer.StaticDrawer.SetChunks();
            renderer.HiddenDrawer.Set();
            site.ClearRenderDirty();
        }

        private void UpdateDirtyTiles()
        {
            bool removeDirty = false;
            bool addDirty = false;
            bool hiddenDirty = false;
            var dirtyTiles = site.RenderState.ConsumeDirtyTiles();

            foreach (var tilePos in dirtyTiles)
            {
                if (!site.Map.InBounds(tilePos))
                    continue;

                if (site.RenderState.TryGet(tilePos, out var state))
                {
                    if (state.ConstructionLocators.Count > 0)
                    {
                        renderer.StaticDrawer.ScheduleRemove(state.ConstructionLocators, tilePos);
                        state.ConstructionLocators.Clear();
                        removeDirty = true;
                    }
                    if (state.FluidLocators.Count > 0)
                    {
                        renderer.StaticDrawer.ScheduleRemove(state.FluidLocators, tilePos);
                        state.FluidLocators.Clear();
                        removeDirty = true;
                    }
                    if (state.VegetationLocators.Count > 0)
                    {
                        renderer.StaticDrawer.ScheduleRemove(state.VegetationLocators, tilePos);
                        state.VegetationLocators.Clear();
                        removeDirty = true;
                    }
                }

                renderer.HiddenDrawer.ClearHidden(tilePos);
                hiddenDirty = true;
            }

            if (removeDirty)
            {
                renderer.StaticDrawer.RemoveSprites();
            }

            foreach (var tilePos in dirtyTiles)
            {
                if (!site.Map.InBounds(tilePos))
                    continue;

                Tile tile = site.Map[tilePos];
                if (!tile.Exists)
                {
                    renderer.HiddenDrawer.MakeHidden(tilePos);
                    site.RenderState.Remove(tilePos);
                    hiddenDirty = true;
                    continue;
                }

                var state = site.RenderState.GetOrCreate(tilePos);

                if (tile.HasConstruction)
                {
                    foreach (var data in GetConstructionRenderData(tilePos, tile))
                    {
                        state.ConstructionLocators.Add(renderer.StaticDrawer.ScheduleUpdate(data));
                        addDirty = true;
                    }
                }

                if (tile.HasFluid)
                {
                    foreach (var data in GetFluidRenderData(tilePos, tile))
                    {
                        state.FluidLocators.Add(renderer.StaticDrawer.ScheduleUpdate(data));
                        addDirty = true;
                    }
                }

                if (tile.HasVegetation && tile.Vegetation.IsGrown)
                {
                    foreach (var data in GetVegetationRenderData(tilePos, tile))
                    {
                        state.VegetationLocators.Add(renderer.StaticDrawer.ScheduleUpdate(data));
                        addDirty = true;
                    }
                }

                if (!state.HasAny)
                {
                    site.RenderState.Remove(tilePos);
                }
            }

            if (addDirty)
            {
                renderer.StaticDrawer.AddSprites();
            }

            if (hiddenDirty)
            {
                renderer.HiddenDrawer.Set();
            }
        }

        private List<RenderData> GetConstructionRenderData(Point3 tilePos, Tile tile)
        {
            var list = new List<RenderData>();
            var constr = tile.Construction.Construction;
            Material mat = tile.Construction.Material;
            int rand = Math.Abs(HashCode.Combine(tilePos.X, tilePos.Y, tilePos.Z));

            {
                byte layer = (int)Global.DrawBufferLayer.Back;
                string spart = "Wall";
                Sprite sprite;
                if (tile.HasConstructionShape)
                {
                    var sprs = constr.Shapes[tile.ConstructionShape.Name].Sprites;
                    sprite = sprs[spart][rand % sprs[spart].Count];
                    if (tile.HasConstructionRotation)
                    {
                        var dlist = sprite.GetSpritesByDir(WorldUtils.RotateDirection(tile.ConstructionRotation.Direction, site.Rotation));
                        sprite = dlist[rand % dlist.Count];
                    }
                }
                else
                {
                    sprite = constr.Sprites[spart][rand % constr.Sprites[spart].Count];
                }

                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(layer, tilePos, sprite, col, Vector3.Zero));

                if (tile.Construction.Construction.Type == "WallFloor")
                {
                    layer = (int)Global.DrawBufferLayer.BackNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Tile tmp) && tmp.Exists && (!tmp.HasConstruction || tmp.Construction.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(layer, tilePos, lborderSprite, borderColor, Vector3.Zero));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp.Exists && (!tmp.HasConstruction || tmp.Construction.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(layer, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0)));
                }
            }
            {
                byte layer = (int)Global.DrawBufferLayer.Front;
                string spart = "Floor";
                Sprite sprite;
                if (tile.HasConstructionShape)
                {
                    var sprs = constr.Shapes[tile.ConstructionShape.Name].Sprites;
                    sprite = sprs[spart][rand % sprs[spart].Count];
                    if (tile.HasConstructionRotation)
                    {
                        var dlist = sprite.GetSpritesByDir(WorldUtils.RotateDirection(tile.ConstructionRotation.Direction, site.Rotation));
                        sprite = dlist[rand % dlist.Count];
                    }
                }
                else
                {
                    sprite = constr.Sprites[spart][rand % constr.Sprites[spart].Count];
                }
                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                list.Add(new RenderData(layer, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                if (tile.Construction.Construction.Type == "WallFloor")
                {
                    layer = (int)Global.DrawBufferLayer.FrontNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Tile tmp) && tmp.Exists && (!tmp.HasConstruction || tmp.Construction.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(layer, tilePos, lborderSprite, borderColor, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp.Exists && (!tmp.HasConstruction || tmp.Construction.Construction.Type != "WallFloor"))
                        list.Add(new RenderData(layer, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset, 0)));
                }
            }

            return list;
        }

        private List<RenderData> GetVegetationRenderData(Point3 tilePos, Tile tile)
        {
            var list = new List<RenderData>();
            if (!tile.HasVegetation || !tile.Vegetation.IsGrown || !tile.HasConstruction)
                return list;

            var constr = tile.Construction.Construction;
            var vegetation = tile.Vegetation.Vegetation;
            int rand = Math.Abs(HashCode.Combine(tilePos.X, tilePos.Y, tilePos.Z, vegetation.ID));
            Sprite sprite = null;
            List<Sprite> sprites = null;

            if (constr.Type != "Ramp")
            {
                if (!string.IsNullOrEmpty(constr.Category) && Resources.Vegetation.VegetationSpritesByConstrCategory.TryGetValue((vegetation, constr.Category), out sprites))
                    sprite = sprites[rand % sprites.Count];
                else if (Resources.Vegetation.VegetationSpritesByConstruction.TryGetValue((vegetation, constr.ID), out sprites))
                    sprite = sprites[rand % sprites.Count];
            }
            else if (tile.HasConstructionShape && tile.HasConstructionRotation &&
                Resources.Vegetation.VegetationDrawingByConstruction.TryGetValue((vegetation, constr.ID), out var drawing) &&
                drawing.Shapes != null && drawing.Shapes.TryGetValue(tile.ConstructionShape.Name, out var shape))
            {
                sprite = shape.Sprites[rand % shape.Sprites.Count];
                var directional = sprite.GetSpritesByDir(tile.ConstructionRotation.Direction);
                directional = sprite.GetSpritesByDir(WorldUtils.RotateDirection(tile.ConstructionRotation.Direction, site.Rotation));
                 sprite = directional[rand % directional.Count];
            }

            if (sprite != null)
            {
                list.Add(new RenderData((int)Global.DrawBufferLayer.FrontOver, tilePos, sprite, Color.White,
                    new Vector3(0, -GlobalResources.Settings.FloorYoffset + (sprites != null ? -8 : 0), 0)));
            }

            return list;
        }

        private List<RenderData> GetFluidRenderData(Point3 tilePos, Tile tile)
        {
            var list = new List<RenderData>();
            if (tile.HasFluid && tile.Fluid.Volume > 0)
            {
                float zoff = 0;
                if ((site.Map.TryGet(tilePos + new Point3(0, 1, 0), out var n1) && n1.Exists && n1.HasFluid) &&
                    (site.Map.TryGet(tilePos + new Point3(1, 0, 0), out var n2) && n2.Exists && n2.HasFluid))
                    zoff = Global.Z_DIAGONAL_OFFSET;

                byte layer = (int)Global.DrawBufferLayer.Water;
                Sprite sprite = GlobalResources.Sprites["Water"];
                Color col = Color.Blue;
                col.A = (byte)(255 - Math.Pow((TileFluid.MaxVolume - tile.Fluid.Volume), 1.2));
                list.Add(new RenderData(layer, tilePos, sprite, col,
                    new Vector3(0, ((TileFluid.MaxVolume - tile.Fluid.Volume) / 2), zoff)));
            }

            return list;
        }
    }
}