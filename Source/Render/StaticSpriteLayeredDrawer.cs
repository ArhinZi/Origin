using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Sprites;

using Arch.Bus;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Model.Site;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Render.SpriteChunk;
using static Origin.Source.Resources.Global;

using Sprite = Origin.Source.Resources.Sprite;
using MonoGame.Extended.Timers;

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

        public SpriteLocator AddTileSprite(byte nlayer, Point3 tilePos, Sprite sprite, Color color, Vector3 spriteOffset = new())
        {
            var chunk = GetChunkByPos(tilePos);

            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(tilePos);
            SpriteLayer layer = chunk.GetLayer(sprite.Texture, nlayer);

            SpriteMainData smd = new()
            {
                SpritePosition = new Vector3(WorldUtils.GetSpritePositionByCellPosition(tilePos).ToVector2(), vertexZ) + spriteOffset,
                CellPosition = tilePos,
                //SpriteSize = new Vector2(32, 32)
            };
            SpriteExtraData sed = new()
            {
                Color = color.ToVector4(),
                TextureRect = new Vector4(sprite.RectPos.X, sprite.RectPos.Y, sprite.RectPos.Width, sprite.RectPos.Height)
            };
            return chunk.AppendDataDirectly(layer, smd, sed);
        }

        public SpriteLocator ScheduleUpdate(byte nlayer, Point3 tilePos, Sprite sprite, Color color, Vector3 spriteOffset = new())
        {
            var chunk = GetChunkByPos(tilePos);

            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(tilePos);
            SpriteLayer layer = chunk.GetLayer(sprite.Texture, nlayer);

            SpriteMainData smd = new()
            {
                SpritePosition = new Vector3(WorldUtils.GetSpritePositionByCellPosition(tilePos).ToVector2(), vertexZ) + spriteOffset,
                CellPosition = tilePos,
                //SpriteSize = new Vector2(32, 32)
            };
            SpriteExtraData sed = new()
            {
                Color = color.ToVector4(),
                TextureRect = new Vector4(sprite.RectPos.X, sprite.RectPos.Y, sprite.RectPos.Width, sprite.RectPos.Height)
            };
            return ScheduleAdd(layer, smd, sed, tilePos);
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

        public void ScheduleRemove(SpriteLocatorsStatic locators, Point3 pos)
        {
            foreach (var locator in locators.list)
            {
                spriteChunks[0, 0, pos.Z].ScheduleRemove(locator);
            }
        }

        public SpriteLocator ScheduleAdd(SpriteLayer layer, SpriteMainData dataMain, SpriteExtraData dataExtra, Point3 pos)
        {
            return spriteChunks[0, 0, pos.Z].ScheduleAdd(layer, dataMain, dataExtra);
        }

        public void RemoveSprites()
        {
            for (int z = 0; z < chunksCount.Z; z++)
                if (spriteChunks[0, 0, z] != null)
                    spriteChunks[0, 0, z].RemoveScheduled();
        }

        public void AddSprites()
        {
            for (int z = 0; z < chunksCount.Z; z++)
                if (spriteChunks[0, 0, z] != null)
                    spriteChunks[0, 0, z].AddScheduled();
        }

        public void Draw(int layer, List<byte> drawableSubLayers = null)
        {
            void CheckLayerLight(byte sublayer)
            {
                site.LightControl.SetBuffers();

                SiteRenderer.InstanceMainEffect.Parameters["SunLightIntensity"].SetValue(site.World.TimeManager.GetSunLightIntensity());
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
                    device.BlendState = BlendState.Additive;
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
            foreach (var tex in texture2Ds)
            {
                SiteRenderer.InstanceMainEffect.Parameters["SpriteTexture"].SetValue(tex);
                SiteRenderer.InstanceMainEffect.Parameters["TextureSize"].SetValue(new Vector2(tex.Width, tex.Height));

                device.SetVertexBuffer(SiteRenderer.GeometryBuffer);
                device.DepthStencilState = DepthStencilState.Default;
                device.BlendState = BlendState.AlphaBlend;

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
                                        if (layersBatches[tex].TryGetValue(sublayer, out SpriteLayer dlayer))
                                        {
                                            CheckLayerLight(sublayer);
                                            SubDraw(sublayer, dlayer);
                                        }
                                }
                                else
                                {
                                    foreach (var pair in layersBatches[tex].OrderBy(x => x.Key))
                                    {
                                        CheckLayerLight((byte)pair.Key);
                                        SubDraw((byte)pair.Key, pair.Value);
                                    }
                                }
                            }
                        }
                    }
            }
        }
    }
}