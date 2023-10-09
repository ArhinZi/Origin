using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.ECS;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.unused
{
    public class SiteInstancingBuffer
    {
        public struct IBData
        {
            public Texture2D Texture;
            public VertexBuffer VertexBuffer;
            public IndexBuffer IndexBuffer;

            public bool instanceDirty;
            public InstanceData[] instanceDatas;
            public VertexBuffer InstanceBuffer;

            public VertexBufferBinding[] Binding;
        }

        private static readonly object Lock = new object();

        public VertexBuffer GeometryBuffer;
        public uint GeomIndex = 0;
        public IndexBuffer IndexBuffer;
        public uint IndIndex = 0;
        public VertexBuffer InstanceBuffer;
        private VertexBufferBinding[] bindings;

        public Dictionary<string, IBData> Instances;

        private GeometryData[] _vertices;
        private uint[] _indices;

        private InstanceData[] instances;
        private int instanceIndex;

        private SiteRenderer _renderer;
        private GraphicsDevice _device;

        private Point _size => _renderer.ChunkSize;

        public SiteInstancingBuffer(SiteRenderer renderer, GraphicsDevice graphicDevice)
        {
            _renderer = renderer;
            _device = graphicDevice;

            Instances = new Dictionary<string, IBData>();
        }

        public void InitHiddenWallGeometry()
        {
            TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
            Sprite sprite = tm.Sprites["Wall"][0];
            Color c = tm.Color;
            Rectangle textureRect = sprite.RectPos;
            Point drawSize = new Point(textureRect.Width, textureRect.Height);

            VertexPosition[] vertices = new VertexPosition[_size.X * _size.Y * 4];
            int[] indices = new int[_size.X * _size.Y * 6];

            int vi = 0;
            int ii = 0;
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    Point3 cellPos = new Point3(x, y, 0);
                    Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos);
                    float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                    // Calc the sprite corners positions
                    Vector3 topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                    Vector3 topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                    Vector3 bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                    Vector3 bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                    vertices[vi + 0].Position = topLeft;
                    vertices[vi + 1].Position = topRight;
                    vertices[vi + 2].Position = bottomRight;
                    vertices[vi + 3].Position = bottomLeft;

                    indices[ii++] = vi + 0;
                    indices[ii++] = vi + 1;
                    indices[ii++] = vi + 3;
                    indices[ii++] = vi + 1;
                    indices[ii++] = vi + 2;
                    indices[ii++] = vi + 3;

                    vi += 4;
                }
            }
            VertexBuffer vb = new(_device, typeof(VertexPosition), vertices.Length, BufferUsage.WriteOnly);
            vb.SetData(vertices);
            IndexBuffer ib = new(_device, typeof(int), _indices.Length, BufferUsage.WriteOnly);
            ib.SetData(indices);

            string key = "HIDDEN_WALL";
            Instances.Add(key, new IBData() { VertexBuffer = vb, IndexBuffer = ib });
        }

        public void InitHiddenFloorGeometry()
        {
            TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
            Sprite sprite = tm.Sprites["Floor"][0];
            Color c = tm.Color;
            Rectangle textureRect = sprite.RectPos;
            Point drawSize = new Point(textureRect.Width, textureRect.Height);

            VertexPosition[] vertices = new VertexPosition[_size.X * _size.Y * 4];
            int[] indices = new int[_size.X * _size.Y * 6];

            int vi = 0;
            int ii = 0;
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    Point3 cellPos = new Point3(x, y, 0);
                    Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos) + new Point(0, -Sprite.FLOOR_YOFFSET);
                    float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos);

                    // Calc the sprite corners positions
                    Vector3 topLeft = new Vector3(spritePos.X, spritePos.Y, vertexZ);
                    Vector3 topRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
                    Vector3 bottomLeft = new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
                    Vector3 bottomRight = new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

                    vertices[vi + 0].Position = topLeft;
                    vertices[vi + 1].Position = topRight;
                    vertices[vi + 2].Position = bottomRight;
                    vertices[vi + 3].Position = bottomLeft;

                    indices[ii++] = vi + 0;
                    indices[ii++] = vi + 1;
                    indices[ii++] = vi + 3;
                    indices[ii++] = vi + 1;
                    indices[ii++] = vi + 2;
                    indices[ii++] = vi + 3;

                    vi += 4;
                }
            }
            VertexBuffer vb = new(_device, typeof(VertexPosition), vertices.Length, BufferUsage.WriteOnly);
            vb.SetData(vertices);
            IndexBuffer ib = new(_device, typeof(int), _indices.Length, BufferUsage.WriteOnly);
            ib.SetData(indices);

            string key = "HIDDEN_FLOOR";
        }

        public void SetInstances()
        {
            if (instanceIndex > 0)
            {
                GeometryBuffer = new VertexBuffer(_device, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
                IndexBuffer = new IndexBuffer(_device, typeof(ushort), _indices.Length, BufferUsage.WriteOnly);
                GeometryBuffer.SetData(_vertices);
                IndexBuffer.SetData(_indices);

                InstanceBuffer = new VertexBuffer(_device, typeof(InstanceData), instanceIndex, BufferUsage.WriteOnly);
                InstanceBuffer.SetData(instances, 0, instanceIndex);

                bindings = new VertexBufferBinding[2];
                bindings[0] = new VertexBufferBinding(GeometryBuffer);

                bindings[1] = new VertexBufferBinding(InstanceBuffer, 0, 1);
            }
        }

        public void Draw(Effect effect)
        {
            if (instanceIndex > 0)
            {
                _device.Indices = IndexBuffer;
                effect.CurrentTechnique.Passes[0].Apply();
                _device.SetVertexBuffers(bindings);
                _device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3, instances.Length);
            }
        }
    }
}