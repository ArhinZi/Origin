using CommunityToolkit.HighPerformance;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public class RenderInstancer
    {
        public enum InstanceDefs
        {
            HiddenWallFlatChank,
            HiddenLBorder,
            HiddenRBorder,
        }

        private class InstanceDeclaration
        {
            public VertexBuffer Geometry;
            public IndexBuffer Indexes;

            public InstancePositionColorTextureLayer[] Instances;
            public int InstanceIndex = 0;

            public VertexBuffer InstanceBuffer;
            public VertexBufferBinding[] Binding;
        }

        /*public VertexBuffer HiddenChankWallGeometry;
        public IndexBuffer HiddenChankWallIndexes;

        public VertexBuffer HiddenChankFloorGeometry;
        public IndexBuffer HiddenChankFloorIndexes;

        private
        private */

        private Point ChankSize;
        private GraphicsDevice _device;
        private Effect _effect;
        public Texture2D HiddenTexture = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID).Sprites["Wall"][0].Texture as Texture2D;

        private Sprite WallSprite = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID).Sprites["Wall"][0];
        private Sprite FloorSprite = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID).Sprites["Floor"][0];

        private Dictionary<InstanceDefs, InstanceDeclaration> _definitions;

        public RenderInstancer(Point chankSize, GraphicsDevice _device, Effect effect)
        {
            ChankSize = chankSize;
            this._device = _device;
            _effect = effect;

            _definitions = new()
            {
                { InstanceDefs.HiddenLBorder, new InstanceDeclaration() },
                { InstanceDefs.HiddenRBorder, new InstanceDeclaration() },
                { InstanceDefs.HiddenWallFlatChank, new InstanceDeclaration() },
            };
            CreateHiddenWallFlatChank();
            CreateHiddenLBorder();
            CreateHiddenRBorder();
            //CreateHiddenRBorder();
        }

        private void CreateHiddenWallFlatChank()
        {
            InstanceDefs def = InstanceDefs.HiddenWallFlatChank;

            VertexPositionTexture[] vertices = new VertexPositionTexture[ChankSize.X * ChankSize.Y * 4];
            ushort[] indices = new ushort[ChankSize.X * ChankSize.Y * 6];

            TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
            Sprite sprite = tm.Sprites["Wall"][0];
            Color c = tm.Color;
            Rectangle textureRect = sprite.RectPos;
            Point drawSize = new Point(textureRect.Width, textureRect.Height);

            ushort vi = 0;
            ushort ii = 0;
            for (int x = 0; x < ChankSize.X; x++)
            {
                for (int y = 0; y < ChankSize.Y; y++)
                {
                    Point3 cellPos = new Point3(x, y, 0);
                    Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                    float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                    // Calc the sprite corners positions
                    Vector3 topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                    Vector3 topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                    Vector3 bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                    Vector3 bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                    // Calc the texture coordinates
                    Vector2 textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                    Vector2 textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                    Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
                    Vector2 textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

                    vertices[vi + 0] = new VertexPositionTexture(topLeft, textureTopLeft);
                    vertices[vi + 1] = new VertexPositionTexture(topRight, textureTopRight);
                    vertices[vi + 2] = new VertexPositionTexture(bottomRight, textureBottomRight);
                    vertices[vi + 3] = new VertexPositionTexture(bottomLeft, textureBottomLeft);

                    indices[ii++] = (ushort)(vi + 0);
                    indices[ii++] = (ushort)(vi + 1);
                    indices[ii++] = (ushort)(vi + 3);
                    indices[ii++] = (ushort)(vi + 1);
                    indices[ii++] = (ushort)(vi + 2);
                    indices[ii++] = (ushort)(vi + 3);

                    if (vi > 65000) throw new Exception();
                    vi += 4;
                }
            }

            _definitions[def].Geometry = new(_device, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            _definitions[def].Geometry.SetData(vertices);
            _definitions[def].Indexes = new(_device, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
            _definitions[def].Indexes.SetData(indices);
        }

        private void CreateHiddenLBorder()
        {
            InstanceDefs def = InstanceDefs.HiddenLBorder;

            VertexPositionTexture[] vertices = new VertexPositionTexture[2 * ChankSize.X * 4];
            ushort[] indices = new ushort[2 * ChankSize.X * 6];

            ushort vi = 0;
            ushort ii = 0;
            int y = ChankSize.Y - 1;
            for (int x = 0; x < ChankSize.X; x++)
            {
                TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
                Point3 cellPos = new Point3(x, y, 0);
                Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                Sprite sprite = tm.Sprites["Wall"][0];
                Rectangle textureRect = sprite.RectPos;
                Point drawSize = new Point(textureRect.Width, textureRect.Height);

                // Calc the sprite corners positions
                Vector3 topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                Vector3 topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                Vector3 bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                Vector3 bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                // Calc the texture coordinates
                Vector2 textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                Vector2 textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
                Vector2 textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

                vertices[vi + 0] = new VertexPositionTexture(topLeft, textureTopLeft);
                vertices[vi + 1] = new VertexPositionTexture(topRight, textureTopRight);
                vertices[vi + 2] = new VertexPositionTexture(bottomRight, textureBottomRight);
                vertices[vi + 3] = new VertexPositionTexture(bottomLeft, textureBottomLeft);

                indices[ii++] = (ushort)(vi + 0);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 3);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 2);
                indices[ii++] = (ushort)(vi + 3);

                vi += 4;

                sprite = tm.Sprites["Floor"][0];
                textureRect = sprite.RectPos;
                spritePos += new Point(0, -Sprite.FLOOR_YOFFSET);
                // Calc the sprite corners positions
                topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                // Calc the texture coordinates
                textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
                textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

                vertices[vi + 0] = new VertexPositionTexture(topLeft, textureTopLeft);
                vertices[vi + 1] = new VertexPositionTexture(topRight, textureTopRight);
                vertices[vi + 2] = new VertexPositionTexture(bottomRight, textureBottomRight);
                vertices[vi + 3] = new VertexPositionTexture(bottomLeft, textureBottomLeft);

                indices[ii++] = (ushort)(vi + 0);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 3);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 2);
                indices[ii++] = (ushort)(vi + 3);

                vi += 4;

                if (vi > 65000) throw new Exception();
            }

            _definitions[def].Geometry = new(_device, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            _definitions[def].Geometry.SetData(vertices);
            _definitions[def].Indexes = new(_device, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
            _definitions[def].Indexes.SetData(indices);
        }

        private void CreateHiddenRBorder()
        {
            InstanceDefs def = InstanceDefs.HiddenRBorder;

            VertexPositionTexture[] vertices = new VertexPositionTexture[2 * ChankSize.X * 4];
            ushort[] indices = new ushort[2 * ChankSize.X * 6];

            ushort vi = 0;
            ushort ii = 0;
            int x = ChankSize.X - 1;
            for (int y = 0; y < ChankSize.Y; y++)
            {
                TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
                Point3 cellPos = new Point3(x, y, 0);
                Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                Sprite sprite = tm.Sprites["Wall"][0];
                Rectangle textureRect = sprite.RectPos;
                Point drawSize = new Point(textureRect.Width, textureRect.Height);

                // Calc the sprite corners positions
                Vector3 topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                Vector3 topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                Vector3 bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                Vector3 bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                // Calc the texture coordinates
                Vector2 textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                Vector2 textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
                Vector2 textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

                vertices[vi + 0] = new VertexPositionTexture(topLeft, textureTopLeft);
                vertices[vi + 1] = new VertexPositionTexture(topRight, textureTopRight);
                vertices[vi + 2] = new VertexPositionTexture(bottomRight, textureBottomRight);
                vertices[vi + 3] = new VertexPositionTexture(bottomLeft, textureBottomLeft);

                indices[ii++] = (ushort)(vi + 0);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 3);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 2);
                indices[ii++] = (ushort)(vi + 3);

                vi += 4;

                sprite = tm.Sprites["Floor"][0];
                textureRect = sprite.RectPos;
                spritePos += new Point(0, -Sprite.FLOOR_YOFFSET);
                // Calc the sprite corners positions
                topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                // Calc the texture coordinates
                textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
                textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
                textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

                vertices[vi + 0] = new VertexPositionTexture(topLeft, textureTopLeft);
                vertices[vi + 1] = new VertexPositionTexture(topRight, textureTopRight);
                vertices[vi + 2] = new VertexPositionTexture(bottomRight, textureBottomRight);
                vertices[vi + 3] = new VertexPositionTexture(bottomLeft, textureBottomLeft);

                indices[ii++] = (ushort)(vi + 0);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 3);
                indices[ii++] = (ushort)(vi + 1);
                indices[ii++] = (ushort)(vi + 2);
                indices[ii++] = (ushort)(vi + 3);

                vi += 4;

                if (vi > 65000) throw new Exception();
            }

            _definitions[def].Geometry = new(_device, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            _definitions[def].Geometry.SetData(vertices);
            _definitions[def].Indexes = new(_device, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
            _definitions[def].Indexes.SetData(indices);
        }

        public void AddInstance(InstanceDefs def, Vector3 position, int layer)
        {
            var definition = _definitions[def];

            if (definition.Instances == null)
            {
                definition.Instances = new InstancePositionColorTextureLayer[SiteRenderer.ONE_MOMENT_DRAW_LEVELS * 64];
                definition.InstanceIndex = 0;
            }
            if (def == InstanceDefs.HiddenWallFlatChank || def == InstanceDefs.HiddenLBorder || def == InstanceDefs.HiddenRBorder)
            {
                TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
                Sprite sprite = tm.Sprites["Wall"][0];
                Color c = tm.Color;
                Rectangle textureRect = sprite.RectPos;
                var data = new InstancePositionColorTextureLayer()
                {
                    Position = position,
                    Color = c,
                    Layer = layer
                };
                definition.Instances[definition.InstanceIndex++] = data;
            }
        }

        public void ClearInstances()
        {
            foreach (var key in _definitions.Keys)
            {
                _definitions[key].InstanceIndex = 0;
            }
        }

        public void SetInstances()
        {
            foreach (var key in _definitions.Keys)
            {
                var InstanceIndex = _definitions[key].InstanceIndex;
                if (InstanceIndex > 0)
                {
                    var Geometry = _definitions[key].Geometry;
                    var GeomIndexes = _definitions[key].Indexes;
                    var Instances = _definitions[key].Instances;
                    var InstanceBuffer = _definitions[key].InstanceBuffer;

                    InstanceBuffer = new VertexBuffer(_device, typeof(InstancePositionColorTextureLayer), InstanceIndex, BufferUsage.WriteOnly);
                    InstanceBuffer.SetData(Instances, 0, InstanceIndex);

                    _definitions[key].Binding = new VertexBufferBinding[2];
                    _definitions[key].Binding[0] = new VertexBufferBinding(Geometry);
                    _definitions[key].Binding[1] = new VertexBufferBinding(InstanceBuffer, 0, 1);
                }
            }
        }

        public void DrawInstancedHidden()
        {
            _effect.Parameters["Texture"].SetValue(HiddenTexture);
            foreach (var key in _definitions.Keys)
            {
                var InstanceIndex = _definitions[key].InstanceIndex;
                if (InstanceIndex > 0)
                {
                    var Geometry = _definitions[key].Geometry;
                    var GeomIndexes = _definitions[key].Indexes;
                    var Instances = _definitions[key].Instances;
                    var InstanceBuffer = _definitions[key].InstanceBuffer;
                    var Binding = _definitions[key].Binding;

                    _device.Indices = GeomIndexes;

                    _effect.CurrentTechnique.Passes[0].Apply();

                    _device.SetVertexBuffers(Binding);

                    _device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, GeomIndexes.IndexCount / 3, InstanceIndex);
                }
            }
        }
    }
}