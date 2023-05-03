using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    /// <summary>
    /// Animation element 8 bits
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct VertexAnimationElement
    {
        [FieldOffset(0)] public Vector2 texturePosition;
    }

    /// <summary>
    /// Animation info 32 bits
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct VertexAnimationIndex
    {
        [FieldOffset(0)] public short indexCount;
        [FieldOffset(2)] public short currentIndex;
    }
}