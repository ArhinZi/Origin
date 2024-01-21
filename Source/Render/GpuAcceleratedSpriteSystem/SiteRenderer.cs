using Arch.Core;
using Arch.Core.Extensions;

using info.lundin.math;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Sprites;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Render.GpuAcceleratedSpriteSystem.SpriteChunk;
using static System.Reflection.Metadata.BlobBuilder;

using Sprite = Origin.Source.Resources.Sprite;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public class SiteRenderer
    {
        private SpriteChunk[,,] spriteChunks;

        private Site site;
        private GraphicsDevice device;
        private Point3 chunksCount;

        private int _drawLowest;
        private int _drawHighest;

        private Effect _effect;

        public Point ChunkSize;
        //public Point3 Size { get; private set; }

        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new Color(0, 0, 0, 150);

        private int seed = 234223534;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            this.site = site;
            ChunkSize = Global.BASE_CHUNK_SIZE;
            device = graphicDevice;

            if (ChunkSize.X > this.site.Size.X) ChunkSize.X = this.site.Size.X;
            if (ChunkSize.Y > this.site.Size.Y) ChunkSize.Y = this.site.Size.Y;
            float j = this.site.Size.X % ChunkSize.X;
            //(this.site.Size.X % ChunkSize.X != 0 || this.site.Size.Y % ChunkSize.Y != 0)
            Debug.Assert(!(this.site.Size.X % ChunkSize.X != 0 || this.site.Size.Y % ChunkSize.Y != 0), "Site size is invalid!");

            chunksCount = new Point3(site.Size.X / ChunkSize.X, site.Size.Y / ChunkSize.Y, site.Size.Z);
            //Size = new Utils.Point3(site.Size.X / ChunkSize.X, site.Size.Y / ChunkSize.Y, site.Size.Z);

            _drawHighest = site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            _effect = OriginGame.Instance.Content.Load<Effect>("FX/InstancedTileDraw");

            SpriteChunk.GraphicsDevice = graphicDevice;
            SpriteChunk.Effect = _effect;
            spriteChunks = new SpriteChunk[chunksCount.X, chunksCount.Y, chunksCount.Z];

            /*for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        spriteChunks[x, y, z] = new SpriteChunk(new Point(x, y), site);
                    }*/

            int rand = seed;
            Random random = new Random(rand);
            for (int z = 0; z < site.Size.Z; z++)
                for (int x = 0; x < site.Size.X; x++)
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new Point3(x, y, z);
                        Entity tile = site.Map[tilePos];

                        rand = random.Next();

                        BaseConstruction bcc;
                        if (tile != Entity.Null && tile.TryGet(out bcc))
                        {
                            Construction constr = bcc.Construction;
                            Material mat = bcc.Material;
                            {
                                byte LAYER = 0;
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                                constr.Sprites["Wall"][rand % constr.Sprites["Wall"].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;

                                AddTileSprite(LAYER, tilePos, sprite, col);

                                Entity tmp;
                                // Draw borders of Wall
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null && (
                                        !tmp.Has<BaseConstruction>()))
                                    AddTileSprite(LAYER, tilePos, lborderSprite, borderColor);
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null && (
                                        !tmp.Has<BaseConstruction>()))
                                    AddTileSprite(LAYER, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0));
                            }
                            {
                                byte LAYER = 10;
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                        constr.Sprites["Floor"][rand % constr.Sprites["Floor"].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;

                                AddTileSprite(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0));

                                Entity tmp;
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null && (
                                            !tmp.Has<BaseConstruction>()))
                                    AddTileSprite(LAYER, tilePos, lborderSprite, borderColor,
                                        new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0));
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null && (
                                        !tmp.Has<BaseConstruction>()))
                                    AddTileSprite(LAYER, tilePos, rborderSprite, borderColor,
                                        new Vector3(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1, 0));

                                //TODO Draw Vegetation
                                LAYER = 15;
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
                                        AddTileSprite(LAYER, tilePos, sprite, Color.White,
                                            new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0));
                                }
                            }
                        }
                        /*else if (tile == Entity.Null)
                        {
                            // Draw hidden
                            Color c = GlobalResources.HIDDEN_COLOR;

                            Sprite sprite = GlobalResources.HIDDEN_WALL_SPRITE;
                            if (site.Size.X - 1 == x || site.Size.Y - 1 == y)
                                AddTileSprite(1, tilePos, sprite, c,
                                        new Vector3(0, 0, 0));
                            *//*else
                                AddTileSprite(1, tilePos, sprite, c,
                                        new Vector3(0, 0, 0));*//*

                            sprite = GlobalResources.HIDDEN_FLOOR_SPRITE;
                            if (site.Size.X - 1 == x || site.Size.Y - 1 == y)
                                AddTileSprite(1, tilePos, sprite, c,
                                        new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0));
                            *//*else
                                AddTileSprite(1, tilePos, sprite, c,
                                        new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0));*//*
                        }*/
                    }
            for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        if (spriteChunks[x, y, z] != null)
                            spriteChunks[x, y, z].Set();
                    }
        }

        public void Update(GameTime gameTime)
        {
        }

        private void CheckCurrentLevelChanged()
        {
            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != site.CurrentLevel)
            {
                /*if (HalfWallMode)
                {
                    RenderTasks.Enqueue(new RenderTask()
                    {
                        task = TaskLevelHalfWallUpdate(Site.PreviousLevel, Site.CurrentLevel),
                        OnComplete = new Action(() => { })
                    });
                }*/
                _drawHighest = site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                //RecalcHiddenInstances();
            }
        }

        public void Draw(GameTime gameTime)
        {
            CheckCurrentLevelChanged();

            for (int z = _drawLowest; z <= _drawHighest; z++)
                foreach (var key in texture2Ds)
                {
                    for (int x = 0; x < chunksCount.X; x++)
                        for (int y = 0; y < chunksCount.Y; y++)
                        {
                            if (spriteChunks[x, y, z] != null)
                                spriteChunks[x, y, z].Draw();
                        }
                }
        }

        private void AddTileSprite(byte nlayer, Point3 tilePos, Sprite sprite, Color color, Vector3 spriteOffset = new())
        {
            Point3 pchunk = new Point3(tilePos.X / ChunkSize.X, tilePos.Y / ChunkSize.Y, tilePos.Z);
            SpriteChunk chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z];
            if (chunk == null)
                chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z] = new SpriteChunk(pchunk.ToPoint(), site);

            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(tilePos);
            Layer layer = chunk.GetLayer(sprite.Texture, nlayer);

            SpriteMainData smd = new SpriteMainData()
            {
                SpritePosition = new Vector3(WorldUtils.GetSpritePositionByCellPosition(tilePos).ToVector2(), vertexZ) + spriteOffset,
                //CellPosition = tilePos.ToVector3(),
                //SpriteSize = new Vector2(32, 32)
            };
            SpriteExtraData sed = new SpriteExtraData()
            {
                Color = color.ToVector4(),
                TextureRect = new Vector4(sprite.RectPos.X, sprite.RectPos.Y, sprite.RectPos.Width, sprite.RectPos.Height)
            };
            chunk.AppendDataDirectly(layer, smd, sed);
        }
    }
}