using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InstancePositionColorTextureLayer : IVertexType
    {
        public Vector3 Position;

        public Color Color;

        public Vector4 TextureCoordinate;

        public int Layer;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(VertexElementByteOffset.PositionStartOffset(), VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
                new VertexElement(VertexElementByteOffset.OffsetColor(), VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(VertexElementByteOffset.OffsetVector4(), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(VertexElementByteOffset.OffsetInt(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3));

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public InstancePositionColorTextureLayer(Vector3 position, Color color, Vector4 textureCoordinate, int layer)
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
            Layer = layer;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
            return (((Position.GetHashCode() * 397) ^ Color.GetHashCode()) * 397) ^ TextureCoordinate.GetHashCode();
        }

        public override string ToString()
        {
            string[] obj = new string[7] { "{{Position:", null, null, null, null, null, null };
            Vector3 position = Position;
            obj[1] = position.ToString();
            obj[2] = " Color:";
            Color color = Color;
            obj[3] = color.ToString();
            obj[4] = " TextureCoordinate:";
            obj[5] = Layer.ToString();
            obj[6] = "}}";
            return string.Concat(obj);
        }

        public static bool operator ==(InstancePositionColorTextureLayer left, InstancePositionColorTextureLayer right)
        {
            if (left.Position == right.Position && left.Color == right.Color)
            {
                return left.TextureCoordinate == right.TextureCoordinate;
            }

            return false;
        }

        public static bool operator !=(InstancePositionColorTextureLayer left, InstancePositionColorTextureLayer right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return this == (InstancePositionColorTextureLayer)obj;
        }

        /// <summary>
        /// This is a helper struct for tallying byte offsets
        /// </summary>
        public struct VertexElementByteOffset
        {
            public static int currentByteSize = 0;

            //[STAThread]
            public static int PositionStartOffset()
            { currentByteSize = 0; var s = sizeof(float) * 3; currentByteSize += s; return currentByteSize - s; }

            public static int Offset(int n)
            { var s = sizeof(int); currentByteSize += s; return currentByteSize - s; }

            public static int Offset(float n)
            { var s = sizeof(float); currentByteSize += s; return currentByteSize - s; }

            public static int Offset(Vector2 n)
            { var s = sizeof(float) * 2; currentByteSize += s; return currentByteSize - s; }

            public static int Offset(Color n)
            { var s = sizeof(int); currentByteSize += s; return currentByteSize - s; }

            public static int Offset(Vector3 n)
            { var s = sizeof(float) * 3; currentByteSize += s; return currentByteSize - s; }

            public static int Offset(Vector4 n)
            { var s = sizeof(float) * 4; currentByteSize += s; return currentByteSize - s; }

            public static int OffsetInt()
            { var s = sizeof(int); currentByteSize += s; return currentByteSize - s; }

            public static int OffsetFloat()
            { var s = sizeof(float); currentByteSize += s; return currentByteSize - s; }

            public static int OffsetColor()
            { var s = sizeof(int); currentByteSize += s; return currentByteSize - s; }

            public static int OffsetVector2()
            { var s = sizeof(float) * 2; currentByteSize += s; return currentByteSize - s; }

            public static int OffsetVector3()
            { var s = sizeof(float) * 3; currentByteSize += s; return currentByteSize - s; }

            public static int OffsetVector4()
            { var s = sizeof(float) * 4; currentByteSize += s; return currentByteSize - s; }
        }
    }
}