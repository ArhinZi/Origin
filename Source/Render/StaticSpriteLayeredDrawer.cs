using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Events;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

using static Origin.Source.Render.SpriteChunk;
using static Origin.Source.Resources.Global;
using static System.Net.Mime.MediaTypeNames;

namespace Origin.Source.Render
{
    public partial class StaticSpriteLayeredDrawer : IBaseLayeredDrawer
    {
        public Point ChunkSize = BASE_CHUNK_SIZE;
        private GraphicsDevice device = Global.GraphicsDevice;

        private SpriteChunk[,,] spriteChunks;
        private Site site;
        private Point3 chunksCount;

        public RenderTarget2D RenderTarget2D { get; private set; }

        public StaticSpriteLayeredDrawer(Site site)
        {
            this.site = site;

            /*if (ChunkSize.X > this.site.Size.X) */
            ChunkSize.X = this.site.Size.X;
            /*if (ChunkSize.Y > this.site.Size.Y) */
            ChunkSize.Y = this.site.Size.Y;
            //Debug.Assert(!(this.site.Size.X % ChunkSize.X != 0 || this.site.Size.Y % ChunkSize.Y != 0), "Site size is invalid!");

            chunksCount = new Point3(site.Size.X / ChunkSize.X, site.Size.Y / ChunkSize.Y, site.Size.Z);

            spriteChunks = new SpriteChunk[chunksCount.X, chunksCount.Y, chunksCount.Z];

            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                Global.Game.Window.ClientBounds.Width, Global.Game.Window.ClientBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            Hook();
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                bounds.screenBounds.Width, bounds.screenBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        }

        public void SetChunks()
        {
            for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        if (spriteChunks[x, y, z] != null)
                            spriteChunks[x, y, z].InitSet();
                    }
        }

        private SpriteChunk GetChunkByPos(Point3 pos)
        {
            Point3 pchunk = new(pos.X / ChunkSize.X, pos.Y / ChunkSize.Y, pos.Z);
            SpriteChunk chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z];
            if (chunk == null)
                chunk = spriteChunks[pchunk.X, pchunk.Y, pchunk.Z] = new SpriteChunk(pchunk.XY());

