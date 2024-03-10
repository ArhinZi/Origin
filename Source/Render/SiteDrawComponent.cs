using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.Events;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;
using Origin.Source.Model.Site;
using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Vegetation.Components;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public partial class SiteDrawComponent
    {
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new Color(0, 0, 0, 255);

        private Site site;
        private SiteRenderer _siteRenderer;
        private Random random;

        public RenderTarget2D RenderTarget2D { get; private set; }
        private SpriteBatch spriteBatch = new SpriteBatch(Global.GraphicsDevice);

        public SiteDrawComponent(Site site)
        {
            this.site = site;
            _siteRenderer = new(site, Global.GraphicsDevice);

            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                Global.Game.Window.ClientBounds.Width, Global.Game.Window.ClientBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            random = site.World.Random;

            InitTerrainSprites();
            InitTerrainHiddence();
            Hook();
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                bounds.screenBounds.Width, bounds.screenBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        }

        public void InitTerrainSprites()
        {
            var query = new QueryDescription().WithAll<IsTile, BaseConstruction>();
            site.ArchWorld.Add<SpriteLocatorsStatic>(query);
            site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref BaseConstruction bcc) =>
            {
                Point3 tilePos = tile.Position;
                {
                    ref SpriteLocatorsStatic locators = ref ent.Get<SpriteLocatorsStatic>();
                    Construction constr = bcc.Construction;
                    Material mat = bcc.Material;
                    int rand = random.Next();
                    {
                        byte LAYER = (int)DrawBufferLayer.Back;
                        string spart = "Wall";
                        Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                        constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                        Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                        locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, col));

                        // Draw borders of Wall
                        if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                                !tmp.Has<BaseConstruction>())
                            locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, lborderSprite, borderColor,
                                new Vector3(0, -1, 0)));
                        if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                !tmp.Has<BaseConstruction>())
                            locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, rborderSprite, borderColor,
                                new Vector3(GlobalResources.Settings.TileSize.X / 2, -1, 0)));
                    }
                    {
                        byte LAYER = (int)DrawBufferLayer.Front;
                        string spart = "Floor";
                        Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                        Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                        locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                        if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                                    !tmp.Has<BaseConstruction>())
                            locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, lborderSprite, borderColor,
                                new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0)));
                        if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                !tmp.Has<BaseConstruction>())
                            locators.list.Add(_siteRenderer.StaticDrawer.AddTileSprite(LAYER, tilePos, rborderSprite, borderColor,
                                new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1, 0)));

                        //TODO Draw Vegetation
                        LAYER = (int)DrawBufferLayer.FrontOver;
                        if (ent.TryGet(out BaseVegetation hveg))
                        {
                            Vegetation veg = GlobalResources.Vegetations[hveg.VegetationMetaID];
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
            });

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
            Point3 tilePos = entity.Get<IsTile>().Position;
            Entity tile = entity;
            ref SpriteLocatorsStatic locators = ref tile.AddOrGet(new SpriteLocatorsStatic());

            int rand = random.Next();

            if (tile != Entity.Null && tile.TryGet(out BaseConstruction bcc))
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

                    // Draw borders of Wall
                    LAYER = (int)DrawBufferLayer.BackNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, lborderSprite, borderColor,
                            new Vector3(0, -1, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, rborderSprite, borderColor,
                            new Vector3(GlobalResources.Settings.TileSize.X / 2, -1, 0)));
                }
                {
                    byte LAYER = (int)DrawBufferLayer.Front;
                    string spart = "Floor";
                    Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                            constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                    Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                    locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0)));

                    LAYER = (int)DrawBufferLayer.FrontNoLight;
                    if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out Entity tmp) && tmp != Entity.Null &&
                                !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, lborderSprite, borderColor,
                            new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0)));
                    if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                            !tmp.Has<BaseConstruction>())
                        locators.list.Add(_siteRenderer.StaticDrawer.ScheduleUpdate(LAYER, tilePos, rborderSprite, borderColor,
                            new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1, 0)));

                    //TODO Draw Vegetation
                    LAYER = (int)DrawBufferLayer.FrontOver;
                    if (tile.TryGet(out BaseVegetation hveg))
                    {
                        Vegetation veg = GlobalResources.Vegetations[hveg.VegetationMetaID];
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
            if (site.ArchWorld.CountEntities(new QueryDescription().WithAll<UpdateTileRenderSelfRequest>()) > 0)
            {
                var query = new QueryDescription().WithAll<UpdateTileRenderSelfRequest, IsTile>();
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
                });
                _siteRenderer.StaticDrawer.RemoveSprites();

                site.ArchWorld.Query(in query, (Entity entity, ref IsTile tile) =>
                {
                    ScheduleUpdateTile(entity);
                });
                _siteRenderer.StaticDrawer.AddSprites();
                _siteRenderer.HiddenDrawer.Set();

                site.ArchWorld.Remove<UpdateTileRenderSelfRequest>(query);
            }
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(GameTime gameTime)
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

            Global.GraphicsDevice.SetRenderTarget(RenderTarget2D);
            Global.GraphicsDevice.Clear(Color.CornflowerBlue);
            _siteRenderer.Draw(gameTime);
            Global.GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin();
            spriteBatch.Draw(RenderTarget2D, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}