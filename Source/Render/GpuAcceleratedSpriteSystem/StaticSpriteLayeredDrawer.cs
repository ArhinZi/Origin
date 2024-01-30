﻿using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public class StaticSpriteLayeredDrawer : IBaseLayeredDrawer
    {
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new Color(0, 0, 0, 150);
        public Point ChunkSize = Global.BASE_CHUNK_SIZE;
        private GraphicsDevice device = OriginGame.Instance.GraphicsDevice;
        private int seed = 123456789;

        private SpriteChunk[,,] spriteChunks;
        private Site site;
        private Point3 chunksCount;
        private Effect _effect;

        public VertexBuffer geometryBuffer;

        public int Seed
        {
            get { return seed; }
            set { seed = value; }
        }

        public StaticSpriteLayeredDrawer(Site site)
        {
            this.site = site;

            if (ChunkSize.X > this.site.Size.X) ChunkSize.X = this.site.Size.X;
            if (ChunkSize.Y > this.site.Size.Y) ChunkSize.Y = this.site.Size.Y;
            Debug.Assert(!(this.site.Size.X % ChunkSize.X != 0 || this.site.Size.Y % ChunkSize.Y != 0), "Site size is invalid!");

            _effect = OriginGame.Instance.Content.Load<Effect>("FX/InstancedTileDraw");
            chunksCount = new Point3(site.Size.X / ChunkSize.X, site.Size.Y / ChunkSize.Y, site.Size.Z);

            spriteChunks = new SpriteChunk[chunksCount.X, chunksCount.Y, chunksCount.Z];
            GenerateInstanceGeometry();
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

                        rand = random.Next();

                        BaseConstruction bcc;
                        if (tile != Entity.Null && tile.TryGet(out bcc))
                        {
                            Construction constr = bcc.Construction;
                            Material mat = bcc.Material;
                            {
                                byte LAYER = 0;
                                string spart = "Wall";
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                                constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;

                                AddTileSprite(LAYER, tilePos, sprite, col);

                                Entity tmp;
                                // Draw borders of Wall
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
                                    AddTileSprite(LAYER, tilePos, lborderSprite, borderColor);
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
                                    AddTileSprite(LAYER, tilePos, rborderSprite, borderColor, new Vector3(GlobalResources.Settings.TileSize.X / 2, 0, 0));
                            }
                            {
                                byte LAYER = 10;
                                string spart = "Floor";
                                Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                        constr.Sprites[spart][rand % constr.Sprites[spart].Count]);
                                Color col = constr.HasMaterialColor ? mat.Color : Color.White;

                                AddTileSprite(LAYER, tilePos, sprite, col, new Vector3(0, -GlobalResources.Settings.FloorYoffset, 0));

                                Entity tmp;
                                if (site.Map.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null &&
                                            !tmp.Has<BaseConstruction>())
                                    AddTileSprite(LAYER, tilePos, lborderSprite, borderColor,
                                        new Vector3(0, -GlobalResources.Settings.FloorYoffset - 1, 0));
                                if (site.Map.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null &&
                                        !tmp.Has<BaseConstruction>())
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
                        /* else if (tile == Entity.Null)
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

        private void AddTileSprite(byte nlayer, Point3 tilePos, Sprite sprite, Color color, Vector3 spriteOffset = new())
        {
            Point3 pchunk = new Point3(tilePos.X / ChunkSize.X, tilePos.Y / ChunkSize.Y, tilePos.Z);
            SpriteChunk chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z];
            if (chunk == null)
                chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z] = new SpriteChunk(pchunk.ToPoint());

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

        private void GenerateInstanceGeometry()
        {
            int size = 256 * 256;
            GeometryData[] _vertices = new GeometryData[6 * size];

            #region filling vertices

            for (int i = 0; i < size; i++)
            {
                _vertices[i * 6 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 3].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 4].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 5].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
            }

            #endregion filling vertices

            geometryBuffer = new VertexBuffer(device, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            geometryBuffer.SetData(_vertices);
        }

        public void Update()
        {
        }

        public void Draw(int layer, Vector2 LowHigh, List<byte> drawableSubLayers = null)
        {
            void SubDraw(Layer dlayer)
            {
                if (dlayer.dataIndex != 0)
                {
                    _effect.Parameters["MainBuffer"].SetValue(dlayer.bufferDataMain);
                    _effect.Parameters["ExtraBuffer"].SetValue(dlayer.bufferDataExtra);

                    _effect.CurrentTechnique.Passes[0].Apply();

                    device.DepthStencilState = DepthStencilState.Default;
                    device.BlendState = BlendState.AlphaBlend;

                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, dlayer.dataIndex * 2);
                }
            }

            Matrix WVP = Matrix.Multiply(Matrix.Multiply(site.Camera.WorldMatrix, site.Camera.Transformation),
                                site.Camera.Projection);
            foreach (var tex in texture2Ds)
            {
                _effect.Parameters["SpriteTexture"].SetValue(tex);
                _effect.Parameters["TextureSize"].SetValue(new Vector2(tex.Width, tex.Height));
                _effect.Parameters["WorldViewProjection"].SetValue(WVP);
                _effect.Parameters["LowHighLevel"].SetValue(LowHigh);
                _effect.Parameters["CurrentLevel"].SetValue(layer);
                _effect.CurrentTechnique = _effect.Techniques["SpriteInstancing"];

                device.SetVertexBuffer(geometryBuffer);

                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        var chunk = spriteChunks[x, y, layer];
                        if (chunk != null)
                        {
                            var layersBatches = chunk.layersBatches;
                            if (layersBatches != null)
                            {
                                if (drawableSubLayers != null)
                                {
                                    foreach (var sublayer in drawableSubLayers)
                                        if (layersBatches[tex].TryGetValue(sublayer, out Layer dlayer))
                                            SubDraw(dlayer);
                                }
                                else
                                {
                                    foreach (var pair in layersBatches[tex].OrderBy(x => x.Key))
                                        SubDraw(pair.Value);
                                }
                            }
                        }
                    }
            }
        }
    }
}