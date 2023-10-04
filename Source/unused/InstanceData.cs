using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Origin.Source.unused
{
    /// <summary>
    /// Instances for Drawing
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct InstanceData : IVertexType
    {
        [FieldOffset(0)] public Vector3 Position;
        [FieldOffset(12)] public Vector3 BlockPosition;

        // Define the VertexDeclaration
        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 3)
                );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}