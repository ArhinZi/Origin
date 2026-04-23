using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Systems;
using Origin.Source.Model.NewWorld.Systems.Vegetation;
using Tile = Origin.Source.Model.NewWorld.Tile;
using Origin.Source.Render;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems.Render
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

                        if (tile.HasVegetation)
                        {
                            foreach (var data in GetVegetationRenderData(tilePos, tile))
                            {
                                var locator = renderer.StaticDrawer.AddTileSprite(data);
                                renderState.VegetationLocators.Add(locator);
                                SetVegetationSpriteLocator(tile, locator);
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

                    // Для рослинності намагаємось зберегти locator і оновити інстанс in-place в другому проході.
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
                if (!site.RenderState.TryGet(tilePos, out var state))
                    state = site.RenderState.GetOrCreate(tilePos);

                if (!tile.Exists)
                {
                    // Якщо тайл зник, прибираємо й рослинні локатори, які могли лишитися.
                    if (state.VegetationLocators.Count > 0)
                    {
                        renderer.StaticDrawer.ScheduleRemove(state.VegetationLocators, tilePos);
                        state.VegetationLocators.Clear();
                        removeDirty = true;
                    }

                    renderer.HiddenDrawer.MakeHidden(tilePos);
                    site.RenderState.Remove(tilePos);
                    hiddenDirty = true;
                    continue;
                }

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

                var vegetationData = GetVegetationRenderData(tilePos, tile);
                if (tile.HasVegetation && vegetationData.Count > 0)
                {
                    // Якщо вже є валідні локатори тієї ж кількості — робимо in-place update без remove/add.
                    if (state.VegetationLocators.Count == vegetationData.Count)
                    {
                        for (int i = 0; i < vegetationData.Count; i++)
                        {
                            var locator = state.VegetationLocators[i];
                            renderer.StaticDrawer.ScheduleUpdate(locator, vegetationData[i]);
                            SetVegetationSpriteLocator(tile, locator);
                            addDirty = true;
                        }
                    }
                    else
                    {
                        // Інакше (кількість змінилась) робимо стандартний remove/add.
                        if (state.VegetationLocators.Count > 0)
                        {
                            renderer.StaticDrawer.ScheduleRemove(state.VegetationLocators, tilePos);
                            state.VegetationLocators.Clear();
                            removeDirty = true;
                        }

                        foreach (var data in vegetationData)
                        {
                            var locator = renderer.StaticDrawer.ScheduleUpdate(data);
                            state.VegetationLocators.Add(locator);
                            SetVegetationSpriteLocator(tile, locator);
                            addDirty = true;
                        }
                    }
                }
                else if (state.VegetationLocators.Count > 0)
                {
                    // Рослинність зникла — прибираємо старі інстанси.
                    renderer.StaticDrawer.ScheduleRemove(state.VegetationLocators, tilePos);
                    state.VegetationLocators.Clear();
                    removeDirty = true;
                }

                if (!state.HasAny)
                {
                    site.RenderState.Remove(tilePos);
                }
            }

            // Після другого проходу могли з'явитися нові remove-операції (наприклад, vegetation mismatch).
            if (removeDirty)
            {
                renderer.StaticDrawer.RemoveSprites();
            }

            if (addDirty)
            {
                // Цей прохід застосовує і додавання, і in-place оновлення інстансів.
                renderer.StaticDrawer.AddSprites();
            }

            if (hiddenDirty)
            {
                renderer.HiddenDrawer.Set();
            }
        }

        private bool HasWallFloorNeighbourForBorder(Point3 tilePos, Point3 rotatedOffset)
        {
            Point3 rotatedPos = WorldUtils.RotatePosition(tilePos, site.Size, site.Rotation);
            Point3 neighbourRotatedPos = rotatedPos + rotatedOffset;
            Point3 neighbourWorldPos = WorldUtils.InverseRotatePosition(neighbourRotatedPos, site.Size, site.Rotation);

            return site.Map.TryGet(neighbourWorldPos, out Tile neighbour)
                   && neighbour.Exists
                   && neighbour.HasConstruction
                   && neighbour.Construction.Construction.Type == "WallFloor";
        }

        private bool ShouldDrawBorderAgainstAir(Point3 tilePos, Point3 rotatedOffset)
        {
            Point3 rotatedPos = WorldUtils.RotatePosition(tilePos, site.Size, site.Rotation);
            Point3 neighbourRotatedPos = rotatedPos + rotatedOffset;
            Point3 neighbourWorldPos = WorldUtils.InverseRotatePosition(neighbourRotatedPos, site.Size, site.Rotation);

            if (!site.Map.TryGet(neighbourWorldPos, out Tile neighbour))
                return false;

            return neighbour.Exists && (!neighbour.HasConstruction || neighbour.IsRamp);
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
                    if (ShouldDrawBorderAgainstAir(tilePos, new Point3(-1, 0, 0)))
                        list.Add(new RenderData(layer, tilePos, lborderSprite, borderColor, Vector3.Zero));
                    if (ShouldDrawBorderAgainstAir(tilePos, new Point3(0, -1, 0)))
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
                    if (ShouldDrawBorderAgainstAir(tilePos, new Point3(-1, 0, 0)))
                        list.Add(new RenderData(layer, tilePos, lborderSprite, borderColor, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                    if (ShouldDrawBorderAgainstAir(tilePos, new Point3(0, -1, 0)))
                        list.Add(new RenderData(layer, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset, 0)));
                }
            }

            return list;
        }

        private List<RenderData> GetVegetationRenderData(Point3 tilePos, Tile tile)
        {
            var list = new List<RenderData>();
            if (!tile.HasVegetation || !tile.HasConstruction)
                return list;

            Entity entity = tile.VegetationEntity;
            if (entity == Entity.Null || !entity.IsAlive())
                return list;

            // Беремо стан рослинності напряму з ECS-ентіті, без дублювання в тайлі.
            if (!VegUtilities.TryGetVegetationState(site, tile, out var vegetation, out var growthLevel, out _))
                return list;

            // Захист від рендера нульового рівня росту.
            if (growthLevel <= 0)
                return list;

            // Простий стабільний варіант спрайта від seed/позиції/типу.
            int vegetationMeta = entity.TryGet(out VegetationTypeTag typeTag) ? typeTag.VegetationMetaID : 0;
            int variant = ComputeStableSpriteVariant(site.World.Seed, site.ID, tilePos, vegetationMeta);

            var constr = tile.Construction.Construction;
            Sprite sprite = null;
            List<Sprite> sprites = null;

            if (constr.Type != "Ramp")
            {
                if (!string.IsNullOrEmpty(constr.Category) && Resources.Vegetation.VegetationSpritesByConstrCategory.TryGetValue((vegetation, constr.Category), out sprites))
                    sprite = sprites[variant % sprites.Count];
                else if (Resources.Vegetation.VegetationSpritesByConstruction.TryGetValue((vegetation, constr.ID), out sprites))
                    sprite = sprites[variant % sprites.Count];
            }
            else if (tile.HasConstructionShape && tile.HasConstructionRotation &&
                Resources.Vegetation.VegetationDrawingByConstruction.TryGetValue((vegetation, constr.ID), out var drawing) &&
                drawing.Shapes != null && drawing.Shapes.TryGetValue(tile.ConstructionShape.Name, out var shape))
            {
                sprite = shape.Sprites[variant % shape.Sprites.Count];
                var directional = sprite.GetSpritesByDir(WorldUtils.RotateDirection(tile.ConstructionRotation.Direction, site.Rotation));
                int dirVariant = ((variant / Math.Max(1, shape.Sprites.Count)) & int.MaxValue) % directional.Count;
                sprite = directional[dirVariant];
            }

            if (sprite != null)
            {
                // Прозорість і яскравість залежать від рівня росту: 0 -> майже прозора і темна, 8 -> повна непрозорість і нормальна яскравість.
                float growth01 = Math.Clamp(growthLevel / 8f, 0f, 1f);
                byte alpha = (byte)Math.Clamp(32 + (int)(growth01 * 223f), 0, 255);
                // Мінімальна яскравість 40% при рівні 0, 100% при рівні 8
                byte brightness = (byte)Math.Clamp(102 + (int)(growth01 * 153f), 0, 255);
                Color color = new Color(brightness, brightness, brightness, alpha);

                list.Add(new RenderData((int)Global.DrawBufferLayer.FrontOver, tilePos, sprite, color,
                    new Vector3(0, -GlobalResources.Settings.FloorYoffset + (sprites != null ? -8 : 0), 0)));
            }

            return list;
        }

        // Фіксуємо locator спрайта рослинності в ECS-компоненті ентіті.
        private static void SetVegetationSpriteLocator(in Tile tile, SpriteLocator locator)
        {
            if (!tile.HasVegetation)
                return;

            Entity entity = tile.VegetationEntity;
            if (entity == Entity.Null || !entity.IsAlive())
                return;

            var value = new VegetationSpriteRenderLocator { Value = locator };
            if (entity.Has<VegetationSpriteRenderLocator>())
                entity.Set(value);
            else
                entity.Add(value);
        }

        // Простий стабільний хеш для вибору варіанту спрайта.
        private static int ComputeStableSpriteVariant(int worldSeed, int siteId, Point3 pos, int vegetationMeta)
        {
            unchecked
            {
                int h = worldSeed + pos.GetHashCode();
                return h & int.MaxValue;
            }
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
