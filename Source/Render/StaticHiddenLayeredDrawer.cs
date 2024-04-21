using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Model.Site;
using Origin.Source.Resources;

using System;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Origin.Source.Render
{
    public class StaticHiddenLayeredDrawer : IBaseLayeredDrawer
    {
        public static int BIT_COUNT = 32 * 4;
        private GraphicsDevice device = Global.GraphicsDevice;
        private Float4[][] _lData;
        private Float4[][] _sData;
        private StructuredBuffer[] bufferLayers;
        private StructuredBuffer[] bufferSides;

        private long[] LayersSize;
        private long[] SidesSize;

        private int RBIT_COUNT;
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
            LayersSize = new long[_site.Size.Z];
            for (int i = 0; i < bufferLayers.Length; i++)
            {
                bufferLayers[i] = new StructuredBuffer(device, typeof(Float4), LCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
                LayersSize[i] = 0;
            }
            bufferSides = new StructuredBuffer[_site.Size.Z];
            SidesSize = new long[_site.Size.Z];
            for (int i = 0; i < bufferSides.Length; i++)
            {
                bufferSides[i] = new StructuredBuffer(device, typeof(Float4), SCHUNK_SIZE, BufferUsage.None, ShaderAccess.Read);
                SidesSize[i] = 0;
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

        public void SetHiddence(Point3 pos, bool value)
        {
            int nxbit = pos.X % RBIT_COUNT;
            int xy = _site.Size.X / RBIT_COUNT * pos.Y + pos.X / RBIT_COUNT;
            _lData[pos.Z][xy].SetBit(nxbit, value);

            if (_site.Size.Y - 1 == pos.Y)
            {
                xy = pos.X / RBIT_COUNT;
                nxbit = pos.X % RBIT_COUNT;
                _sData[pos.Z][xy].SetBit(nxbit, value);
            }
            else if (_site.Size.X - 1 == pos.X)
            {
                xy = _site.Size.X / RBIT_COUNT + (_site.Size.Y / RBIT_COUNT - 1 - pos.Y / RBIT_COUNT);
                nxbit = (_site.Size.Y - 1 - pos.Y) % RBIT_COUNT;
                _sData[pos.Z][xy].SetBit(nxbit, value);
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
    }
}