            return chunk;
        }

        public SpriteLocator AddTileSprite(RenderData data)
        {
            var chunk = GetChunkByPos(data.tilePos);

            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(data.tilePos, site);
            SpriteLayer layer = chunk.GetLayer(data.sprite.Texture, data.nlayer);

            SpriteMainData smd = new()
            {
                SpritePosition = new Vector3(WorldUtils.GetSpritePositionByCellPosition(data.tilePos, site).ToVector2(), vertexZ) + data.spriteOffset,
                CellPosition = data.tilePos,
                //SpriteSize = new Vector2(32, 32)
            };
            SpriteExtraData sed = new()
            {
                Color = data.color.ToVector4(),
                TextureRect = new Vector4(data.sprite.RectPos.X, data.sprite.RectPos.Y, data.sprite.RectPos.Width, data.sprite.RectPos.Height)
            };
            return chunk.AppendDataDirectly(layer, smd, sed);
        }

        public SpriteLocator ScheduleUpdate(RenderData data)
        {
            var chunk = GetChunkByPos(data.tilePos);

            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(data.tilePos, site);
            SpriteLayer layer = chunk.GetLayer(data.sprite.Texture, data.nlayer);

            SpriteMainData smd = new()
            {
                SpritePosition = new Vector3(WorldUtils.GetSpritePositionByCellPosition(data.tilePos, site).ToVector2(), vertexZ) + data.spriteOffset,
                CellPosition = data.tilePos,
                //SpriteSize = new Vector2(32, 32)
            };
            SpriteExtraData sed = new()
            {
                Color = data.color.ToVector4(),
                TextureRect = new Vector4(data.sprite.RectPos.X, data.sprite.RectPos.Y, data.sprite.RectPos.Width, data.sprite.RectPos.Height)
            };
            return ScheduleAdd(layer, smd, sed, data.tilePos);
        }

        public void ClearLayer(DrawBufferLayer layer)
        {
            for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        if (spriteChunks[x, y, z] != null)
                            spriteChunks[x, y, z].ClearLayer((byte)layer);
                    }
        }

        public void ScheduleRemove(List<SpriteLocator> list, Point3 pos)
        {
            var chunk = GetChunkByPos(pos);
            foreach (var locator in list)
            {
                chunk.ScheduleRemove(locator);
            }
        }

        public SpriteLocator ScheduleAdd(SpriteLayer layer, SpriteMainData dataMain, SpriteExtraData dataExtra, Point3 pos)
        {
            var chunk = GetChunkByPos(pos);
            return chunk.ScheduleAdd(layer, dataMain, dataExtra);
        }

        public void RemoveSprites()
        {
            for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                        if (spriteChunks[x, y, z] != null)
                            spriteChunks[x, y, z].RemoveScheduled();
        }

        public void AddSprites()
        {
            for (int z = 0; z < chunksCount.Z; z++)
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                        if (spriteChunks[x, y, z] != null)
                            spriteChunks[x, y, z].AddScheduled();
        }

        public void ResetAll()
        {
            spriteChunks = new SpriteChunk[chunksCount.X, chunksCount.Y, chunksCount.Z];
        }

        public void Draw(int layer, List<byte> drawableSubLayers = null, bool HalfWall = false)
        {
            void CheckLayerLight(byte sublayer)
            {
                site.LightControl.SetBuffers();

                SiteRenderer.InstanceMainEffect.Parameters["SunLightIntensity"].SetValue(site.World.TimeManager.GetSunLightIntensity());
                //SiteRenderer.InstanceMainEffect.Parameters["SunLightIntensity"].SetValue(1);
                if (sublayer == Global.LightFrontStart)
                {
                    if (layer + 1 < site.Size.Z && site.LightControl.buffers[layer + 1].ElementCount > 0)
                        SiteRenderer.InstanceMainEffect.Parameters["LightBuffer"].SetValue(site.LightControl.buffers[layer + 1]);
                }
                if (Global.NoLightLayers.Contains(sublayer))
                {
                    SiteRenderer.InstanceMainEffect.Parameters["nolight"].SetValue(true);
                }
                else
                {
                    SiteRenderer.InstanceMainEffect.Parameters["nolight"].SetValue(false);
                }

                if (sublayer == (int)DrawBufferLayer.Water)
                {
                    /*var blend = new BlendState();
                    blend.ColorSourceBlend = Blend.SourceAlpha;
                    blend.AlphaSourceBlend = Blend.SourceAlpha;
                    blend.ColorDestinationBlend = Blend.One;
                    blend.AlphaDestinationBlend = Blend.One;
                    BlendState blendState = new BlendState();
                    blendState.ColorSourceBlend = Blend.SourceAlpha;
                    blendState.AlphaSourceBlend = Blend.SourceAlpha;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
                    blendState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    blendState.ColorBlendFunction = BlendFunction.Add;
                    blendState.AlphaBlendFunction = BlendFunction.Add;*/
                    device.BlendState = BlendState.AlphaBlend;
                }
                else
                    device.BlendState = BlendState.AlphaBlend;
            }

            void SubDraw(byte sublayer, SpriteLayer dlayer)
            {
                if (dlayer.dataIndex != 0)
                {
                    /*if (sublayer == (int)DrawBufferLayer.Water)
                    {
                        BlendState blendState = new BlendState();
                        blendState.ColorSourceBlend = Blend.SourceAlpha;
                        blendState.AlphaSourceBlend = Blend.SourceAlpha;
                        blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
                        blendState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                        blendState.ColorBlendFunction = BlendFunction.Add;
                        blendState.AlphaBlendFunction = BlendFunction.Add;
                        device.BlendState = blendState;

                        var lastRT = Global.GraphicsDevice.GetRenderTargets()[0];
                        Global.GraphicsDevice.SetRenderTarget(RenderTarget2D);
                        Global.GraphicsDevice.Clear(new Color(0, 0, 0, 0));

                        SiteRenderer.InstanceMainEffect.Parameters["MainBuffer"].SetValue(dlayer.bufferDataMain);
                        SiteRenderer.InstanceMainEffect.Parameters["ExtraBuffer"].SetValue(dlayer.bufferDataExtra);
                        SiteRenderer.InstanceMainEffect.CurrentTechnique.Passes[0].Apply();
                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, (int)(dlayer.dataIndex * 2));

                        Global.GraphicsDevice.SetRenderTarget((RenderTarget2D)lastRT.RenderTarget);
                        device.BlendState = BlendState.AlphaBlend;
                        return;
                    }
                    else*/
                    {
                        SiteRenderer.InstanceMainEffect.Parameters["MainBuffer"].SetValue(dlayer.bufferDataMain);
                        SiteRenderer.InstanceMainEffect.Parameters["ExtraBuffer"].SetValue(dlayer.bufferDataExtra);

                        SiteRenderer.InstanceMainEffect.CurrentTechnique.Passes[0].Apply();

                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, (int)(dlayer.dataIndex * 2));
                    }
                }
            }

            SiteRenderer.InstanceMainEffect.CurrentTechnique = SiteRenderer.InstanceMainEffect.Techniques["SpriteInstancing"];

            drawableSubLayers ??= new List<byte>((IEnumerable<byte>)Enum.GetValues(typeof(DrawBufferLayer)));
            foreach (var sublayer in drawableSubLayers)
            {
                for (int x = 0; x < chunksCount.X; x++)
                    for (int y = 0; y < chunksCount.Y; y++)
                    {
                        var chunk = spriteChunks[x, y, layer];
                        if (chunk != null)
                        {
                            var layersBatches = chunk.layersBatches;
                            if (layersBatches != null)
                            {
                                foreach (var item in layersBatches)
                                {
                                    if (!item.Value.TryGetValue(sublayer, out var dlayer)) continue;
                                    var tex = item.Key;
                                    var offset = new Vector3(0, 0, 0);
                                    if (HalfWall)
                                    {
                                        //bool found = false;
                                        foreach (var hwt in GlobalResources.HalfWallDefs)
                                        {
                                            if (hwt.Original == tex)
                                            {
                                                //found = true;
                                                tex = hwt.HalfWall;
                                                offset = hwt.TexOffset.ToVector3();
                                                break;
                                            }
                                        }
                                    }
                                    //if(sublayer == (int)DrawBufferLayer.Water)
                                    //{
                                    //    SiteRenderer.InstanceMainEffect.Parameters["PositionOffset"].SetValue(offset+new Vector3(0,0, 0.01f));
                                    //} 
                                    //else
                                        SiteRenderer.InstanceMainEffect.Parameters["PositionOffset"].SetValue(offset);
                                    SiteRenderer.InstanceMainEffect.Parameters["SpriteTexture"].SetValue(tex);
                                    SiteRenderer.InstanceMainEffect.Parameters["TextureSize"].SetValue(new Vector2(tex.Width, tex.Height));

                                    device.SetVertexBuffer(SiteRenderer.GeometryBuffer);
                                    device.DepthStencilState = DepthStencilState.Default;
                                    device.BlendState = BlendState.AlphaBlend;

                                    CheckLayerLight(sublayer);
                                    SubDraw(sublayer, dlayer);
                                }
                            }
                        }
                    }
            }
        }
    }
}