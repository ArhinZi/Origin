using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using Origin.Source.ECS;

using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public class SiteDrawControlService
    {
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new Color(0, 0, 0, 150);

        private Site site;
        private SiteRenderer _siteRenderer;
        private int seed = 123456789;

        public SiteDrawControlService(Site site)
        {
            this.site = site;
            _siteRenderer = new(site, OriginGame.Instance.GraphicsDevice);

            InitTerrainSprites();
            InitTerrainHiddence();
        }

        public void InitTerrainSprites()
        {
            int rand = seed;
            Random random = new Random(seed);
            for (int z = 0; z < site.Size.Z; z++)
                for (int x = 0; x < site.Size.X; x++)
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new Point3(x, y, z);
                        Entity tile = site.Map[tilePos];
                        SpriteLocatorsStatic locators = tile.AddOrGet(new SpriteLocatorsStatic());

                        rand = random.Next();

                        BaseConstruction bcc;
                        if (tile != Entity.Null && tile.TryGet(out bcc))
                        {
                            Construction constr = bcc.Construction;
                            Material mat = bcc.Material;
                            {
                                byte LAYER = (int)DrawBufferLayer.Back;
                                string spart = "Wall";
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                                constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                                locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, col));

                                Entity tmp;
                                // Draw borders of Wall
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
                                    locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, lborderSprite, borderColor));
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
                                    locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0)));
                            }
                            {
                                byte LAYER = (int)DrawBufferLayer.Front;
                                string spart = "Floor";
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                        constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                                locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                                Entity tmp;
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                                            !tmp.Has<BaseConstruction>())
                                    locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, lborderSprite, borderColor,
                                        new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0)));
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
                                    locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, rborderSprite, borderColor,
                                        new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1, 0)));

                                //TODO Draw Vegetation
                                LAYER = (int)DrawBufferLayer.FrontOver;
                                HasVegetation hveg;
                                if (tile.TryGet(out hveg))
                                {
                                    Vegetation veg = GlobalResources.GetResourceBy(GlobalResources.Vegetations, "ID", hveg.VegetationID);
                                    List<string> spritesIDs;
                                    if (Vegetation.VegetationSpritesByConstrCategory.TryGetValue((veg, constr.ID), out spritesIDs))
                                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);
                                    else if (Vegetation.VegetationSpritesByConstruction.TryGetValue((veg, constr.ID), out spritesIDs))
                                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);

                                    if (spritesIDs != null)
                                        locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, Color.White,
                                            new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                                }
                            }
                        }
                    }

            _siteRenderer.StaticDrawer.SetChunks();
        }

        public void InitTerrainHiddence()
        {
            for (int z = 0; z < site.Size.Z; z++)
                for (int x = 0; x < site.Size.X; x++)
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new Point3(x, y, z);
                        Entity tile = site.Map[tilePos];

                        if (tile == Entity.Null)
                        {
                            _siteRenderer.HiddenDrawer.MakeHidden(tilePos);
                        }
                    }
            _siteRenderer.HiddenDrawer.Set();
        }

        public void ScheduleUpdateTile(Entity entity)
        {
            int rand = seed;
            Random random = new Random(seed);

            Point3 tilePos = entity.Get<IsTile>().Position;
            Entity tile = entity;
            SpriteLocatorsStatic locators = tile.AddOrGet(new SpriteLocatorsStatic());

            rand = random.Next();

            BaseConstruction bcc;
            if (tile != Entity.Null && tile.TryGet(out bcc))
            {
                Construction constr = bcc.Construction;
                Material mat = bcc.Material;
                {
                    byte LAYER = (int)DrawBufferLayer.Back;
                    string spart = "Wall";
                    Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                    constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                    Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                    locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, sprite, col));

                    Entity tmp;
                    // Draw borders of Wall
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, lborderSprite, borderColor));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0)));
                }
                {
                    byte LAYER = (int)DrawBufferLayer.Front;
                    string spart = "Floor";
                    Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                            constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                    Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                    locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                    Entity tmp;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                                !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, lborderSprite, borderColor,
                            new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, rborderSprite, borderColor,
                            new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1, 0)));

                    //TODO Draw Vegetation
                    LAYER = (int)DrawBufferLayer.FrontOver;
                    HasVegetation hveg;
                    if (tile.TryGet(out hveg))
                    {
                        Vegetation veg = GlobalResources.GetResourceBy(GlobalResources.Vegetations, "ID", hveg.VegetationID);
                        List<string> spritesIDs;
                        if (Vegetation.VegetationSpritesByConstrCategory.TryGetValue((veg, constr.ID), out spritesIDs))
                            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);
                        else if (Vegetation.VegetationSpritesByConstruction.TryGetValue((veg, constr.ID), out spritesIDs))
                            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", spritesIDs[rand % spritesIDs.Count]);

                        if (spritesIDs != null)
                            locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, sprite, Color.White,
                                new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));
                    }
                }
            }
        }

        private void UpdateChangedTiles()
        {
            if (site.ArchWorld.CountEntities(new QueryDescription().WithAll<WaitingForUpdateTileRender>()) > 0)
            {
                var query = new QueryDescription().WithAll<WaitingForUpdateTileRender, IsTile>();
                var commands = new CommandBuffer(site.ArchWorld);
                site.ArchWorld.Query(in query, (Entity entity, ref IsTile tile) =>
                {
                    var item = tile.Position;
                    List<Point3> neighbours = new List<Point3>()
                        {
                            new Point3(0, 0, 0),
                            new Point3(-1, 0, 0),new Point3(0, -1, 0),
                            new Point3(1, 0, 0),new Point3(0, 1, 0)
                        };
                    //foreach (var n in neighbours)
                    {
                        if (entity.TryGet(out SpriteLocatorsStatic locators))
                        {
                            _siteRenderer.StaticDrawer.ScheduleRemove(locators, item);
                            locators.list.Clear();
                        }
                    }
                    _siteRenderer.HiddenDrawer.ClearHidden(item);
                    commands.Remove<WaitingForUpdateTileRender>(entity);
                });
                _siteRenderer.StaticDrawer.RemoveSprites();
                query = new QueryDescription().WithAll<WaitingForUpdateTileRender, IsTile>();
                site.ArchWorld.Query(in query, (Entity entity, ref IsTile tile) =>
                {
                    ScheduleUpdateTile(entity);
                });
                commands.Playback();
                _siteRenderer.StaticDrawer.AddSprites();
                _siteRenderer.HiddenDrawer.Set();
            }
        }

        public void Update(GameTime gameTime)
        {
            UpdateChangedTiles();

            if (site.Tools.CurrentTool != null && site.Tools.CurrentTool.DrawDirty)
            {
                site.Tools.CurrentTool.DrawDirty = false;
                _siteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.BackInteractives);
                _siteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.FrontInteractives);
                foreach (var sprite in site.Tools.CurrentTool.sprites)
                {
                    byte LAYER = (byte)site.Tools.CurrentTool.RenderLayer;
                    _siteRenderer.StaticDrawer.AddTileSprite(LAYER, sprite.position, sprite.sprite, sprite.color, new Vector3(sprite.offset.ToVector2(), 0));
                }
                _siteRenderer.StaticDrawer.SetChunks();
            }
        }

        public void Draw(GameTime gameTime)
        {
            _siteRenderer.Draw(gameTime);
        }
    }
}