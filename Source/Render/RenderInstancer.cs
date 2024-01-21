﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;

namespace Origin.Source.Render
{
    public class RenderInstancer
    {
        public enum InstanceDefs
        {
            HiddenWallFlatChunk,
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

        private Point ChunkSize;
        private GraphicsDevice _device;
        private Effect _effect;
        public Texture2D HiddenTexture = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", GlobalResources.Settings.HiddenWallSprite).Texture;

        private Sprite WallSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", GlobalResources.Settings.HiddenWallSprite);
        private Sprite FloorSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", GlobalResources.Settings.HiddenFloorSprite);
        private Color HiddenColor = GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", "HIDDEN").Color;

        private Dictionary<InstanceDefs, InstanceDeclaration> _definitions;

        public RenderInstancer(Point chunkSize, GraphicsDevice _device, Effect effect)
        {
            ChunkSize = chunkSize;
            this._device = _device;
            _effect = effect;

            _definitions = new()
            {
                { InstanceDefs.HiddenLBorder, new InstanceDeclaration() },
                { InstanceDefs.HiddenRBorder, new InstanceDeclaration() },
                { InstanceDefs.HiddenWallFlatChunk, new InstanceDeclaration() },
            };
            CreateHiddenWallFlatChunk();
            CreateHiddenLBorder();
            CreateHiddenRBorder();
        }

        private void CreateHiddenWallFlatChunk()
        {
            InstanceDefs def = InstanceDefs.HiddenWallFlatChunk;

            VertexPositionTexture[] vertices = new VertexPositionTexture[ChunkSize.X * ChunkSize.Y * 4];
            ushort[] indices = new ushort[ChunkSize.X * ChunkSize.Y * 6];

            Sprite sprite = WallSprite;
            Rectangle textureRect = sprite.RectPos;
            Point drawSize = new Point(textureRect.Width, textureRect.Height);

            ushort vi = 0;
            ushort ii = 0;
            for (int x = 0; x < ChunkSize.X; x++)
            {
                for (int y = 0; y < ChunkSize.Y; y++)
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

            VertexPositionTexture[] vertices = new VertexPositionTexture[2 * ChunkSize.X * 4];
            ushort[] indices = new ushort[2 * ChunkSize.X * 6];

            ushort vi = 0;
            ushort ii = 0;
            int y = ChunkSize.Y - 1;
            for (int x = 0; x < ChunkSize.X; x++)
            {
                Point3 cellPos = new Point3(x, y, 0);
                Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                Sprite sprite = WallSprite;
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

                sprite = FloorSprite;
                textureRect = sprite.RectPos;
                spritePos += new Point(0, -GlobalResources.Settings.FloorYoffset);
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

            VertexPositionTexture[] vertices = new VertexPositionTexture[2 * ChunkSize.Y * 4];
            ushort[] indices = new ushort[2 * ChunkSize.Y * 6];

            ushort vi = 0;
            ushort ii = 0;
            int x = ChunkSize.X - 1;
            for (int y = 0; y < ChunkSize.Y; y++)
            {
                Point3 cellPos = new Point3(x, y, 0);
                Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                Sprite sprite = WallSprite;
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

                sprite = FloorSprite;
                textureRect = sprite.RectPos;
                spritePos += new Point(0, -GlobalResources.Settings.FloorYoffset);
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
                definition.Instances = new InstancePositionColorTextureLayer[Global.ONE_MOMENT_DRAW_LEVELS * 64];
                definition.InstanceIndex = 0;
            }
            if (def == InstanceDefs.HiddenWallFlatChunk || def == InstanceDefs.HiddenLBorder || def == InstanceDefs.HiddenRBorder)
            {
                Sprite sprite = WallSprite;
                Color c = HiddenColor;
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