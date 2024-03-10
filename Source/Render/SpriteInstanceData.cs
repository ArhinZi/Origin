﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.Runtime.InteropServices;

using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Origin.Source.Render
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteMainData
    {
        public Vector3 SpritePosition; //12
        public uint pud1;
        public Point3 CellPosition; //12
        public uint pud2;
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