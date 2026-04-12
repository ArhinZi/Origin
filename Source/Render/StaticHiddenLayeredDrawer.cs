using Microsoft.Xna.Framework.Graphics;
using Origin.Source.Model.Map;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Origin.Source.Render
{
    public class StaticHiddenLayeredDrawer : IBaseLayeredDrawer
    {
        public static int BIT_COUNT = 32 * 4;
        private GraphicsDevice device = Global.GraphicsDevice;

        // Packed hidden masks per Z-level for full top layers.
        // Indexed as [z][packedXY].
        private Float4[][] _lData;

        // Packed hidden masks per Z-level for the two front side curtains.
        // First segment stores the front-left strip, second segment stores the front-right strip.
        private Float4[][] _sData;

        // GPU buffers mirroring _lData and _sData.
        private StructuredBuffer[] bufferLayers;
        private StructuredBuffer[] bufferSides;

        // Number of packed bits stored in one Float4 entry.
        private int RBIT_COUNT;

        // Packed element counts for one full horizontal layer and one side-strip buffer.
        private int LCHUNK_SIZE;
        private int SCHUNK_SIZE;

        private Site _site;
        private Texture2D _texture;

        public StaticHiddenLayeredDrawer(Site site)
        {
            _site = site;
            _texture = GlobalResources.HIDDEN_WALL_SPRITE.Texture;
            RBIT_COUNT = Math.Min(_site.Size.X, BIT_COUNT);
            LCHUNK_SIZE = _site.Size.X / RBIT_COUNT * _site.Size.Y;
            SCHUNK_SIZE = _site.Size.X / RBIT_COUNT + _site.Size.Y / RBIT_COUNT;

            _lData = new Float4[_site.Size.Z][];
            for (int z = 0; z < _lData.Length; z++)
            {
                _lData[z] = new Float4[LCHUNK_SIZE];
                for (int xy = 0; xy < _lData[z].Length; xy++)
                    _lData[z][xy] = new Float4();
            }

            _sData = new Float4[_site.Size.Z][];
            for (int z = 0; z < _sData.Length; z++)
            {
                _sData[z] = new Float4[SCHUNK_SIZE];
                for (int xy = 0; xy < _sData[z].Length; xy++)
                    _sData[z][xy] = new Float4();
            }

            bufferLayers = new StructuredBuffer[_site.Size.Z];
            for (int i = 0; i < bufferLayers.Length; i++)
            {
                bufferLayers[i] = new StructuredBuffer(device, typeof(Float4), LCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
            }
            bufferSides = new StructuredBuffer[_site.Size.Z];
            for (int i = 0; i < bufferSides.Length; i++)
            {
                bufferSides[i] = new StructuredBuffer(device, typeof(Float4), SCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
            }
        }

        public void MakeHidden(Point3 pos)
        {
            SetHiddence(pos, true);
        }

        public void ClearHidden(Point3 pos)
        {
            SetHiddence(pos, false);
        }

        private bool TryGetFrontLeftSideIndex(Point3 pos, out int sideIndex)
        {
            sideIndex = -1;
            switch (_site.Rotation)
            {
                case WorldRotation.TR:
                    if (pos.Y == _site.Size.Y - 1)
                    {
                        sideIndex = pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.TL:
                    if (pos.X == 0)
                    {
                        sideIndex = pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.BL:
                    if (pos.Y == 0)
                    {
                        sideIndex = _site.Size.X - 1 - pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.BR:
                    if (pos.X == _site.Size.X - 1)
                    {
                        sideIndex = _site.Size.Y - 1 - pos.Y;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool TryGetFrontRightSideIndex(Point3 pos, out int sideIndex)
        {
            sideIndex = -1;
            switch (_site.Rotation)
            {
                case WorldRotation.TR:
                    if (pos.X == _site.Size.X - 1)
                    {
                        sideIndex = _site.Size.Y - 1 - pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.TL:
                    if (pos.Y == _site.Size.Y - 1)
                    {
                        sideIndex = pos.X;
                        return true;
                    }
                    break;
                case WorldRotation.BL:
                    if (pos.X == 0)
                    {
                        sideIndex = pos.Y;
                        return true;
                    }
                    break;
                case WorldRotation.BR:
                    if (pos.Y == 0)
                    {
                        sideIndex = _site.Size.X - 1 - pos.X;
                        return true;
                    }
                    break;
            }

            return false;
        }

        // Writes one bit into the packed front-side buffer.
        private void SetSideBit(int z, int sideIndex, bool value)
        {
            int xy;
            int nxbit;

            if (sideIndex < _site.Size.X)
            {
                xy = sideIndex / RBIT_COUNT;
                nxbit = sideIndex % RBIT_COUNT;
            }
            else
            {
                int rightIndex = sideIndex - _site.Size.X;
                xy = _site.Size.X / RBIT_COUNT + rightIndex / RBIT_COUNT;
                nxbit = rightIndex % RBIT_COUNT;
            }

            _sData[z][xy].SetBit(nxbit, value);
        }

        // Updates both the horizontal hidden layer and the two front-facing side strips.
        public void SetHiddence(Point3 pos, bool value)
        {
            int nxbit = pos.X % RBIT_COUNT;
            int xy = _site.Size.X / RBIT_COUNT * pos.Y + pos.X / RBIT_COUNT;
            _lData[pos.Z][xy].SetBit(nxbit, value);

            if (TryGetFrontLeftSideIndex(pos, out int leftIndex))
            {
                SetSideBit(pos.Z, leftIndex, value);
            }

            if (TryGetFrontRightSideIndex(pos, out int rightIndex))
            {
                SetSideBit(pos.Z, _site.Size.X + rightIndex, value);
            }
        }

        public void Set()
        {
            for (int z = 0; z < _site.Size.Z; z++)
            {
                bufferLayers[z].SetData(_lData[z]);
                bufferSides[z].SetData(_sData[z]);
            }
        }

        private void DrawBasic(int layer)
        {
            SiteRenderer.InstanceMainEffect.Parameters["PositionOffset"].SetValue(new Vector3(0, 0, 0));
            SiteRenderer.InstanceMainEffect.Parameters["RBIT_COUNT"].SetValue(RBIT_COUNT);
            SiteRenderer.InstanceMainEffect.Parameters["SpriteTexture"].SetValue(_texture);
            SiteRenderer.InstanceMainEffect.Parameters["TextureSize"].SetValue(new Vector2(_texture.Width, _texture.Height));

            device.SetVertexBuffer(SiteRenderer.GeometryBuffer);
            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.AlphaBlend;
        }

        public void DrawLayer(int layer, bool HalfWall = false)
        {
            DrawBasic(layer);

            if (HalfWall)
            {
                SiteRenderer.InstanceMainEffect.Parameters["PositionOffset"].SetValue(new Vector3(0, 25, 0));
                SiteRenderer.InstanceMainEffect.Parameters["HiddenSpriteTexturePos"].SetValue(GlobalResources.HIDDEN_FLOOR_SPRITE.RectPos.Location.ToVector2());
            }
            else
            {
                SiteRenderer.InstanceMainEffect.Parameters["HiddenSpriteTexturePos"].SetValue(GlobalResources.HIDDEN_WALL_SPRITE.RectPos.Location.ToVector2());
            }

            SiteRenderer.InstanceMainEffect.Parameters["HiddenLBuffer"].SetValue(bufferLayers[layer]);
            SiteRenderer.InstanceMainEffect.CurrentTechnique = SiteRenderer.InstanceMainEffect.Techniques["HiddenLInstancing"];

            SiteRenderer.InstanceMainEffect.CurrentTechnique.Passes[0].Apply();

            device.DrawPrimitives(PrimitiveType.TriangleList, 0, _site.Size.X * _site.Size.Y * 2);
        }

        public void DrawSides(int layer)
        {
            DrawBasic(layer);

            SiteRenderer.InstanceMainEffect.Parameters["HiddenSBuffer"].SetValue(bufferSides[layer]);
            SiteRenderer.InstanceMainEffect.CurrentTechnique = SiteRenderer.InstanceMainEffect.Techniques["HiddenSInstancing"];

            SiteRenderer.InstanceMainEffect.Parameters["HiddenSpriteTexturePos"].SetValue(GlobalResources.HIDDEN_WALL_SPRITE.RectPos.Location.ToVector2());
            SiteRenderer.InstanceMainEffect.CurrentTechnique.Passes[0].Apply();
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, (_site.Size.X + _site.Size.Y) * 2);

            SiteRenderer.InstanceMainEffect.Parameters["PositionOffset"].SetValue(new Vector3(0, -7, 0));
            SiteRenderer.InstanceMainEffect.Parameters["HiddenSpriteTexturePos"].SetValue(GlobalResources.HIDDEN_FLOOR_SPRITE.RectPos.Location.ToVector2());
            SiteRenderer.InstanceMainEffect.CurrentTechnique.Passes[0].Apply();
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, (_site.Size.X + _site.Size.Y) * 2);
        }

        public void ResetAll()
        {
            for (int z = 0; z < _lData.Length; z++)
            {
                for (int i = 0; i < _lData[z].Length; i++)
                    _lData[z][i] = new Float4();
                for (int i = 0; i < _sData[z].Length; i++)
                    _sData[z][i] = new Float4();
            }
        }
    }
}