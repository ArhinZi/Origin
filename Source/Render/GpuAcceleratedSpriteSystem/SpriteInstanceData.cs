using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteMainData
    {
        public Vector3 SpritePosition; //12
        public float pud1;
        //[FieldOffset(12)] public Vector2 SpriteSize; //8
        //[FieldOffset(20)] public Vector3 CellPosition; //12
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteExtraData
    {
        public Vector4 Color; //16
        public Vector4 TextureRect; //16

        public override string ToString()
        {
            return Color.ToString();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GeometryData : IVertexType
    {
        [FieldOffset(0)] public Color World;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement[]{
                new VertexElement(0, VertexElementFormat.Color,
                                     VertexElementUsage.Color, 0) }
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    /*[StructLayout(LayoutKind.Explicit)]
    public struct SpriteInstanceData
    {
        [FieldOffset(0)] public Vector3 World;
        [FieldOffset(12)] public Vector2 SpriteCoordinates;
        [FieldOffset(20)] public Color Color;
        [FieldOffset(24)] public Vector2 SpriteSize;
        [FieldOffset(32)] public Vector3 BlockPosition;
        [FieldOffset(44)] public int Light;

        public override string ToString()
        {
            return BlockPosition.ToString();
        }
    }*/
}